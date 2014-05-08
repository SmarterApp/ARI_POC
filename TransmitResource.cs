using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Diagnostics;
using System.IO;

namespace ARI_POC
{
    /// <summary>
    /// Transmits a resource from the test package.
    /// </summary>
    /// <remarks>
    /// This class does not enforce security to prevent transmission of resources
    /// in the srv-res branch. That enforcement is done through the proper use of
    /// routes in the RouteConfig file.
    /// </remarks>
    public class TransmitResource : IHttpHandler
    {
        static readonly string sTstPkg = "/tstpkg";

        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext context)
        {
            HttpRequest request = context.Request; // for convenience
            HttpResponse response = context.Response;

            string path = request.Path;
            if (request.ApplicationPath.Length > 1 && path.StartsWith(request.ApplicationPath)) path = path.Substring(request.ApplicationPath.Length);
            if (!path.StartsWith(sTstPkg))
            {
                Debug.Fail("TransmitResource for resource not in test package.");
                Err404.RespondAndEnd(response);
            }
            string filename = String.Concat(Global.PackagePath, path.Substring(sTstPkg.Length).Replace('/', '\\'));
            if (!File.Exists(filename)) Err404.RespondAndEnd(response);

            response.StatusCode = 200;
            response.StatusDescription = "OK";
            response.TransmitFile(filename);
        }
    }

    public class TransmitResourceHandler : IRouteHandler
    {
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new TransmitResource();
        }
    }
}