using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Routing;
using System.Web.Configuration;

namespace ARI_POC
{

    public class Global : System.Web.HttpApplication
    {
        public static readonly string ItemUrlName = "item";
        public static readonly string ScoreUrlName = "score";
        public static readonly string ItemFilename = "item.ejs";
        public static readonly string ScoreFilename = "score.ejs";

        private static readonly string sPackagePathKey = "TestPackagePath";
        private static readonly string sDefaultPackagePath = "~/SamplePackage";
        private static string fPackagePath;
        public static string PackagePath
        {
            get
            {
                if (fPackagePath == null)
                {
                    string packagePath =
                        WebConfigurationManager.AppSettings[sPackagePathKey];
                    if (packagePath == null) packagePath = sDefaultPackagePath;
                    fPackagePath = (packagePath[0] == '~')
                        ? HttpContext.Current.Server.MapPath(VirtualPathUtility.ToAbsolute(packagePath))
                        : packagePath;
                }
                return fPackagePath;
            }
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        /*
        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
        */
    }
}