using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Text;
using System.IO;
using Microsoft.JScript;
using JSON_ns;

namespace ARI_POC
{
    public partial class ScoreItem : System.Web.UI.Page
    {
       string fPathToPackage;      // Relative to application
        string fItemId;
        string fPackageFilename;    // Relative to package
        string fPackageFoldername;  // Relative to package
        string fName;
        string fDescription;
        string fMessage;
        string fScoreOutput;
        string fScoreValue;

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
            fPackageFilename = string.Concat(fPackageFoldername, "/", Global.ScoreFilename);
            string filename = string.Concat(Global.PackagePath, "\\items\\", Page.RouteData.Values["itemFolder"], "\\", Global.ScoreFilename);
            string itemFilename = string.Concat(Global.PackagePath, "\\items\\", Page.RouteData.Values["itemFolder"], "\\", Global.ItemFilename);

            fMessage = null;
            fScoreOutput = string.Empty;
            fScoreValue = string.Empty;
            try
            {
                // Get the metadata from the item ejs
                {
                    FileTypeId fileTypeId;
                    SortedDictionary<string, string> metadata;
                    FileMeta.GetFileTypeAndMetadata(itemFilename, out fileTypeId, out metadata);
                    if (!metadata.TryGetValue("name", out fName)) fName = "(unnamed)";
                    if (!metadata.TryGetValue("description", out fDescription)) fDescription = "(no description)";
                }

                // Compile the score metadata
                EJScript script;
                using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    FileTypeId fileTypeId;
                    SortedDictionary<string, string> metadata;
                    FileMeta.GetFileTypeAndMetadata(stream, out fileTypeId, out metadata);
                    if (fileTypeId != FileTypeId.ejs)
                    {
                        throw new ApplicationException("Scoring Script is not EJS.");
                    }

                    FilenameResolver fnr = new FilenameResolver(Global.PackagePath, fPackageFilename);
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
                    {
                        script = EJScript.CompileEjs(reader, fnr.OpenReader, fPackageFilename);
                    }
                }

                // Execute the script to score the item
                {
                    BrowserSession brs = BrowserSession.Get(Context);
                    UTF8Encoding utf8NoBom = new UTF8Encoding(false);
                    using (StringWriter writer = new StringWriter())
                    {
                        Ari_s_Object ari_s = new Ari_s_Object(writer, fPathToPackage, fPackageFilename);
                        ari_s.a11y = brs.A11y;
                        ari_s.response = brs.Responses[fItemId];
                        script.Run(ari_s);
                        writer.Flush();

                        if (ari_s.score == null)
                        {
                            fScoreValue = string.Empty;
                            fMessage = "Failed to set valid score value.";
                        }
                        else if (ari_s.score is double)
                        {
                            double d = (double)ari_s.score;
                            fScoreValue = d.ToString();
                        }
                        else if (ari_s.score is JSObject)
                        {
                            fScoreValue = JSON.stringify((JSObject)ari_s.score);
                        }
                        else
                        {
                            fScoreValue = string.Empty;
                            fMessage = string.Format("Invalid score type. Expected number or JavaScript Object. Found '{0}'.", ari_s.score.GetType().Name);
                        }
                        fScoreOutput = writer.ToString();
                    }
                }
            }
            catch (EJSParseException err)
            {
                fMessage = err.Message;
            }
            catch (Exception err)
            {
                fMessage = err.ToString();
            }
        }

        protected string ItemName { get { return fName; } }
        protected string ItemDescription { get { return fDescription; } }
        protected string PathToPackage { get { return fPathToPackage; } }  // Relative to the application
        protected string PackageFoldername { get { return fPackageFoldername; } } // Relative to the package
        protected string PackageFilename { get { return fPackageFilename; } } // Relative to the package
        protected string ItemId { get { return fItemId; } }
        protected string Message { get { return fMessage; } }
        protected string ScoreValue { get { return fScoreValue; } }
        protected string ScoreOutput { get { return fScoreOutput; } }
        protected string PathToItem { get { return string.Concat(VirtualPathUtility.ToAbsolute("~/tstpkg/items/"), fItemId, "/", Global.ItemUrlName); } }

        private void ProcessPost()
        {
            throw new ApplicationException("Unexpected post!");
        }
    }
}