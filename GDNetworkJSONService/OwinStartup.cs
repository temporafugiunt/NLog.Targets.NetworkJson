using Microsoft.Owin.Cors;
using Owin;

namespace GDNetworkJSONService
{
    class OwinStartup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
}
