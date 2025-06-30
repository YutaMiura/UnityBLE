using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace UnityBLE.Editor
{
    /// <summary>
    /// iOS build post processor for UnityBLE.
    /// Automatically configures required frameworks and Info.plist entries for iOS builds.
    /// </summary>
    public class iOSBuildPostProcessor
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                Debug.Log("[iOSBuildPostProcessor] Starting iOS build post-processing for UnityBLE...");
                
                try
                {
                    ConfigureXcodeProject(buildPath);
                    ConfigureInfoPlist(buildPath);
                    
                    Debug.Log("[iOSBuildPostProcessor] iOS build post-processing completed successfully");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[iOSBuildPostProcessor] Error during post-processing: {ex.Message}");
                    throw;
                }
            }
        }

        private static void ConfigureXcodeProject(string buildPath)
        {
            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            PBXProject project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projectPath));

            // Get the target GUID
            string targetGuid = project.GetUnityMainTargetGuid();
            
            // Add required frameworks for Core Bluetooth
            project.AddFrameworkToProject(targetGuid, "CoreBluetooth.framework", false);
            project.AddFrameworkToProject(targetGuid, "Foundation.framework", false);
            
            Debug.Log("[iOSBuildPostProcessor] Added CoreBluetooth and Foundation frameworks to Xcode project");

            // Write the modified project back
            File.WriteAllText(projectPath, project.WriteToString());
        }

        private static void ConfigureInfoPlist(string buildPath)
        {
            string infoPlistPath = Path.Combine(buildPath, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(infoPlistPath));

            PlistElementDict root = plist.root;

            // Add Bluetooth usage description
            const string bluetoothUsageKey = "NSBluetoothAlwaysUsageDescription";
            const string bluetoothUsageDescription = "This app uses Bluetooth to connect to BLE devices for data communication.";
            
            if (!root.values.ContainsKey(bluetoothUsageKey))
            {
                root.SetString(bluetoothUsageKey, bluetoothUsageDescription);
                Debug.Log($"[iOSBuildPostProcessor] Added {bluetoothUsageKey} to Info.plist");
            }
            else
            {
                Debug.Log($"[iOSBuildPostProcessor] {bluetoothUsageKey} already exists in Info.plist");
            }

            // Add Bluetooth peripheral usage description (for iOS 13+)
            const string bluetoothPeripheralUsageKey = "NSBluetoothPeripheralUsageDescription";
            const string bluetoothPeripheralUsageDescription = "This app uses Bluetooth to communicate with BLE peripheral devices.";
            
            if (!root.values.ContainsKey(bluetoothPeripheralUsageKey))
            {
                root.SetString(bluetoothPeripheralUsageKey, bluetoothPeripheralUsageDescription);
                Debug.Log($"[iOSBuildPostProcessor] Added {bluetoothPeripheralUsageKey} to Info.plist");
            }
            else
            {
                Debug.Log($"[iOSBuildPostProcessor] {bluetoothPeripheralUsageKey} already exists in Info.plist");
            }

            // Ensure required device capabilities are set
            PlistElementArray requiredDeviceCapabilities;
            if (root.values.ContainsKey("UIRequiredDeviceCapabilities"))
            {
                requiredDeviceCapabilities = root["UIRequiredDeviceCapabilities"].AsArray();
            }
            else
            {
                requiredDeviceCapabilities = root.CreateArray("UIRequiredDeviceCapabilities");
            }

            // Add bluetooth-le capability if not present
            bool hasBluetoothLE = false;
            foreach (var capability in requiredDeviceCapabilities.values)
            {
                if (capability.AsString() == "bluetooth-le")
                {
                    hasBluetoothLE = true;
                    break;
                }
            }

            if (!hasBluetoothLE)
            {
                requiredDeviceCapabilities.AddString("bluetooth-le");
                Debug.Log("[iOSBuildPostProcessor] Added bluetooth-le to UIRequiredDeviceCapabilities");
            }
            else
            {
                Debug.Log("[iOSBuildPostProcessor] bluetooth-le already exists in UIRequiredDeviceCapabilities");
            }

            // Write the modified plist back
            File.WriteAllText(infoPlistPath, plist.WriteToString());
        }
    }
}