using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using Microsoft.JScript;

namespace ARI_POC
{
    /// <summary>
    /// The class that implements the "ari_s" object that is passed into EJScript when
    /// rendering items.
    /// </summary>
    public class Ari_s_Object
    {
        TextWriter fWriter;
        string fPathToPackage;
        Uri fIntraPackageUri;

        public object Obj1 { get; set; }

        public Ari_s_Object(TextWriter writer, string pathToPackage, string intraPackagePath)
        {
            fWriter = writer;
            a11y = new JSObject();
            fPathToPackage = pathToPackage;
            if (intraPackagePath[0] != '/') throw new ArgumentException("intraPackagePath must begin with '/'");
            fIntraPackageUri = new Uri(new Uri("http://fakehost/"), intraPackagePath); // The path relative to the package (e.g. /items/item-42/main.ejs)
        }

        // Properties used from JScript

        /// <summary>
        /// Accessibility property
        /// </summary>
        public JSObject a11y;

        /// <summary>
        /// The student response (to be scored)
        /// </summary>
        public JSObject response;

        /// <summary>
        /// Accepts the score when scoring.
        /// </summary>
        public Object score;

        // Methods called from JScript

        public void write(string str)
        {
            fWriter.Write(str);
        }

        public string toLocalPath(string packagePath)
        {
            Uri uri = new Uri(fIntraPackageUri, packagePath);   // Resolves relative path to absolute within the package.
            string absolutePath = uri.AbsolutePath;
            return string.Concat(fPathToPackage, absolutePath);
        }


    }
}