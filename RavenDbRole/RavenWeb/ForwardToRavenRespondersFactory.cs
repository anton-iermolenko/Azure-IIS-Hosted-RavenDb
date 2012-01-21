//-----------------------------------------------------------------------
// <copyright file="ForwardToRavenRespondersFactory.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Web;
using System.Web.Hosting;
using Raven.Database;
using Raven.Database.Config;
using Raven.Database.Server;

namespace Raven.Web
{
    using System.Configuration;
    using System.IO;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using RavenDbRole.WebRole;

    public class ForwardToRavenRespondersFactory : IHttpHandlerFactory
    {
        internal static DocumentDatabase database;
        internal static HttpServer server;
        private static readonly object locker = new object();

        public class ReleaseRavenDBWhenAppDomainIsTornDown : IRegisteredObject
        {
            public void Stop(bool immediate)
            {
                Shutdown();
                HostingEnvironment.UnregisterObject(this);
            }
        }

        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            if (database == null)
            {
                throw new InvalidOperationException("Database has not been initialized properly");
            }
            return new ForwardToRavenResponders(server);
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
        }

        public static void Init()
        {
            if (database != null)
                return;

            lock (locker)
            {
                if (database != null)
                    return;

                try
                {
                    var ravenConfiguration = new RavenConfiguration();
                    if (RoleEnvironment.IsAvailable)
                    {
                        // Mount Cloud drive and set it as Data Directory
                        var currentConfiguredRavenDataDir = ConfigurationManager.AppSettings["Raven/DataDir"] ?? string.Empty;
                        string azureDrive = Environment.GetEnvironmentVariable(RavenDriveConfiguration.AzureDriveEnvironmentVariableName, EnvironmentVariableTarget.Machine);
                        if (string.IsNullOrWhiteSpace(azureDrive))
                        {
                            throw new ArgumentException("RavenDb drive environment variable is not yet set by worker role. Please, retry in a couple of seconds");
                        }

                        string azurePath = Path.Combine(azureDrive, 
                            currentConfiguredRavenDataDir.StartsWith(@"~\")
                                ? currentConfiguredRavenDataDir.Substring(2)
                                : "Data");
                        ravenConfiguration.DataDirectory = azurePath;

                        // Read port number specified for this Raven instance and set it in configuration
                        var endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["Raven"];
                        ravenConfiguration.Port = endpoint.IPEndpoint.Port;

                        // When mounting drives in emulator only Munin storage is supported, since drive is not actually present and low level access to it failes (Esent mode)
                    }
                    HttpEndpointRegistration.RegisterHttpEndpointTarget();
                    database = new DocumentDatabase(ravenConfiguration);
                    database.SpinBackgroundWorkers();
                    server = new HttpServer(ravenConfiguration, database);
                    server.Init();
                }
                catch
                {
                    if (database != null)
                    {
                        database.Dispose();
                        database = null;
                    }
                    if (server != null)
                    {
                        server.Dispose();
                        server = null;
                    }
                    throw;
                }

                HostingEnvironment.RegisterObject(new ReleaseRavenDBWhenAppDomainIsTornDown());
            }
        }

        public static void Shutdown()
        {
            lock (locker)
            {
                if (server != null)
                    server.Dispose();

                if (database != null)
                    database.Dispose();

                server = null;
                database = null;
            }
        }
    }
}