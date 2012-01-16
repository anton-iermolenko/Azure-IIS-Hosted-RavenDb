namespace RavenDbRole.WebRole
{
    using System;
    using System.Linq;
    using Azure;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using System.Threading;
    using NLog;

    public class RavenDbRole : RoleEntryPoint
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly CancellationTokenSource _source = new CancellationTokenSource();
        private bool _startedProperly = true;

        public override bool OnStart()
        {
            Logger.Info("Raven starting...");

            try
            {
                ValidateInstanceCount();

                var driveConfiguration = RavenDriveConfiguration.Get();
                string driveRoot;

                // In emulated environment Azure doesn't actually mount to drive letters and uses some hacky ways to redirect reads to location on disk
                if (!RoleEnvironment.IsEmulated)
                {
                    // Allocate total read cache for all Azure drives to be mounted as part of this role (maximum cache size)
                    // Provided value is the name of Local Storage from Azure's Role configuration
                    AzureCloudDrive.PrepareLocalReadCache("CloudDriveLocalReadCache");

                    // Mount drive that will be stored in Container with certain blob Name, given drive size (in MB) and read cache size (subset of maximum cache size allocated above)
                    driveRoot = AzureCloudDrive.MountDrive(driveConfiguration.CloudContainerName,
                                                               driveConfiguration.CloudDriveName,
                                                               driveConfiguration.CloudDriveSize,
                                                               driveConfiguration.CloudDriveReadCacheSize);
                }
                else
                {
                    // Use allocated read cache as if it was Azure drive
                    driveRoot = RoleEnvironment.GetLocalResource("CloudDriveLocalReadCache").RootPath;
                }
                Environment.SetEnvironmentVariable(RavenDriveConfiguration.AzureDriveEnvironmentVariableName, driveRoot, EnvironmentVariableTarget.Machine);

                CloudIISConfiguration.Configure("Web", bindingWorksForAllUnassignedIPs: true);
                RoleEnvironment.Changed += RoleEnvironment_Changed;
            }
            catch (Exception e)
            {
                Logger.FatalException("Raven couldn't start properly", e);
                _startedProperly = false;
            }

            return base.OnStart();
        }

        public override void Run()
        {
            Logger.Info("Raven worker role started. Running...");

            try
            {
                if (_startedProperly)
                    _source.Token.WaitHandle.WaitOne();
            }
            catch (Exception ex)
            {
                Logger.FatalException("Raven cannot run properly", ex);
            }
        }

        public override void OnStop()
        {
            try
            {
                Logger.Info("Raven stopping...");

                try
                {
                    Environment.SetEnvironmentVariable(RavenDriveConfiguration.AzureDriveEnvironmentVariableName, null, EnvironmentVariableTarget.Machine);

                    if (!RoleEnvironment.IsEmulated)
                    {
                        var driveConfiguration = RavenDriveConfiguration.Get();
                        // Unmount to unlock the blob
                        AzureCloudDrive.UnmountDrive(driveConfiguration.CloudContainerName,
                                                     driveConfiguration.CloudDriveName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WarnException("Cloud drive couldn't be unmounted", ex);
                }

                _source.Cancel(true);
                base.OnStop();
            }
            catch (Exception ex)
            {
                Logger.FatalException("Raven couldn't stop properly", ex);
            }
        }

        void ValidateInstanceCount()
        {
            if (RoleEnvironment.CurrentRoleInstance.Role.Instances.Count >= 2)
            {
                throw new InvalidOperationException("Raven can only as single instance per role. Even if replication is setup -> it won't work because of Azure's round-robin load balancing");
            }
        }

        void RoleEnvironment_Changed(object sender, RoleEnvironmentChangedEventArgs e)
        {
            if (!e.Changes.OfType<RoleEnvironmentTopologyChange>().Any())
            {
                return;
            }

            ValidateInstanceCount();
        }
    }
}
