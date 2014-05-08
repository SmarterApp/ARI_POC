using System;
using System.Collections.Generic;
using System.Web;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.JScript;
using JSON_ns;

namespace ARI_POC
{
    public partial class RenderItem : System.Web.UI.Page
    {

        static readonly string cCssColorDefault = ".ariA11y {color:black;background-color:white;}";
        static Dictionary<string, string> sColorsetToCss;
        static RenderItem()
        {
            sColorsetToCss = new Dictionary<string, string>();
            sColorsetToCss.Add("blackOnWhite", cCssColorDefault);
            sColorsetToCss.Add("blackOnRose", ".ariA11y {color:black;background-color:#FFD0FF;}");
            sColorsetToCss.Add("yellowOnBlue", ".ariA11y {color:#FFCC00;background-color:#003399;");
            sColorsetToCss.Add("mediumGrayOnLightGray", ".ariA11y {color:#707070;background-color:#E5E5E5;");
            sColorsetToCss.Add("reverseContrast", ".ariA11y {color:white;background-color:black");
        }

        string fPathToPackage;      // Relative to application
        string fItemId;
        string fPackageFilename;    // Relative to package
        string fPackageFoldername;  // Relative to package
        string fName;
        string fDescription;
        string fMessage;
        EJScript fScript;

        protected void Page_Load(object sender, EventArgs e)
        {
            fPathToPackage = VirtualPathUtility.ToAbsolute("~/tstpkg");
            if (Request.RequestType.Equals("POST", StringComparison.OrdinalIgnoreCase)) ProcessPost();

            fItemId = Page.RouteData.Values["itemFolder"] as string;
            if (string.IsNullOrEmpty(fItemId))
            {
#if DEBUG
                Response.Redirect(VirtualPathUtility.ToAbsolute("~/SelectA11y"));
#else
                Err404.RespondAndEnd(Response);
#endif
            }
            fPackageFoldername = string.Concat("/items/", fItemId);
            fPackageFilename = string.Concat(fPackageFoldername, "/", Global.ItemFilename);
            string filename = string.Concat(Global.PackagePath, "\\items\\", Page.RouteData.Values["itemFolder"], "\\", Global.ItemFilename);

            try
            {
                using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    FileTypeId fileTypeId;
                    SortedDictionary<string, string> metadata;
                    FileMeta.GetFileTypeAndMetadata(stream, out fileTypeId, out metadata);
                    if (fileTypeId != FileTypeId.ejs)
                    {
                        throw new ApplicationException("Item is not EJS.");
                    }
                    if (!metadata.TryGetValue("name", out fName)) fName = "(unnamed)";
                    if (!metadata.TryGetValue("description", out fDescription)) fDescription = "(no description)";

                    FilenameResolver fnr = new FilenameResolver(Global.PackagePath, fPackageFilename);
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
                    {
                        fScript = EJScript.CompileEjs(reader, fnr.OpenReader, fPackageFilename);
                    }
                }
            }
            catch (EJSParseException err)
            {
                fScript = null;
                fMessage = err.Message;
            }
            catch (FileNotFoundException err)
            {
                if (string.Equals(err.FileName, filename, StringComparison.OrdinalIgnoreCase))
                {
                    Err404.RespondAndEnd(Response);
                }
                throw;
            }
        }

        protected string ItemName { get { return fName; } }
        protected string ItemDescription { get { return fDescription; } }
        protected string PathToPackage { get { return fPathToPackage; } }  // Relative to the application
        protected string PackageFoldername { get { return fPackageFoldername; } } // Relative to the package
        protected string PackageFilename { get { return fPackageFilename; } } // Relative to the package
        protected string ItemId { get { return fItemId; } }
        protected string Message { get { return fMessage; } }

        private JSObject fA11y;
        public JSObject A11y
        {
            get
            {
                if (fA11y == null)
                {
                    fA11y = BrowserSession.Get(Context).A11y;
                }
                return fA11y;
            }
        }

        private string fA11yJson;
        public string A11yJson
        {
            get
            {
                if (fA11yJson == null)
                {
                    fA11yJson = JSON.stringify(A11y);
                }
                return fA11yJson;
            }
        }

        private JSObject fItemResponse;
        public JSObject ItemResponse
        {
            get
            {
                if (fItemResponse == null)
                {
                    if (!BrowserSession.Get(Context).Responses.TryGetValue(fItemId, out fItemResponse)) fItemResponse = new JSObject();
                }
                return fItemResponse;
            }
            
        }

        private string fItemResponseJson;
        public string ItemResponseJson
        {
            get
            {
                if (fItemResponse == null)
                {
                    fItemResponseJson = JSON.stringify(ItemResponse);
                }
                return fItemResponseJson;
            }
        }

        private string fA11yColorCss;
        public string A11yColorCss
        {
            get
            {
                if (fA11yColorCss == null)
                {
                    string colorset = A11y["colorset"] as string;
                    if (colorset == null || !sColorsetToCss.TryGetValue(A11y["colorset"].ToString(), out fA11yColorCss))
                    {
                        fA11yColorCss = cCssColorDefault;
                    }
                }
                return fA11yColorCss;
            }
        }

        public void Render()
        {
            if (fScript == null) return;
            UTF8Encoding utf8NoBom = new UTF8Encoding(false);
            using (StreamWriter writer = new StreamWriter(Response.OutputStream, utf8NoBom, 512, true))
            {
                Ari_s_Object item = new Ari_s_Object(writer, fPathToPackage, fPackageFilename);
                item.a11y = A11y;
                item.response = ItemResponse;
                if (item.response == null) item.response = new JSObject();
                fScript.Run(item);
            }
        }

        private void ProcessPost()
        {
            string itemId = Request.Form["itemid"];
            string response = Request.Form["response"];
            string nextUrl = Request.Form["nexturl"];
            if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(response) || string.IsNullOrEmpty(nextUrl)) throw new ArgumentException("Invalid Item Submission");
            BrowserSession.Get(Context).Responses[itemId] = JSON.parse(response);
            if (nextUrl == "ScoreItem") nextUrl = string.Concat(fPathToPackage, "/items/", itemId, "/", Global.ScoreUrlName);
            Response.Redirect(nextUrl);
        }
    }
}