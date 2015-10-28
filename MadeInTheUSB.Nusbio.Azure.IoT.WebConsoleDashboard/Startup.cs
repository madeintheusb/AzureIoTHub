using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MadeInTheUSB.Nusbio.Azure.IoT.WebConsoleDashboard.Startup))]
namespace MadeInTheUSB.Nusbio.Azure.IoT.WebConsoleDashboard
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
