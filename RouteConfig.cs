using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.FriendlyUrls;


namespace ARI_POC
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapPageRoute("RenderItem", "tstpkg/items/{itemfolder}/" + Global.ItemUrlName, "~/RenderItem.aspx");
            routes.MapPageRoute("ScoreItem", "tstpkg/items/{itemfolder}/" + Global.ScoreUrlName, "~/ScoreItem.aspx");
            routes.Add("ItemResources", new Route("tstpkg/items/{itemfolder}/res/{*respath}", new TransmitResourceHandler()));
            routes.Add("CommonResources", new Route("tstpkg/common-res/{*respath}", new TransmitResourceHandler()));
            routes.MapPageRoute("Err404", "tstpkg/{*tail}", "~/Err404.aspx");   // Catch all other test package URLs with a 404 error
            routes.EnableFriendlyUrls();
        }
    }
}