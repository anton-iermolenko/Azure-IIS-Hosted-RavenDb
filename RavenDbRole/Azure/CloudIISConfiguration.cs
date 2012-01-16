namespace RavenDbRole.Azure
{
    using System;
    using System.Linq;
    using System.IO;
    using System.Threading;
    using Microsoft.Web.Administration;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using NLog;

    public class CloudIISConfiguration
    {
        static Logger Logger = LogManager.GetCurrentClassLogger();

        public static void Configure(string webApplicationProjectName, bool noAppPoolTimeout = true, bool bindingWorksForAllUnassignedIPs = false)
        {
            if (!RoleEnvironment.IsAvailable)
                return;

            var siteName = RoleEnvironment.CurrentRoleInstance.Id + "_" + webApplicationProjectName;
            Logger.Info("Fine tunning IIS on the cloud for following site: {0}", siteName);

            int tryCount = 0;
            bool successullySaved = false;
            do
            {
                try
                {
                    using (var serverManager = new ServerManager())
                    {
                        var site = serverManager.Sites[siteName];

                        if (noAppPoolTimeout)
                        {
                            var siteApplication = serverManager.Sites[siteName].Applications.First();
                            var appPoolName = siteApplication.ApplicationPoolName;

                            var appPool = serverManager.ApplicationPools[appPoolName];

                            appPool.ProcessModel.IdleTimeout = TimeSpan.Zero;
                            appPool.Recycling.PeriodicRestart.Time = TimeSpan.Zero;
                        }

                        if (bindingWorksForAllUnassignedIPs)
                        {
                            var binding = site.Bindings.First();
                            site.Bindings.Add(string.Format("*:{0}:", binding.EndPoint.Port), binding.Protocol);
                        }

                        serverManager.CommitChanges();
                        successullySaved = true;
                    }
                }
                catch (FileLoadException ex)
                {
                    Logger.DebugException("Couldn't save IIS changes", ex);
                    Thread.Sleep(1000);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Logger.ErrorException("IIS changes cannot be applied due to permissions", ex);
                    // No need to try anymore
                    break;
                }
            } while (!successullySaved && ++tryCount < 3);

            if (!successullySaved)
            {
                Logger.Warn("Couldn't succesfully make IIS changes in the cloud for {0}. This doesn't mean that they were not actually applied", siteName);
            }
        }
    }
}