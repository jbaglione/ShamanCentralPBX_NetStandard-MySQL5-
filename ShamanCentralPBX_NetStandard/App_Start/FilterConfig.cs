using System.Web;
using System.Web.Mvc;

namespace ShamanCentralPBX_NetStandard
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
