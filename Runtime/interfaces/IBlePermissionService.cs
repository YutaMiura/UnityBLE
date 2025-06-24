using System.Threading.Tasks;

namespace UnityBLE
{
    public interface IBlePermissionService
    {
        /// <summary>
        /// Checks if the necessary permissions for BLE operations are granted.
        /// </summary>
        /// <returns>True if permissions are granted, false otherwise.</returns>
        bool ArePermissionsGranted();

        /// <summary>
        /// Requests the necessary permissions for BLE operations.
        /// </summary>
        /// <returns>True if permissions were successfully requested, false otherwise.</returns>
        Task<bool> RequestPermissionsAsync();
    }
}