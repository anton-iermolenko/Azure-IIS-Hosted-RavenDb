namespace RavenDbRole.Implementation
{
    using System.Web;

    public class ForwardToRavenRespondersFactory : IHttpHandlerFactory
    {
        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            var httpServer = Global.RavenServer;
            return new ForwardToRavenResponders(httpServer);
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
        }
    }
}