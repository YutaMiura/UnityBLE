// UnityBleWindows - C++/WinRT BLE native plugin for Unity (Windows Editor / Standalone).
//
// This DLL mirrors the contract expected by the C# WindowsBleNativePlugin bridge
// (see Runtime/internal/Windows/WindowsBleNativePlugin.cs). All exported entry
// points use the "UnityBLEWin_" prefix. Results are delivered asynchronously
// through registered callbacks using JSON DTOs that match the package's
// PeripheralDTO / ServiceDTO / CharacteristicDTO field names.
//
// Target: x64 only (64-bit Windows has a single calling convention, so the
// __cdecl/__stdcall distinction between the C# delegates and these callbacks
// is irrelevant). Requires Windows 10 1809+ for the BLE GATT APIs used here.

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Devices.Bluetooth.h>
#include <winrt/Windows.Devices.Bluetooth.Advertisement.h>
#include <winrt/Windows.Devices.Bluetooth.GenericAttributeProfile.h>
#include <winrt/Windows.Devices.Radios.h>
#include <winrt/Windows.Storage.Streams.h>
#include <winrt/Windows.Security.Cryptography.h>

#include <atomic>
#include <cctype>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <map>
#include <memory>
#include <mutex>
#include <set>
#include <sstream>
#include <string>
#include <vector>

using namespace winrt;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Foundation::Collections;
using namespace winrt::Windows::Devices::Bluetooth;
using namespace winrt::Windows::Devices::Bluetooth::Advertisement;
using namespace winrt::Windows::Devices::Bluetooth::GenericAttributeProfile;
using namespace winrt::Windows::Devices::Radios;
using namespace winrt::Windows::Storage::Streams;
using namespace winrt::Windows::Security::Cryptography;

#define UNITYBLE_API extern "C" __declspec(dllexport)

// ---------------------------------------------------------------------------
// Callback typedefs (match the C# delegates in WindowsBleNativePlugin).
// ---------------------------------------------------------------------------
typedef void(*PeripheralFoundCb)(const char* json);
typedef void(*ConnectedCb)(const char* json);
typedef void(*DisconnectedCb)(const char* uuid);
typedef void(*BleErrorCb)(const char* json);
typedef void(*BleStateChangedCb)(int state);
typedef void(*ClearedCb)();
typedef void(*ServiceFoundCb)(const char* json);
typedef void(*CharacteristicFoundCb)(const char* json);
typedef void(*LogCb)(const char* msg);
typedef void(*WriteCompletedCb)(const char* charUuid, int result, const char* err);
typedef void(*ReadRssiCb)(int result, const char* err);
typedef void(*ValueReceivedCb)(const char* charUuid, const char* base64);

namespace {

// BleStatus enum values (must match Runtime/interfaces/BleStatus.cs).
enum class BleState : int {
    Unknown = 0,
    Resetting = 1,
    UnSupported = 2,
    UnAuthorized = 3,
    PoweredOff = 4,
    PoweredOn = 5,
};

// Registered callbacks.
PeripheralFoundCb       g_onPeripheralFound = nullptr;
ConnectedCb             g_onConnected = nullptr;
DisconnectedCb          g_onDisconnected = nullptr;
BleErrorCb              g_onError = nullptr;
BleStateChangedCb       g_onStateChanged = nullptr;
ClearedCb               g_onCleared = nullptr;
ServiceFoundCb          g_onService = nullptr;
CharacteristicFoundCb   g_onCharacteristic = nullptr;
LogCb                   g_onLog = nullptr;
WriteCompletedCb        g_onWriteCompleted = nullptr;
ReadRssiCb              g_onReadRssi = nullptr;
ValueReceivedCb         g_onValueReceived = nullptr;

std::once_flag g_initOnce;

// Scan state.
BluetoothLEAdvertisementWatcher g_watcher{ nullptr };
bool g_scanning = false;
std::set<uint64_t> g_seen;
std::set<std::string> g_serviceFilter; // lower-case service UUIDs
std::string g_nameFilter;
std::mutex g_scanMutex;

// Connected device tracking.
struct DeviceContext {
    BluetoothLEDevice device{ nullptr };
    GattSession session{ nullptr };                            // keeps the BLE link alive
    uint64_t address = 0;
    std::atomic<bool> connected{ false };                      // set once connection is established
    event_token connectionToken{};
    std::map<std::string, GattCharacteristic> characteristics; // key: char UUID (lower)
    std::map<std::string, event_token> valueChangedTokens;     // key: char UUID (lower)
    std::map<std::string, bool> appSubscribed;                 // key: char UUID -> app wants values
    std::map<std::string, std::vector<std::string>> pendingValues; // buffered base64 before app subscribes
};
std::map<uint64_t, std::shared_ptr<DeviceContext>> g_devices;
std::mutex g_devMutex;

// ---------------------------------------------------------------------------
// Helpers.
// ---------------------------------------------------------------------------
void EnsureInit() {
    std::call_once(g_initOnce, []() {
        try {
            winrt::init_apartment(apartment_type::multi_threaded);
        } catch (...) {
            // The Unity thread may already be initialized with a different
            // apartment model; that is fine for our usage.
        }
    });
}

void Log(const std::string& msg) {
    if (g_onLog) g_onLog(msg.c_str());
}

void LogError(const std::string& msg) {
    if (g_onLog) g_onLog((std::string("Error:") + msg).c_str());
}

std::string ToLower(std::string s) {
    for (auto& c : s) c = (char)std::tolower((unsigned char)c);
    return s;
}

std::string AddressToString(uint64_t addr) {
    char buf[13];
    std::snprintf(buf, sizeof(buf), "%012llx", (unsigned long long)addr);
    return std::string(buf);
}

uint64_t StringToAddress(const std::string& s) {
    return std::strtoull(s.c_str(), nullptr, 16);
}

std::string GuidToString(const winrt::guid& g) {
    char buf[37];
    std::snprintf(buf, sizeof(buf),
        "%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x",
        (unsigned)g.Data1, (unsigned)g.Data2, (unsigned)g.Data3,
        g.Data4[0], g.Data4[1], g.Data4[2], g.Data4[3],
        g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7]);
    return std::string(buf);
}

std::string JsonEscape(const std::string& in) {
    std::string out;
    out.reserve(in.size() + 8);
    for (char c : in) {
        switch (c) {
            case '"':  out += "\\\""; break;
            case '\\': out += "\\\\"; break;
            case '\b': out += "\\b"; break;
            case '\f': out += "\\f"; break;
            case '\n': out += "\\n"; break;
            case '\r': out += "\\r"; break;
            case '\t': out += "\\t"; break;
            default:
                if ((unsigned char)c < 0x20) {
                    char u[8];
                    std::snprintf(u, sizeof(u), "\\u%04x", c);
                    out += u;
                } else {
                    out += c;
                }
        }
    }
    return out;
}

std::string Utf8(hstring const& h) {
    return winrt::to_string(h);
}

std::string Base64(IBuffer const& buffer) {
    if (!buffer) return std::string();
    return Utf8(CryptographicBuffer::EncodeToBase64String(buffer));
}

// Map RadioState to our BleState. Falls back to a BluetoothAdapter check when
// no Bluetooth radio is present.
BleState QueryState() {
    try {
        auto adapter = BluetoothAdapter::GetDefaultAsync().get();
        if (!adapter) {
            return BleState::UnSupported;
        }
        auto radio = adapter.GetRadioAsync().get();
        if (!radio) {
            return BleState::UnSupported;
        }
        switch (radio.State()) {
            case RadioState::On:       return BleState::PoweredOn;
            case RadioState::Off:      return BleState::PoweredOff;
            case RadioState::Disabled: return BleState::PoweredOff;
            default:                   return BleState::Unknown;
        }
    } catch (winrt::hresult_error const& e) {
        LogError("QueryState failed: " + Utf8(e.message()));
        return BleState::Unknown;
    } catch (...) {
        return BleState::Unknown;
    }
}

std::string MakePeripheralJson(uint64_t addr, const std::string& name, int rssi) {
    std::ostringstream os;
    os << "{\"uuid\":\"" << AddressToString(addr) << "\","
       << "\"name\":\"" << JsonEscape(name) << "\","
       << "\"rssi\":" << rssi << "}";
    return os.str();
}

std::string CharFlagsToJson(const std::string& peripheralUuid,
                            const std::string& serviceUuid,
                            const std::string& charUuid,
                            GattCharacteristicProperties props) {
    bool canRead = (props & GattCharacteristicProperties::Read) != GattCharacteristicProperties::None;
    bool canWrite = (props & GattCharacteristicProperties::Write) != GattCharacteristicProperties::None;
    bool canWriteNoResp = (props & GattCharacteristicProperties::WriteWithoutResponse) != GattCharacteristicProperties::None;
    bool canNotify = ((props & GattCharacteristicProperties::Notify) != GattCharacteristicProperties::None) ||
                     ((props & GattCharacteristicProperties::Indicate) != GattCharacteristicProperties::None);

    std::ostringstream os;
    os << "{\"peripheralUUID\":\"" << peripheralUuid << "\","
       << "\"serviceUUID\":\"" << serviceUuid << "\","
       << "\"uuid\":\"" << charUuid << "\","
       << "\"value\":\"\","
       << "\"isReadable\":" << (canRead ? "true" : "false") << ","
       << "\"isWritable\":" << (canWrite ? "true" : "false") << ","
       << "\"isWritableWithoutResponse\":" << (canWriteNoResp ? "true" : "false") << ","
       << "\"isNotifiable\":" << (canNotify ? "true" : "false") << "}";
    return os.str();
}

std::shared_ptr<DeviceContext> FindDevice(const std::string& peripheralUuid) {
    uint64_t addr = StringToAddress(peripheralUuid);
    std::lock_guard<std::mutex> lock(g_devMutex);
    auto it = g_devices.find(addr);
    return (it != g_devices.end()) ? it->second : nullptr;
}

void RemoveDevice(uint64_t addr) {
    std::shared_ptr<DeviceContext> ctx;
    {
        std::lock_guard<std::mutex> lock(g_devMutex);
        auto it = g_devices.find(addr);
        if (it == g_devices.end()) return;
        ctx = it->second;
        g_devices.erase(it);
    }
    if (ctx && ctx->device) {
        try {
            if (ctx->connectionToken) {
                ctx->device.ConnectionStatusChanged(ctx->connectionToken);
            }
            for (auto& kv : ctx->valueChangedTokens) {
                auto cit = ctx->characteristics.find(kv.first);
                if (cit != ctx->characteristics.end()) {
                    try { cit->second.ValueChanged(kv.second); } catch (...) {}
                }
            }
            ctx->device.Close();
        } catch (...) {}
    }
}

bool IsActiveContext(uint64_t addr, std::shared_ptr<DeviceContext> const& ctx) {
    std::lock_guard<std::mutex> lock(g_devMutex);
    auto it = g_devices.find(addr);
    return it != g_devices.end() && it->second == ctx;
}

// Route an incoming characteristic value. If the app has subscribed, forward it
// immediately; otherwise buffer it so it can be flushed once the app subscribes.
// This is what lets us capture the GranBoard version message, which is sent
// right after connection (before the app gets a chance to subscribe).
void HandleIncomingValue(uint64_t addr, const std::string& charUuid, const std::string& b64) {
    std::shared_ptr<DeviceContext> ctx;
    {
        std::lock_guard<std::mutex> lock(g_devMutex);
        auto it = g_devices.find(addr);
        if (it != g_devices.end()) ctx = it->second;
    }
    if (!ctx) return;

    bool deliver = false;
    {
        std::lock_guard<std::mutex> lock(g_devMutex);
        auto sit = ctx->appSubscribed.find(charUuid);
        deliver = (sit != ctx->appSubscribed.end() && sit->second);
        if (!deliver) {
            ctx->pendingValues[charUuid].push_back(b64);
        }
    }
    if (deliver && g_onValueReceived) g_onValueReceived(charUuid.c_str(), b64.c_str());
}

// Register a ValueChanged handler and enable notifications/indications on a
// characteristic (idempotent). Values flow through HandleIncomingValue.
IAsyncAction EnableNotifications(std::shared_ptr<DeviceContext> ctx, GattCharacteristic ch, std::string charUuid, uint64_t addr) {
    {
        std::lock_guard<std::mutex> lock(g_devMutex);
        if (ctx->valueChangedTokens.count(charUuid)) co_return; // already enabled
    }

    auto token = ch.ValueChanged(
        [addr, charUuid](GattCharacteristic const&, GattValueChangedEventArgs const& args) {
            std::string b64 = Base64(args.CharacteristicValue());
            HandleIncomingValue(addr, charUuid, b64);
        });
    {
        std::lock_guard<std::mutex> lock(g_devMutex);
        ctx->valueChangedTokens[charUuid] = token;
    }

    auto props = ch.CharacteristicProperties();
    auto cccd = ((props & GattCharacteristicProperties::Notify) != GattCharacteristicProperties::None)
        ? GattClientCharacteristicConfigurationDescriptorValue::Notify
        : GattClientCharacteristicConfigurationDescriptorValue::Indicate;
    try {
        co_await ch.WriteClientCharacteristicConfigurationDescriptorAsync(cccd);
    } catch (winrt::hresult_error const& e) {
        LogError("EnableNotifications CCCD failed for " + charUuid + ": " + Utf8(e.message()));
    }
}

// ---------------------------------------------------------------------------
// Async operations (fire-and-forget coroutines; results via callbacks).
// ---------------------------------------------------------------------------
fire_and_forget ConnectAsync(uint64_t addr) {
    try {
        auto device = co_await BluetoothLEDevice::FromBluetoothAddressAsync(addr);
        if (!device) {
            LogError("Failed to obtain BluetoothLEDevice for " + AddressToString(addr));
            co_return;
        }

        auto ctx = std::make_shared<DeviceContext>();
        ctx->device = device;
        ctx->address = addr;

        // Detect disconnects (connect is signaled explicitly below).
        // BluetoothLEDevice fires a transient Disconnected while the connection
        // is still being established; reacting to it would Close() the device
        // and cancel the in-flight GATT operations. So only treat Disconnected
        // as real once we have actually signaled the connection (connected flag).
        ctx->connectionToken = device.ConnectionStatusChanged(
            [addr](BluetoothLEDevice const& sender, IInspectable const&) {
                if (sender.ConnectionStatus() != BluetoothConnectionStatus::Disconnected) {
                    return;
                }
                std::shared_ptr<DeviceContext> c;
                {
                    std::lock_guard<std::mutex> lock(g_devMutex);
                    auto it = g_devices.find(addr);
                    if (it != g_devices.end()) c = it->second;
                }
                if (!c || !c->connected.load()) {
                    return; // ignore transient pre-connection Disconnected
                }
                if (g_onDisconnected) g_onDisconnected(AddressToString(addr).c_str());
                RemoveDevice(addr);
            });

        {
            std::lock_guard<std::mutex> lock(g_devMutex);
            g_devices[addr] = ctx;
        }

        // Accessing GATT services forces the connection to be established.
        auto result = co_await device.GetGattServicesAsync(BluetoothCacheMode::Uncached);
        if (!IsActiveContext(addr, ctx)) {
            Log("ConnectAsync canceled before GATT services completed for " + AddressToString(addr));
            co_return;
        }

        if (result.Status() == GattCommunicationStatus::Success &&
            device.ConnectionStatus() == BluetoothConnectionStatus::Connected) {
            ctx->connected = true;

            // Now that the connection is established, hold a GattSession with
            // MaintainConnection so Windows keeps the BLE link up (otherwise it
            // tears the connection down shortly after GATT activity settles,
            // e.g. right after the first notification). Done after connecting so
            // it cannot interfere with establishing the connection itself.
            try {
                auto session = co_await GattSession::FromDeviceIdAsync(device.BluetoothDeviceId());
                if (!IsActiveContext(addr, ctx)) {
                    Log("ConnectAsync canceled before GattSession completed for " + AddressToString(addr));
                    co_return;
                }
                if (session) {
                    session.MaintainConnection(true);
                    ctx->session = session;
                }
            } catch (winrt::hresult_error const& e) {
                LogError("GattSession setup failed: " + Utf8(e.message()));
            }

            // Enumerate characteristics and pre-enable notifications NOW (before
            // signaling connected / discovery). The GranBoard sends its version
            // message right after connection; enabling notifications early and
            // buffering values ensures it is not missed before the app subscribes.
            for (auto const& service : result.Services()) {
                try {
                    auto chResult = co_await service.GetCharacteristicsAsync(BluetoothCacheMode::Uncached);
                    if (!IsActiveContext(addr, ctx)) {
                        Log("ConnectAsync canceled during characteristic discovery for " + AddressToString(addr));
                        co_return;
                    }
                    if (chResult.Status() == GattCommunicationStatus::Success) {
                        for (auto const& ch : chResult.Characteristics()) {
                            std::string charUuid = ToLower(GuidToString(ch.Uuid()));
                            {
                                std::lock_guard<std::mutex> lock(g_devMutex);
                                ctx->characteristics.insert_or_assign(charUuid, ch);
                            }
                            auto props = ch.CharacteristicProperties();
                            bool notifiable =
                                ((props & GattCharacteristicProperties::Notify) != GattCharacteristicProperties::None) ||
                                ((props & GattCharacteristicProperties::Indicate) != GattCharacteristicProperties::None);
                            if (notifiable) {
                                co_await EnableNotifications(ctx, ch, charUuid, addr);
                                if (!IsActiveContext(addr, ctx)) {
                                    Log("ConnectAsync canceled while enabling notifications for " + AddressToString(addr));
                                    co_return;
                                }
                            }
                        }
                    }
                } catch (...) {
                    LogError("Pre-enable characteristics failed for a service");
                }
            }

            if (!IsActiveContext(addr, ctx)) {
                Log("ConnectAsync canceled before connected callback for " + AddressToString(addr));
                co_return;
            }

            std::string name = Utf8(device.Name());
            if (g_onConnected) g_onConnected(MakePeripheralJson(addr, name, 0).c_str());
        } else {
            LogError("Connection to " + AddressToString(addr) + " did not establish. GATT status=" + std::to_string(static_cast<int>(result.Status())));
            RemoveDevice(addr);
        }
    } catch (winrt::hresult_error const& e) {
        LogError("ConnectAsync error: " + Utf8(e.message()));
        RemoveDevice(addr);
    } catch (...) {
        LogError("ConnectAsync unknown error");
        RemoveDevice(addr);
    }
}

fire_and_forget DiscoverServicesAsync(uint64_t addr) {
    auto ctx = [addr]() -> std::shared_ptr<DeviceContext> {
        std::lock_guard<std::mutex> lock(g_devMutex);
        auto it = g_devices.find(addr);
        return (it != g_devices.end()) ? it->second : nullptr;
    }();

    if (!ctx) {
        LogError("DiscoverServices: device not connected " + AddressToString(addr));
        co_return;
    }

    std::string peripheralUuid = AddressToString(addr);
    try {
        auto svcResult = co_await ctx->device.GetGattServicesAsync(BluetoothCacheMode::Cached);
        if (svcResult.Status() != GattCommunicationStatus::Success) {
            LogError("GetGattServicesAsync failed for " + peripheralUuid);
            co_return;
        }

        for (auto const& service : svcResult.Services()) {
            std::string serviceUuid = ToLower(GuidToString(service.Uuid()));

            std::ostringstream chars;
            chars << "[";
            bool first = true;

            try {
                auto chResult = co_await service.GetCharacteristicsAsync(BluetoothCacheMode::Cached);
                if (chResult.Status() == GattCommunicationStatus::Success) {
                    for (auto const& ch : chResult.Characteristics()) {
                        std::string charUuid = ToLower(GuidToString(ch.Uuid()));
                        {
                            std::lock_guard<std::mutex> lock(g_devMutex);
                            // Do not overwrite an entry stored during connect: that
                            // instance holds the ValueChanged registration. Only add
                            // if missing (emplace is a no-op when the key exists).
                            ctx->characteristics.emplace(charUuid, ch);
                        }
                        std::string charJson = CharFlagsToJson(peripheralUuid, serviceUuid, charUuid, ch.CharacteristicProperties());
                        if (!first) chars << ",";
                        chars << charJson;
                        first = false;
                    }
                }
            } catch (...) {
                LogError("GetCharacteristicsAsync failed for service " + serviceUuid);
            }

            chars << "]";

            std::ostringstream svcJson;
            svcJson << "{\"peripheralUUID\":\"" << peripheralUuid << "\","
                    << "\"uuid\":\"" << serviceUuid << "\","
                    << "\"description\":\"\","
                    << "\"characteristics\":" << chars.str() << "}";

            // Emit the service with its characteristics embedded. The C# layer
            // (WindowsBleService.FromDTO) populates service.Characteristics from
            // this, which the consumer enumerates synchronously. We deliberately
            // do NOT also raise per-characteristic callbacks here, since that
            // would add each characteristic a second time and cause duplicate
            // notification subscriptions.
            if (g_onService) g_onService(svcJson.str().c_str());
        }
    } catch (winrt::hresult_error const& e) {
        LogError("DiscoverServicesAsync error: " + Utf8(e.message()));
    } catch (...) {
        LogError("DiscoverServicesAsync unknown error");
    }
}

fire_and_forget ReadAsync(std::string peripheralUuid, std::string charUuidLower) {
    auto ctx = FindDevice(peripheralUuid);
    if (!ctx) { LogError("Read: device not found"); co_return; }

    GattCharacteristic ch{ nullptr };
    {
        std::lock_guard<std::mutex> lock(g_devMutex);
        auto it = ctx->characteristics.find(charUuidLower);
        if (it != ctx->characteristics.end()) ch = it->second;
    }
    if (!ch) { LogError("Read: characteristic not found " + charUuidLower); co_return; }

    try {
        auto result = co_await ch.ReadValueAsync(BluetoothCacheMode::Uncached);
        if (result.Status() == GattCommunicationStatus::Success) {
            std::string b64 = Base64(result.Value());
            if (g_onValueReceived) g_onValueReceived(charUuidLower.c_str(), b64.c_str());
        } else {
            LogError("ReadValueAsync failed for " + charUuidLower);
        }
    } catch (winrt::hresult_error const& e) {
        LogError("ReadAsync error: " + Utf8(e.message()));
    } catch (...) {
        LogError("ReadAsync unknown error");
    }
}

fire_and_forget WriteAsync(std::string peripheralUuid, std::string charUuidLower,
                           std::vector<uint8_t> data, bool withResponse) {
    auto ctx = FindDevice(peripheralUuid);
    if (!ctx) {
        if (g_onWriteCompleted) g_onWriteCompleted(charUuidLower.c_str(), -1, "device not found");
        co_return;
    }

    GattCharacteristic ch{ nullptr };
    {
        std::lock_guard<std::mutex> lock(g_devMutex);
        auto it = ctx->characteristics.find(charUuidLower);
        if (it != ctx->characteristics.end()) ch = it->second;
    }
    if (!ch) {
        if (g_onWriteCompleted) g_onWriteCompleted(charUuidLower.c_str(), -1, "characteristic not found");
        co_return;
    }

    try {
        auto buffer = CryptographicBuffer::CreateFromByteArray(
            array_view<uint8_t const>(data.data(), data.data() + data.size()));
        auto option = withResponse ? GattWriteOption::WriteWithResponse
                                   : GattWriteOption::WriteWithoutResponse;
        auto result = co_await ch.WriteValueWithResultAsync(buffer, option);
        if (result.Status() == GattCommunicationStatus::Success) {
            if (g_onWriteCompleted) g_onWriteCompleted(charUuidLower.c_str(), 0, "");
        } else {
            if (g_onWriteCompleted) g_onWriteCompleted(charUuidLower.c_str(), -1, "write failed");
        }
    } catch (winrt::hresult_error const& e) {
        if (g_onWriteCompleted) g_onWriteCompleted(charUuidLower.c_str(), -1, Utf8(e.message()).c_str());
    } catch (...) {
        if (g_onWriteCompleted) g_onWriteCompleted(charUuidLower.c_str(), -1, "unknown error");
    }
}

fire_and_forget SubscribeAsync(std::string peripheralUuid, std::string charUuidLower) {
    auto ctx = FindDevice(peripheralUuid);
    if (!ctx) { LogError("Subscribe: device not found"); co_return; }

    GattCharacteristic ch{ nullptr };
    bool needEnable = false;
    {
        std::lock_guard<std::mutex> lock(g_devMutex);
        auto it = ctx->characteristics.find(charUuidLower);
        if (it != ctx->characteristics.end()) ch = it->second;
        ctx->appSubscribed[charUuidLower] = true; // mark the app as wanting values
        needEnable = (ctx->valueChangedTokens.count(charUuidLower) == 0);
    }
    if (!ch) { LogError("Subscribe: characteristic not found " + charUuidLower); co_return; }

    try {
        // Notifications are normally pre-enabled at connect; enable here too in
        // case this characteristic was not enabled yet.
        if (needEnable) {
            co_await EnableNotifications(ctx, ch, charUuidLower, ctx->address);
        }

        // Flush any values buffered before the app subscribed (e.g. the version
        // message sent right after connection).
        std::vector<std::string> buffered;
        {
            std::lock_guard<std::mutex> lock(g_devMutex);
            auto pit = ctx->pendingValues.find(charUuidLower);
            if (pit != ctx->pendingValues.end()) {
                buffered = std::move(pit->second);
                ctx->pendingValues.erase(pit);
            }
        }
        for (auto const& b64 : buffered) {
            if (g_onValueReceived) g_onValueReceived(charUuidLower.c_str(), b64.c_str());
        }
    } catch (winrt::hresult_error const& e) {
        LogError("SubscribeAsync error: " + Utf8(e.message()));
    } catch (...) {
        LogError("SubscribeAsync unknown error");
    }
}

fire_and_forget UnsubscribeAsync(std::string peripheralUuid, std::string charUuidLower) {
    auto ctx = FindDevice(peripheralUuid);
    if (!ctx) co_return;

    GattCharacteristic ch{ nullptr };
    event_token token{};
    {
        std::lock_guard<std::mutex> lock(g_devMutex);
        auto it = ctx->characteristics.find(charUuidLower);
        if (it != ctx->characteristics.end()) ch = it->second;
        auto tit = ctx->valueChangedTokens.find(charUuidLower);
        if (tit != ctx->valueChangedTokens.end()) {
            token = tit->second;
            ctx->valueChangedTokens.erase(tit);
        }
        ctx->appSubscribed[charUuidLower] = false;
        ctx->pendingValues.erase(charUuidLower);
    }
    if (!ch) co_return;

    try {
        if (token) ch.ValueChanged(token);
        co_await ch.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue::None);
    } catch (winrt::hresult_error const& e) {
        LogError("UnsubscribeAsync error: " + Utf8(e.message()));
    } catch (...) {
        LogError("UnsubscribeAsync unknown error");
    }
}

void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher const&,
                             BluetoothLEAdvertisementReceivedEventArgs const& args) {
    uint64_t addr = args.BluetoothAddress();
    std::string name = Utf8(args.Advertisement().LocalName());

    // Name filter (exact match when provided).
    {
        std::lock_guard<std::mutex> lock(g_scanMutex);
        if (!g_nameFilter.empty() && name != g_nameFilter) {
            return;
        }
        if (!g_serviceFilter.empty()) {
            bool match = false;
            for (auto const& uuid : args.Advertisement().ServiceUuids()) {
                if (g_serviceFilter.count(ToLower(GuidToString(uuid))) > 0) {
                    match = true;
                    break;
                }
            }
            if (!match) return;
        }
        if (g_seen.count(addr) > 0) {
            return; // emit each device only once per scan session
        }
        g_seen.insert(addr);
    }

    int rssi = args.RawSignalStrengthInDBm();
    if (g_onPeripheralFound) {
        g_onPeripheralFound(MakePeripheralJson(addr, name, rssi).c_str());
    }
}

} // namespace

// ---------------------------------------------------------------------------
// Exported API.
// ---------------------------------------------------------------------------
UNITYBLE_API int UnityBLEWin_StartScanning(const char* serviceUuidsCsv, const char* nameFilter) {
    EnsureInit();

    BleState state = QueryState();
    if (state == BleState::UnSupported) return -3;
    if (state == BleState::UnAuthorized) return -2;
    if (state != BleState::PoweredOn) return -1;

    {
        std::lock_guard<std::mutex> lock(g_scanMutex);
        if (g_scanning) return 1;

        g_seen.clear();
        g_serviceFilter.clear();
        g_nameFilter.clear();

        if (serviceUuidsCsv && serviceUuidsCsv[0] != '\0') {
            std::stringstream ss(serviceUuidsCsv);
            std::string item;
            while (std::getline(ss, item, ',')) {
                if (!item.empty()) g_serviceFilter.insert(ToLower(item));
            }
        }
        if (nameFilter && nameFilter[0] != '\0') {
            g_nameFilter = nameFilter;
        }
    }

    try {
        g_watcher = BluetoothLEAdvertisementWatcher();
        g_watcher.ScanningMode(BluetoothLEScanningMode::Active);
        g_watcher.Received(OnAdvertisementReceived);
        g_watcher.Start();
        std::lock_guard<std::mutex> lock(g_scanMutex);
        g_scanning = true;
    } catch (winrt::hresult_error const& e) {
        LogError("StartScanning failed: " + Utf8(e.message()));
        return -1;
    }
    return 0;
}

UNITYBLE_API void UnityBLEWin_StopScanning() {
    try {
        if (g_watcher) g_watcher.Stop();
    } catch (...) {}
    std::lock_guard<std::mutex> lock(g_scanMutex);
    g_scanning = false;
}

UNITYBLE_API int UnityBLEWin_IsScanning() {
    std::lock_guard<std::mutex> lock(g_scanMutex);
    return g_scanning ? 1 : 0;
}

UNITYBLE_API int UnityBLEWin_GetState() {
    EnsureInit();
    return (int)QueryState();
}

UNITYBLE_API int UnityBLEWin_ConnectToPeripheral(const char* peripheralUUID) {
    EnsureInit();
    if (!peripheralUUID) return -1;
    ConnectAsync(StringToAddress(peripheralUUID));
    return 0;
}

UNITYBLE_API int UnityBLEWin_DisconnectFromPeripheral(const char* peripheralUUID) {
    if (!peripheralUUID) return -1;
    uint64_t addr = StringToAddress(peripheralUUID);
    RemoveDevice(addr);
    if (g_onDisconnected) g_onDisconnected(AddressToString(addr).c_str());
    return 0;
}

UNITYBLE_API int UnityBLEWin_DiscoverServices(const char* peripheralUUID) {
    if (!peripheralUUID) return -1;
    DiscoverServicesAsync(StringToAddress(peripheralUUID));
    return 0;
}

UNITYBLE_API int UnityBLEWin_ReadCharacteristic(const char* peripheralUUID, const char*, const char* characteristicUUID) {
    if (!peripheralUUID || !characteristicUUID) return -1;
    ReadAsync(peripheralUUID, ToLower(characteristicUUID));
    return 0;
}

UNITYBLE_API int UnityBLEWin_WriteCharacteristic(const char* peripheralUUID, const char*, const char* characteristicUUID,
                                                 const unsigned char* data, int length, int withResponse) {
    if (!peripheralUUID || !characteristicUUID) return -1;
    std::vector<uint8_t> bytes;
    if (data && length > 0) bytes.assign(data, data + length);
    WriteAsync(peripheralUUID, ToLower(characteristicUUID), std::move(bytes), withResponse != 0);
    return 0;
}

UNITYBLE_API int UnityBLEWin_SubscribeToCharacteristic(const char* peripheralUUID, const char*, const char* characteristicUUID) {
    if (!peripheralUUID || !characteristicUUID) return -1;
    SubscribeAsync(peripheralUUID, ToLower(characteristicUUID));
    return 0;
}

UNITYBLE_API int UnityBLEWin_UnsubscribeFromCharacteristic(const char* peripheralUUID, const char*, const char* characteristicUUID) {
    if (!peripheralUUID || !characteristicUUID) return -1;
    UnsubscribeAsync(peripheralUUID, ToLower(characteristicUUID));
    return 0;
}

// --- Callback registration ---
UNITYBLE_API void UnityBLEWin_registerOnPeripheralDiscovered(PeripheralFoundCb cb) { g_onPeripheralFound = cb; }
UNITYBLE_API void UnityBLEWin_registerOnPeripheralConnected(ConnectedCb cb) { g_onConnected = cb; }
UNITYBLE_API void UnityBLEWin_registerOnPeripheralDisconnected(DisconnectedCb cb) { g_onDisconnected = cb; }
UNITYBLE_API void UnityBLEWin_registerOnBleErrorDetected(BleErrorCb cb) { g_onError = cb; }
UNITYBLE_API void UnityBLEWin_registerOnBleStateChanged(BleStateChangedCb cb) { g_onStateChanged = cb; }
UNITYBLE_API void UnityBLEWin_registerOnDiscoveredPeripheralCleared(ClearedCb cb) { g_onCleared = cb; }
UNITYBLE_API void UnityBLEWin_registerOnDiscoveredServices(ServiceFoundCb cb) { g_onService = cb; }
UNITYBLE_API void UnityBLEWin_registerOnDiscoveredCharacteristics(CharacteristicFoundCb cb) { g_onCharacteristic = cb; }
UNITYBLE_API void UnityBLEWin_registerOnLog(LogCb cb) { g_onLog = cb; }
UNITYBLE_API void UnityBLEWin_registerOnWriteCharacteristicCompleted(WriteCompletedCb cb) { g_onWriteCompleted = cb; }
UNITYBLE_API void UnityBLEWin_registerOnReadRSSICompleted(ReadRssiCb cb) { g_onReadRssi = cb; }
UNITYBLE_API void UnityBLEWin_registerOnValueReceived(ValueReceivedCb cb) { g_onValueReceived = cb; }
