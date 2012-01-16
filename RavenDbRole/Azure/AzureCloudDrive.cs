namespace RavenDbRole.Azure
{
    using System;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Microsoft.WindowsAzure.StorageClient;
    using NLog;

    public class AzureCloudDrive
    {
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void PrepareLocalReadCache(string localResourceName)
        {
            // Get a reference to the local resource.
            LocalResource localResource = RoleEnvironment.GetLocalResource(localResourceName);
            if (localResource == null)
            {
                throw new ArgumentException("Local resource not found");
            }

            // Initialize the drive cache.
            CloudDrive.InitializeCache(localResource.RootPath, localResource.MaximumSizeInMegabytes);
        }

        public static string MountDrive(string containerName, string vhdName, int driveSize, int driveLocalReadCacheSize)
        {
            var client = GetCloudClientInstance();

            // Create the container for the drive if it does not already exist.
            var container = new CloudBlobContainer(containerName, client);
            if (container.CreateIfNotExist())
            {
                container.SetPermissions(new BlobContainerPermissions {PublicAccess = BlobContainerPublicAccessType.Off});
            }

            var cloudDrive = new CloudDrive(container.GetPageBlobReference(vhdName).Uri, client.Credentials);
            try
            {
                cloudDrive.Create(driveSize);
            }
            catch (CloudDriveException ex)
            {
                Logger.Info(string.Format("Cloud drive already exists. Uri: {0}", cloudDrive.Uri), ex);
            }

            string pathToDrive = cloudDrive.Mount(driveLocalReadCacheSize, DriveMountOptions.Force);
            return pathToDrive;
        }

        public static void UnmountDrive(string containerName, string vhdName)
        {
            var client = GetCloudClientInstance();
            var vhdBlob = client.GetContainerReference(containerName).GetPageBlobReference(vhdName);

            var cloudDrive = new CloudDrive(vhdBlob.Uri, client.Credentials);
            cloudDrive.Unmount();
        }

        private static CloudBlobClient GetCloudClientInstance()
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            var client = cloudStorageAccount.CreateCloudBlobClient();
            return client;
        }
    }
}