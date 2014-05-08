using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using JSON_ns;
using System.Diagnostics;

namespace ARI_POC
{
    public partial class SelectA11y : System.Web.UI.Page
    {
        private string fMessage = null;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.RequestType.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    BrowserSession.Get(Context).A11y = JSON.parse(Request.Form["a11y"]);
                    Response.Redirect(Request.Form["nexturl"]);
                }
                catch (Exception err)
                {
                    fMessage = err.Message;
                }
            }
        }

        string fA11yJson;
        protected string AccessibilityJSON
        {
            get
            {
                if (fA11yJson == null)
                {
                    fA11yJson = JSON.stringify(BrowserSession.Get(Context).A11y);
                }
                return fA11yJson;
            }
        }

        protected string Message { get { return fMessage; } }
    }
}