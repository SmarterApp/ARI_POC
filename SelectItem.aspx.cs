using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Diagnostics;

namespace ARI_POC
{
    public partial class SelectItem : System.Web.UI.Page
    {
        protected class ItemEntry
        {
            public ItemEntry(string title, string href)
            {
                Title = title;
                Href = href;
            }
            public string Title;
            public string Href;
        }

        protected List<ItemEntry> fItems;

        protected void Page_Load(object sender, EventArgs e)
        {
            fItems = new List<ItemEntry>();

            string testPackagePath = VirtualPathUtility.ToAbsolute("~/tstpkg/");

            // List all of the items
            DirectoryInfo itemsDir = new DirectoryInfo(Path.Combine(Global.PackagePath, "items"));
            foreach (DirectoryInfo itemDir in itemsDir.EnumerateDirectories())
            {
                FileInfo mainFile = new FileInfo(Path.Combine(itemDir.FullName, Global.ItemFilename));
                if (mainFile.Exists)
                {
                    FileTypeId fileTypeId;
                    SortedDictionary<string, string> fileMetadata;
                    FileMeta.GetFileTypeAndMetadata(mainFile.FullName, out fileTypeId, out fileMetadata);
#if DEBUG
                    Debug.WriteLine(mainFile.FullName);
                    foreach(var entry in fileMetadata)
                    {
                        Debug.WriteLine("   {0}: {1}", entry.Key, entry.Value);
                    }
#endif
                    string title;
                    if (fileMetadata.TryGetValue("title", out title))
                    {
                        fItems.Add(new ItemEntry(title, string.Concat(testPackagePath, "items/", itemDir.Name, "/", Global.ItemUrlName)));
                    }
#if DEBUG
                    else
                    {
                        Debug.WriteLine("  No title found for item. Excluded from the list.");
                    }
#endif
                }
            }
            

        }
    }
}