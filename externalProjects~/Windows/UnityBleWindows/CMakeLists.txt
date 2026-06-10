cmake_minimum_required(VERSION 3.20)
project(UnityBleWindows CXX)

# C++/WinRT requires C++17 or later. We use C++20 so the standard <coroutine>
# header is available, which keeps the clang/clang-cl build path simple
# (no -fcoroutines-ts flag needed).
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Build a single shared library that Unity loads as a native plugin.
add_library(UnityBleWindows SHARED src/UnityBleWindows.cpp)

target_compile_definitions(UnityBleWindows PRIVATE
    WIN32_LEAN_AND_MEAN
    NOMINMAX
    UNICODE
    _UNICODE)

# WindowsApp.lib is the umbrella import library that exposes the WinRT APIs
# (Bluetooth, Radios, Storage.Streams, Security.Cryptography) to desktop apps.
target_link_libraries(UnityBleWindows PRIVATE WindowsApp.lib)

set_target_properties(UnityBleWindows PROPERTIES
    OUTPUT_NAME "UnityBleWindows"
    PREFIX "")
