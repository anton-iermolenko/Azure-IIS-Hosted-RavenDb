namespace RavenDbRole
{
    using System;
    using System.Linq;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using System.Threading;
    using Implementation;
    using NLog;

    public class WebRole : RoleEntryPoint
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
