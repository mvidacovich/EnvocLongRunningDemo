using Owin;

namespace Envoc.AzureLongRunningTask.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}