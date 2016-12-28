using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PnPTemplateManager.Web.Startup))]
namespace PnPTemplateManager.Web
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
