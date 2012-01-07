namespace RavenDbRole.Implementation
{
    using System.Web;
    using Raven.Database.Server;
    using Raven.Database.Server.Abstractions;

    public class ForwardToRavenResponders : IHttpHandler
    {
        private readonly HttpServer _server;

        public ForwardToRavenResponders(HttpServer server)
        {
            _server = server;
        }

        public void ProcessRequest(HttpContext context)
        {
            _server.HandleActualRequest(new HttpContextAdapter(context, _server.Configuration));
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}