namespace RavenDbRole.WebRole
{
    using System;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class RavenDriveConfiguration
    {
        public const string AzureDriveEnvironmentVariableName = "AzureMountedDrive";

        public string CloudContainerName { get; private set; }
        public string CloudDriveName { get; private set; }
        public int CloudDriveSize { get; private set; }
        public int CloudDriveReadCacheSize { get; private set; }

        private RavenDriveConfiguration()
        {}

        public static RavenDriveConfiguration Get()
        {
            string containerName = RoleEnvironment.GetConfigurationSettingValue("Raven.Data.CloudContainerName");
            string driveName = RoleEnvironment.GetConfigurationSettingValue("Raven.Data.CloudDriveName");
            string driveSize = RoleEnvironment.GetConfigurationSettingValue("Raven.Data.CloudDriveSize");
            string readCacheSize = RoleEnvironment.GetConfigurationSettingValue("Raven.Data.CloudDriveReadCacheSize");

            int driveSizeValue;
            int readCacheSizeValue;
            if (string.IsNullOrWhiteSpace(containerName)
                || string.IsNullOrWhiteSpace(driveName)
                || !int.TryParse(driveSize, out driveSizeValue)
                || !int.TryParse(readCacheSize, out readCacheSizeValue))
            {
                throw new ArgumentException("Raven drive settings were found, but some or all have empty/invalid values");
            }

            var result = new RavenDriveConfiguration
                             {
                                 CloudContainerName = containerName,
                                 CloudDriveName = driveName,
                                 CloudDriveReadCacheSize = readCacheSizeValue,
                                 CloudDriveSize = driveSizeValue
                             };

            return result;
        }
    }
}