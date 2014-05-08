using System;
using System.Collections.Generic;
using System.Web;
using Microsoft.JScript;
using JSON_ns;

namespace ARI_POC
{
    /// <summary>
    /// Manage session information.
    /// </summary>
    /// <remarks>
    /// A proper design would keep all session information either in the browser (in the form of
    /// cookies and URLs) or in the database. This makes load balancing and server redundancy
    /// much more robust. This demo app (which doesn't have a database) uses the ASP.Net session
    /// class. The interface is abstracted in case something real is made out of this proof
    /// of concept.
    /// </remarks>
    public class BrowserSession
    {
        static readonly string cSessionKey = "ariSession";

        public static BrowserSession Get(HttpContext context)
        {
            BrowserSession session = context.Session[cSessionKey] as BrowserSession;
            if (session == null)
            {
                session = new BrowserSession();
                context.Session[cSessionKey] = session;
            }
            return session;
        }

        private BrowserSession()
        {
            A11y = new JSObject();
            Responses = new Dictionary<string,JSObject>();
        }

        public JSObject A11y { get; set; }

        public Dictionary<string, JSObject> Responses { get; private set; }

    }
}