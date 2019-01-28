using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ShamanCentralPBX_NetStandard.Startup))]
namespace ShamanCentralPBX_NetStandard
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //ConfigureAuth(app);
        }
    }
}
