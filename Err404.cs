using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ARI_POC
{
    // Callable from anywhere, not just Err404.aspx
    public static class Err404
    {
        static readonly string sErr404Body =
@"<!DOCTYPE html>
<html> 
<head> 
    <title>404.0 - Not Found</title> 
</head> 
<body> 
  <h3 style=""color: darkred;"">HTTP Error 404.0 - Not Found</h3> 
  <h4>The resource you are looking for does not exist.</h4> 
</body> 
</html> 
";

        public static void RespondAndEnd(HttpResponse response)
        {
            response.StatusCode = 404;
            response.StatusDescription = "Not Found";
            response.ContentType = "text/html";
            response.Write(sErr404Body);
            response.Flush();
            response.End();
        }

    }
}