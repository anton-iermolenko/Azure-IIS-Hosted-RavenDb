namespace RavenDbRole.Implementation
{
    using System;
    using System.Configuration;

    public class RavenDriveConfiguration
    {
        public string CloudContainerName { get; private set; }
        public string CloudDriveName { get; private set; }
        public int CloudDriveSize { get; private set; }
        public int CloudDriveReadCacheSize { get; private set; }

        private RavenDriveConfiguration()
        {}

        public static RavenDriveConfiguration Get()
        {
            string containerName = ConfigurationManager.AppSettings["Raven.Data.CloudContainerName"];
            string driveName = ConfigurationManager.AppSettings["Raven.Data.CloudDriveName"];
            string driveSize = ConfigurationManager.AppSettings["Raven.Data.CloudDriveSize"];
            string readCacheSize = ConfigurationManager.AppSettings["Raven.Data.CloudDriveReadCacheSize"];

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