using Microsoft.Owin.Cors;
using Owin;

namespace GDNetworkJSONService
{
    class OwinStartup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.Run(context =>
            {
                context.Response.ContentType = "text/plain";
                return context.Response.WriteAsync("Diagnostics Info should go here!");
            });
        }
    }
}
