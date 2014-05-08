using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ARI_POC
{
    /// <summary>
    /// Resolves filenames relative to a root path and a context path.
    /// </summary>
    public class FilenameResolver
    {
        /// <summary>
        /// Root path (in Microsoft format).
        /// "/" refers to here.
        /// Will not resolve paths outside this one.
        /// </summary>
        string fRootPath;

        /// <summary>
        /// Context path (in URL/Unix format).
        /// This is a path into the virtual directory rooted in fRootPath.
        /// Used for relative path resolution (e.g. "..\phred.txt")
        /// Must begin and end with slashes
        /// </summary>
        Uri fContextUri;

        /// <summary>
        /// Constructs a filename resolver with a simple root
        /// </summary>
        /// <param name="rootPath">The root path (in Microsoft format)</param>
        public FilenameResolver(string rootPath)
        {
            fRootPath = Path.GetFullPath(rootPath);
            fContextUri = new Uri("http://fakehost/");
        }

        /// <summary>
        /// Constructs a filename resolver with a root and a path relative to that root
        /// </summary>
        /// <param name="rootPath">Root path in Microsoft format. No relative paths can move below this root.</param>
        /// <param name="contextPath">Path or filename relative to the root. Relative paths will be resolved relative to this.</param>
        public FilenameResolver(string rootPath, string contextPath)
        {
            fRootPath = Path.GetFullPath(rootPath);
            contextPath = contextPath.Replace('\\', '/');
            if (contextPath[0] != '/') contextPath = string.Concat("/", contextPath);
            contextPath = contextPath.Substring(0, contextPath.LastIndexOf('/') + 1);
            fContextUri = new Uri(new Uri("http://fakehost/"), contextPath);
        }

        /// <summary>
        /// Convert a path that's local to the FilenameResolver context into an absolution path.
        /// </summary>
        /// <param name="localPath">The local path to resolve.</param>
        /// <returns>The absolute filename.</returns>
        public string GetFullPath(string localPath)
        {
            // This is done in two steps in order to prevent the local path from
            // using "/" or "../../.." to move below fRootPath.

            Uri uri = new Uri(fContextUri, localPath);
            string absolutePath = uri.AbsolutePath;
            absolutePath = absolutePath.Substring(1, absolutePath.Length-1);
            return Path.Combine(fRootPath, absolutePath).Replace('/', '\\');
        }

        /// <summary>
        /// Open a reader on a path that's local to FilenameResolver context.
        /// </summary>
        /// <param name="localPath">The local path to open.</param>
        /// <returns>A StreamReader on the local path.</returns>
        /// <remarks>
        /// This matches the EJScript.OpenIncludeFileDelegate prototype.
        /// </remarks>
        public TextReader OpenReader(string localPath, out FileTypeId rFileTypeId)
        {
            return FileMeta.GetReaderAndFileType(GetFullPath(localPath), out rFileTypeId);
        }
    }
}