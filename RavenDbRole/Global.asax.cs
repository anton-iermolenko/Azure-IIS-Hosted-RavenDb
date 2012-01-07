namespace RavenDbRole
{
    using System;
    using System.Web;
    using System.Configuration;
    using System.IO;
    using Implementation;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using NLog;
    using Raven.Database;
    using Raven.Database.Config;
    using Raven.Database.Server;

    public class Global : HttpApplication
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static HttpServer RavenServer;
        public static DocumentDatabase Database;

        protected void Application_Start(object sender, EventArgs e)
        {
            Configure();
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            Configure();
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {
            Cleanup();
        }

        void Configure()
        {
            var obj = Application["AppConfigured"];

            if (obj != null)
                return;

            try
            {
                InitRavenServer();

                Application["AppConfigured"] = true;
            }
            catch (Exception ex)
            {
                Logger.FatalException("RavenDb couldn't be properly configured", ex);
                throw;
            }
        }

        private void InitRavenServer()
        {
            var ravenConfiguration = new RavenConfiguration();
            if (RoleEnvironment.IsAvailable)
            {
                // Mount Cloud drive and set it as Data Directory
                var currentConfiguredRavenDataDir = ConfigurationManager.AppSettings["Raven/DataDir"] ?? string.Empty;
                ravenConfiguration.DataDirectory = GetAzureHostedDataDirectory(currentConfiguredRavenDataDir);

                // Read port number specified for this Raven instance and set it in configuration
                var endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["Raven"];
                ravenConfiguration.Port = endpoint.IPEndpoint.Port;
            }

            HttpEndpointRegistration.RegisterHttpEndpointTarget();
            Database = new DocumentDatabase(ravenConfiguration);
            Database.SpinBackgroundWorkers();
            RavenServer = new HttpServer(ravenConfiguration, Database);
        }

        private string GetAzureHostedDataDirectory(string configurationProvidedDataDir)
        {
            var driveConfiguration = RavenDriveConfiguration.Get();

            // Allocate total read cache for all Azure drives to be mounted as part of this role (maximum cache size)
            // Provided value is the name of Local Storage from Azure's Role configuration
            AzureCloudDrive.PrepareLocalReadCache("CloudDriveLocalReadCache");

            // Mount drive that will be stored in Container with certain blob Name, given drive size (in MB) and read cache size (subset of maximum cache size allocated above)
            var driveRoot = AzureCloudDrive.MountDrive(driveConfiguration.CloudContainerName, driveConfiguration.CloudDriveName, 
                driveConfiguration.CloudDriveSize, driveConfiguration.CloudDriveReadCacheSize);

            // If Data directory name and relative path was set it web.config -> use it, but route from newly mounted drive root
            string result = Path.Combine(driveRoot, 
                configurationProvidedDataDir.StartsWith(@"~\") 
                    ? configurationProvidedDataDir.Substring(2) 
                    : "Data");
            return result;
        }

        private void Cleanup()
        {
            try
            {
                if (RavenServer != null)
                    RavenServer.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WarnException("Raven server couldn't be disposed", ex);
            }

            try
            {
                if (Database != null)
                    Database.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WarnException("Raven database couldn't be disposed", ex);
            }

            try
            {
                if (RoleEnvironment.IsAvailable)
                {
                    var driveConfiguration = RavenDriveConfiguration.Get();
                    // Unmount to unlock the blob
                    AzureCloudDrive.UnmountDrive(driveConfiguration.CloudContainerName, driveConfiguration.CloudDriveName);
                }
            }
            catch (Exception ex)
            {
                Logger.WarnException("Cloud drive couldn't be unmounted", ex);
            }
        }
    }
}
