using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ARI_POC
{
    /// <summary>
    /// Determines the type of a file from it's header.
    /// </summary>
    /// <remarks>
    /// In keeping with the FileMeta Manifesto (see http://www.filemeta.org/manifesto) file types
    /// should be determined by their contents, not by their container or by their reference.
    /// When necessary, resorts to the filename extension for backward compatibility.
    /// </remarks>
    // 
    // So far, supports the followign file types with their corresponding headers and extensions:
    // 
    //  header        | extension | type
    //  ----------------------------------------
    //  <%#ejs        | .ejs      | application/x-embeddedjavascript
    //  /*javascript  | .js       | application/javascript
    //  <?xml         | .xml      | text/xml
    //
    // JSON presents a problem (as, likely, will many other formats) because it doesn't define any
    // kind of header other than the opening brace. For the moment, I'm leaving it off the list
    // bbecause it's not needed in this application. Despite heavy use of JSON, it's noever stored
    // to disk. However, so long as no other format opens with a single brace with no other distinguishing
    // features, that may be sufficient to add JSON.

    public enum FileTypeId
    {
        unknown = 0,
        ejs = 1,
        js = 2,
        xml = 3
    };

    /// <summary>
    /// Retrieves file type and metadata information from files. Presently only
    /// identifies JavaScript, Embedded JavaScript (EJS), and XML files and only reads metadata
    /// from JavaScript and Embedded JavaScript.
    /// </summary>
    /// <remarks>
    /// <para>This would be an amazing class if it worked with more than a limited set
    /// of file types. However, it remains useful for the set with which it work
    /// and open for extension.
    /// </para>
    /// <para>File type identification and metadata requires that files meet certain requirements:
    /// </para>
    /// <para>JavaScript: The file MUST begin with the text "/*javascript" followed by a newline.
    /// This file-type declaration is cases-sensitive and must be all lower-case. Because "/*" is
    /// the beginning of a multiline comment in JavaScript this line and subsequent lines until
    /// the comment end will be ignored by other JavaScript.
    /// </para>
    /// <para>The declaration line (above) SHOULD be followed by one or more lines of metadata
    /// in "microYAML" format (see below). The metadata is concluded by a line containing only
    /// the close-comment sequence "*/
    /// </para>
    /// <para>Embedded JavaScript: The file MUST begin with the text "&lt;%#ejs" followed by a
    /// newline. This file-type declaration is case-sensitive and must be all lower-case. The
    /// declaration is followed by one or mor lines of metadata in "microYAML" format (see
    /// below). The metadata is concluded by the close-declaration sequence "%>".
    /// </para>
    /// <para>microYAML: The metadata uses a simplified subset of YAML inspired by but different
    /// from the the work at: https://code.google.com/p/mini-yaml-parser/. It is a line-oriented
    /// format and follows this syntax:
    /// </para>
    /// <para>key: literal    -- Specifies a key and the literal value that follows.</para>
    /// <para>key: |          -- Specifies a key followed by a multiline literal.</para>
    /// <para> some text      -- All text following the multiline literal that's indented more than the key is part of the literal.</para>
    /// <para># text          -- Lines beginning with '#' are comments and ignored.</para>
    /// <para>*/              -- End of document (for JavaScript).</para>
    /// <para>%>              -- End of document (for EJS).</para>
    /// <para>                -- Anything else is a syntax error that ends the metatadata and is silently ignored.</para>
    /// <para>Keys must start with a letter and be composed of letters, </para>
    /// <para>microYAML is a proper subset of YAML (except for document beginning and
    /// ending syntax). Additional features of YAML could be added in the future.
    /// </para>
    /// </remarks>
    public static class FileMeta
    {
        const int cMaxHeaderBytes = 32;
        static readonly string sEjs_Header = "<%#ejs";
        static readonly string sJs_Header = "/*javascript";
        static readonly string sXml_Header = "<?xml";

        static bool sStrict = true;

        /// <summary>
        /// If strict is true, file type header is required and types will not be inferred from the file extension.
        /// </summary>
        public static bool Strict
        {
            get { return sStrict; }
            set { sStrict = value; }
        }

        public static FileTypeId GetFileTypeFromHeader(string header)
        {
            // When more types are added, we can optimize this using a hard-coded boolean search
            // Unfortunately, a typical hash won't work because we don't know how many characters to use.
            if (header.StartsWith(sEjs_Header)) return FileTypeId.ejs;
            if (header.StartsWith(sJs_Header)) return FileTypeId.js;
            if (header.StartsWith(sXml_Header)) return FileTypeId.xml;
            return FileTypeId.unknown;
        }

        public static FileTypeId GetFileTypeFromExtension(string filename)
        {
            switch(Path.GetExtension(filename).ToLowerInvariant())
            {
                case ".ejs":
                    return FileTypeId.ejs;
                case ".js":
                    return FileTypeId.js;
                case "xml":
                    return FileTypeId.xml;
                default:
                    return FileTypeId.unknown;
            }
        }

        public static FileTypeId GetFileType(byte[] header, int offset, int len)
        {
            using (StreamReader reader = new StreamReader(new MemoryStream(header, 0, len), Encoding.UTF8, true, cMaxHeaderBytes, false))
            {
                return GetFileTypeFromHeader(reader.ReadToEnd());
            }
        }

        public static FileTypeId GetFileType(Stream stream)
        {
            long pos = stream.Position;
            byte[] buf = new byte[cMaxHeaderBytes];
            int bufLen = stream.Read(buf, 0, cMaxHeaderBytes);
            stream.Position = pos;
            return GetFileType(buf, 0, bufLen);
        }

        public static FileTypeId GetFileType(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, cMaxHeaderBytes))
            {
                FileTypeId typeId = GetFileType(stream);
                return (sStrict || typeId != FileTypeId.unknown) ? typeId : GetFileTypeFromExtension(filename);
            }
        }

        public static TextReader GetReaderAndFileType(string filename, out FileTypeId rFileType)
        {
            FileStream stream = null;
            try
            {
                stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                FileTypeId typeId = GetFileType(stream);
                if (!sStrict && typeId == FileTypeId.unknown)
                {
                    typeId = GetFileTypeFromExtension(filename);
                }

                rFileType = typeId;
                StreamReader reader = new StreamReader(stream, Encoding.UTF8, true);
                stream = null;
                return reader;
            }
            finally
            {
                if (stream != null) stream.Dispose();
            }
        }

        /* This regular expression limits mini-YAML acceptance exclusively to lines that
         * have keys and values that meet YAML "Plain Style". Since this doesn't support
         * character escaping it limits the values that can be expressed. Nevertheless, it's
         * sufficient for current needs. If those needs change, the syntax can be extended
         * to use additional YAML features.
         * This regex assumes that leading and trailing whitespace has alrady been trimmed.
         */
        private static readonly Regex sYamlPlain = new Regex(@"^(?<key>[a-zA-Z_][a-zA-Z0-9_]*):[ \t]+(?<value>(\w.*)|(\|)|())\z");

        private static string TrimAndCount(string txt, out int rIndent)
        {
            int indent = 0;
            while (indent < txt.Length && (txt[indent] == ' ' || txt[indent] == '\t')) ++indent;
            int end = txt.Length;
            while (end > indent && (txt[end-1] == ' ' || txt[end-1] == '\t')) --end;
            if (indent > 0 || end < txt.Length) txt = txt.Substring(indent, end - indent);
            rIndent = indent;
            return txt;
        }


        public static SortedDictionary<string, string> ParseMicroYaml(TextReader reader)
        {
            SortedDictionary<string, string> dict = new SortedDictionary<string, string>();

            // Since YAML is a line-oriented format we just read line-by-line and treat each line accordingly.

            // Read the first line
            int indent;
            string line = reader.ReadLine();
            if (line == null) line = "*/"; // Fake end-of-document
            line = TrimAndCount(line, out indent);

            for (; ; )
            {
                if (line.Length == 0 || line[0] == '#')
                {
                    // Do nothing
                }
                else if (line.StartsWith("*/") || line.StartsWith("%>"))
                {
                    break;  // End of document
                }
                else
                {
                    Match match = sYamlPlain.Match(line);
                    if (!match.Success)
                    {
                        Debug.WriteLine("microYAML syntax error: '{0}'", line);
                        break;  // Just consider this to be the end of input.
                    }
                    string key = match.Groups["key"].Value;
                    string value = match.Groups["value"].Value;
                    if (!value.Equals("|", StringComparison.Ordinal)) // Single-line value
                    {
                        dict[key] = value;
                    }
                    else
                    {
                        value = string.Empty;
                        for (; ; )
                        {
                            int indent2;
                            line = reader.ReadLine();
                            if (line == null) line = "*/";  // Fake end-of-document;
                            line = TrimAndCount(line, out indent2);
                            if (line.Length == 0 || indent2 > indent)
                            {
                                value = string.Concat(value, line, "\r\n");
                            }
                            else
                            {
                                dict[key] = value;
                                indent = indent2;
                                break ;
                            }
                        }
                        continue;
                    }
                }

                // Load the next line
                line = reader.ReadLine();
                if (line == null) break;    // end of document
                line = TrimAndCount(line, out indent);
            }

            return dict;
        }

        /// <summary>
        /// Gets the file type and reads the metadata from a file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="rTypeId"></param>
        /// <param name="rMetadata"></param>
        public static void GetFileTypeAndMetadata(Stream stream, out FileTypeId rTypeId, out SortedDictionary<string, string> rMetadata)
        {
            FileTypeId typeId = GetFileType(stream);
            SortedDictionary<string, string> metadata;

            if (typeId == FileTypeId.ejs || typeId == FileTypeId.js)
            {
                long pos = stream.Position;
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 256, true))
                {
                    reader.ReadLine();  // Skip the filetype indicator line
                    metadata = ParseMicroYaml(reader);
                }
                stream.Position = pos;
            }
            else
            {
                metadata = new SortedDictionary<string, string>();  // Empty metadata
            }
            rTypeId = typeId;
            rMetadata = metadata;
        }

        /// <summary>
        /// Gets the file type and reads the metadata from a file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="rTypeId"></param>
        /// <param name="rMetadata"></param>
        public static void GetFileTypeAndMetadata(string filename, out FileTypeId rTypeId, out SortedDictionary<string, string> rMetadata)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                GetFileTypeAndMetadata(stream, out rTypeId, out rMetadata);
            }
        }

    }

}