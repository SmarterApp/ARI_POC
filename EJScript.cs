#define INSERT_DEBUG_IN_OUTPUT
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Microsoft.JScript;
using System.CodeDom.Compiler;
using System.Reflection;

namespace ARI_POC
{
    public class EJScript
    {
        const int cMinHeaderBytes = 8;
        static readonly char[] sEjs_Header = { '<', '%', '#', ' ', 'e', 'j', 's', ' ' };
        static readonly char[] sJs_Header = { '/', '*', ' ', 'j', 's', ' ' };

        static readonly string sEjs_Class = "__ejsItem";
        static readonly string sEjs_Function = "__Run";

        static readonly string sEJS_Header = "import JSON_ns;class __ejsItem{public function __Run(ari_s) : void{";
        static readonly string sEJS_Footer = "}}\r\n";

        static readonly string sEJS_Literal = "ari_s.write(\"";
        static readonly string sEJS_LiteralEnd = "\");\r\n";
        static readonly string sEJS_Write = "ari_s.write(";
        static readonly string sEJS_WriteEnd = ");\r\n";

        private class JsWriter
        {
            private string fFilename = string.Empty;     // Curent inbound filename
            private int fLineNumber = -1;                // Current outbound line number
            private TextWriter fWriter;

            public JsWriter(TextWriter writer)
            {
                fWriter = writer;
            }

            public TextWriter DetachTextWriter()
            {
                TextWriter writer = fWriter;
                fWriter = null;
                return writer;
            }

            public void TrackInput(string filename, int lineNumber)
            {
                fWriter.WriteLine(); // Ensure we're at the beginning of a line
                if (fLineNumber >= 0) fWriter.WriteLine("@set @position(end)");
                fWriter.WriteLine("@set @position( file=\"{0}\"; line = {1} )", filename, lineNumber);
                fFilename = filename;
                fLineNumber = lineNumber;
            }

            // Track line numbering
            private void TrackLine(int inLn)
            {
                Debug.Assert(fLineNumber >= 0); // First position has been set
                int diff = inLn - fLineNumber;
                if (diff == 0) return;
                if (diff < -4 || diff > 4)
                {
                    fWriter.WriteLine("@set @position(end)");
                    fWriter.WriteLine("@set @position( file=\"{0}\" line = {1} )", fFilename, inLn);
                    fLineNumber = inLn;
                }
                else
                {
                    while (fLineNumber < inLn)
                    {
                        fWriter.WriteLine();    // Write blank lines to catch up
                        ++fLineNumber;
                    }
                }
            }

            public void Write(int inLineNum, char charToWrite)
            {
                // Write the text
                fWriter.Write(charToWrite);

                // Count and record line numbers
                if (charToWrite == '\n')
                {
                    ++fLineNumber;
                    TrackLine(inLineNum);
                }
            }

            public void Write(int inLineNum, char[] buf, int index, int count)
            {
                // Write while recording line numbers
                int end = index + count;
                int anchor = index;
                for (int i=index; i<end; ++i)
                {
                    if (buf[i] == '\n')
                    {
                        fWriter.Write(buf, anchor, (i - anchor) + 1);
                        ++fLineNumber;
                        TrackLine(inLineNum);
                        anchor = i + 1;
                    }
                }
                // Write the rest
                if (anchor < end) fWriter.Write(buf, anchor, end - anchor);
            }

            public void Write(int inLineNum, string textToWrite)
            {
                char[] buf = textToWrite.ToCharArray();
                Write(inLineNum, buf, 0, buf.Length);
            }

            // Include an inbound javascript file
            public void IncludeLiteral(TextReader reader, string inFilename, JsWriter writer)
            {
                const int cBufSize = 1024;
                char[] buf = new char[cBufSize];

                // Track position (to emit the position directive)
                fWriter.WriteLine();    // Make sure we're on a clean line break
                TrackInput(inFilename, 1);
                
                // Copy literally
                for (;;)
                {
                    int charsRead = reader.Read(buf, 0, cBufSize);
                    if (charsRead <= 0) break;
                    fWriter.Write(buf, 0, charsRead);
                }
            }

        } // class JsWriter

        private class EjsScanner
        {
            const int cBufSize = 2048;

            TextReader fReader;

            char[] fBuf = new char[cBufSize];
            int fBi = 0; // buffer index
            int fBe = 0; // buffer end
            bool fAtEnd = false;

            // For debugging
            string fInFilename;
            int fInLn = 1;

            public EjsScanner(TextReader reader, string inFilename, int inLineNum)
            {
                fReader = reader;
                fInFilename = inFilename;
                fInLn = inLineNum;
            }

            public int InLineNo
            {
                get { return fInLn; }
            }

            public string InFilename
            {
                get { return fInFilename; }
            }

            public bool AtEnd
            {
                get { return fBi >= fBe && fAtEnd; }
            }

            void RefillBuf()
            {
                if (fAtEnd) return;

                Buffer.BlockCopy(fBuf, fBi * sizeof(char), fBuf, 0, (fBe - fBi) * sizeof(char));
                fBe -= fBi;
                fBi = 0;
                int charsRead = fReader.Read(fBuf, fBe, cBufSize - fBe);
                if (charsRead == 0)
                {
                    Debug.Assert(fBe < cBufSize);
                    // Null pad for lookahead
                    fBuf[fBe] = '\0';
                    fBuf[fBe + 1] = '\0';
                    fAtEnd = true;
                }
                fBe += charsRead;
            }

            public void Skip(int count)
            {
                while (count > 0)
                {
                    if (fBi >= fBe)
                    {
                        RefillBuf();
                        if (fBi >= fBe) return;
                    }

                    while (count > 0 && fBi < fBe)
                    {
                        if (fBuf[fBi] == '\n') ++fInLn;
                        --count;
                        ++fBi;
                    }
                }
            }

            public void SkipWhitespace()
            {
                for (;;)
                {
                    if (fBi >= fBe)
                    {
                        RefillBuf();
                        if (fBi >= fBe) return;
                    }

                    while (fBi < fBe && char.IsWhiteSpace(fBuf[fBi]))
                    {
                        if (fBuf[fBi] == '\n') ++fInLn;
                        ++fBi;
                    }

                    if (fBi < fBe || fAtEnd) break;
                }
            }

            public void SkipUntilCloseDelimiter()
            {
                do
                {
                    // Refill buffer if fewer than two chars
                    if (!fAtEnd && fBe - fBi < 2)
                    {
                        RefillBuf();
                    }

                    // We can look ahead one character because the buffer refill null-pads
                    if (fBuf[fBi] == '%' || fBuf[fBi + 1] == '>') break;

                    // Count newlines
                    if (fBuf[fBi] == '\n') ++fInLn;
                    ++fBi;
                }
                while (fBi < fBe);
            }

            public void PumpJavaScript(JsWriter writer)
            {
                SkipWhitespace();   // Skip leading whitespace

                do
                {
                    // Refill buffer if fewer than two chars
                    if (!fAtEnd && fBe - fBi < 2)
                    {
                        RefillBuf();
                    }

                    // Count newlines
                    if (fBi < fBe && fBuf[fBi] == '\n')
                    {
                        ++fBi;
                        ++fInLn;
                        writer.Write(fInLn, '\n');
                    }

                    // Scan to any escape character or line break (because we're counting lines)
                    int anchor = fBi;
                    char c = '\0';
                    while (fBi < fBe)
                    {
                        c = fBuf[fBi];
                        if (c == '%' || c == '\n') break;
                        ++fBi;
                    }

                    // Pump the javascript to output
                    writer.Write(fInLn, fBuf, anchor, fBi - anchor);

                    // Refill if fewer than two characters
                    if (!fAtEnd && fBe - fBi < 2) continue;

                    if (c == '%')
                    {
                        // Exit if closing token
                        if (fBuf[fBi + 1] == '>') break;

                        // Else, just transfer the percent sign
                        writer.Write(fInLn, '%');
                        ++fBi;
                    }
                }
                while (!fAtEnd || fBi < fBe);
            }

            public void PumpTemplate(JsWriter writer)
            {
                do
                {
                    // Refill buffer if fewer than two chars
                    if (!fAtEnd && fBe - fBi < 2)
                    {
                        RefillBuf();
                    }

                    // Scan to any escape character
                    int anchor = fBi;
                    char c = '\0';
                    while (fBi < fBe)
                    {
                        c = fBuf[fBi];
                        if (c == '<' || c == '\\' || c == '\"' || c == '\r' || c == '\n') break;
                        ++fBi;
                    }

                    // Pump the template to output
                    writer.Write(fInLn, fBuf, anchor, fBi - anchor);

                    // Refill if fewer than two characters
                    if (!fAtEnd && fBe - fBi < 2) continue;

                    // Handle the special character
                    // We can look ahead even if at end of file
                    // because refill buff null pads.
                    switch(c)
                    {
                        // May be opening token
                        case '<':
                            if (fBuf[fBi + 1] == '%')
                            {
                                return; // Found the token. End.
                            }
                            else
                            {
                                // Pump one character
                                writer.Write(fInLn, '<');
                                ++fBi;
                            }
                            break;

                        // Escape backslashes
                        case '\\':
                            writer.Write(fInLn, "\\\\");
                            ++fBi;
                            break;

                        // Escape quotes
                        case '\"':
                            writer.Write(fInLn, "\\\"");
                            ++fBi;
                            break;

                        case '\r':
                            writer.Write(fInLn, "\\r");
                            ++fBi;
                            break;

                        case '\n':
                            writer.Write(fInLn, "\\n");
                            ++fBi;
                            ++fInLn;
                            break;
                    }
                }
                while (!fAtEnd || fBi < fBe);
            }

            public string ReadToken()
            {
                SkipWhitespace();

                // Scan to the end of the token. Try once, refill buffer and try again
                int scan = fBi;
                for (int attempt = 0; attempt < 2; ++attempt)
                {
                    scan = fBi;
                    while (fBi < fBe)
                    {
                        char c = fBuf[scan];
                        if (char.IsWhiteSpace(c) || c == '%') break;
                        ++scan;
                    }

                    if (scan < fBe || attempt > 0) break;

                    RefillBuf();
                }

                string result = new string(fBuf, fBi, scan - fBi);
                fBi = scan;
                return result;
            }

            public string ReadQuoted()
            {
                SkipWhitespace();

                if (fBi >= fBe) RefillBuf();

                char quote = fBuf[fBi]; // Safe because even when the buffer is empty it's null-filled.
                if (quote != '\'' && quote != '\"')
                {
                    throw new EJSParseException(fInFilename, fInLn,
                        string.Format("Expected quote. Found '{0}'", quote));
                }

                // Skip the quote
                ++fBi;

                // Scan to the end of the quoted string.
                string token = string.Empty;
                while (fBi < fBe)
                {
                    int anchor = fBi;

                    // Scan up to the quote, escape or end-of-buffer
                    char c = '\0';
                    while (fBi < fBe)
                    {
                        c = fBuf[fBi];
                        if (c == quote || c == '\\') break;
                        ++fBi;
                    }
                    token = string.Concat(token, new String(fBuf, anchor, fBi-anchor));

                    // Skip the terminator
                    ++fBi;

                    // Exit if quote
                    if (c != '\\') break;

                    // Refill the buffer if necessary
                    if (fBi >= fBe) RefillBuf();
                    if (fBi >= fBe)
                    {
                        throw new EJSParseException(fInFilename, fInLn, "Unexpected end-of-file.");
                    }

                    // Deal with the escape character
                    c = fBuf[fBi++];
                    switch (c)
                    {
                        case 't':
                            token = string.Concat(token, "\t");
                            break;

                        case 'r':
                            token = string.Concat(token, "\r");
                            break;

                        case 'n':
                            token = string.Concat(token, "\n");
                            break;

                        case '\n':
                            token = string.Concat(token, "\n");
                            ++fInLn;
                            break;

                        default:
                            token = string.Concat(token, c);
                            break;
                    }
                }

                return token;
            }

            public string Peek(int len)
            {
                if (len > fBe-fBi)
                {
                    RefillBuf();
                    if (len > fBe - fBi) len = fBe - fBi;
                }
                return new string(fBuf, fBi, len);
            }

        }

        public delegate TextReader OpenIncludeFileDelegate(string filename, out FileTypeId rFileTypeId);

        private static void ProcessHeader(EjsScanner pump, JsWriter writer)
        {
            /* The header is in a key: value format with one value per line. Potentially the
             * header could be expanded to full YAML. (see http://www.yaml.org )
             * Regardless, this parser does not parse the header but simply skips to the
             * ending delimiter "%>".
             */
            pump.SkipUntilCloseDelimiter();
        }

        private static void ProcessInclude(EjsScanner pump, OpenIncludeFileDelegate openIncludeFile, JsWriter writer)
        {
            string includeFilename = pump.ReadQuoted();
            pump.SkipWhitespace();

            if (openIncludeFile == null)
            {
                throw new EJSParseException(pump.InFilename, pump.InLineNo, "No OpenIncludeFileDelegate provided.");
            }

            TextReader includeReader = null;
            FileTypeId fileTypeId = FileTypeId.unknown;
            try
            {
                try
                {
                    includeReader = openIncludeFile(includeFilename, out fileTypeId);
                }
                catch (Exception err)
                {
                    throw new EJSParseException(pump.InFilename, pump.InLineNo,
                        string.Format("Failed to open include file '{0}' : {1}", includeFilename, err.Message));
                }
                if (includeReader == null)
                {
                    throw new EJSParseException(pump.InFilename, pump.InLineNo,
                        string.Format("Failed to open include file '{0}' : {1}", includeFilename));
                }

                if (fileTypeId == FileTypeId.ejs)
                {
                    // Embed EJS
                    Translate(includeReader, openIncludeFile, Path.GetFileName(includeFilename), writer);
                }
                else if (fileTypeId == FileTypeId.js)
                {
                    // Embed literal
                    writer.IncludeLiteral(includeReader, Path.GetFileName(includeFilename), writer);
                }
                else
                {
                    throw new EJSParseException(pump.InFilename, pump.InLineNo,
                        string.Format("Type of include file '{0}' is '{1}'. Must be JavaScript or Embedded JavaScript. Type is determined by file header (\"<%#ejs\" or \"/*javascript\").", includeFilename, fileTypeId));
                }
            }
            finally
            {
                if (includeReader != null)
                {
                    includeReader.Close();
                    includeReader = null;
                }
            }

            // Restore context
            writer.TrackInput(pump.InFilename, pump.InLineNo);
        }

        private static void ProcessDirective(EjsScanner pump, OpenIncludeFileDelegate openIncludeFile, JsWriter writer)
        {
            string token = pump.ReadToken();

            switch (token)
            {
                case "ejs": // Technically the metadata header should only appear at the beginning. But we tolerate it anywhere.
                    ProcessHeader(pump, writer);
                    break;

                case "include":
                    ProcessInclude(pump, openIncludeFile, writer);
                    break;

                default:
                    throw new EJSParseException(pump.InFilename, pump.InLineNo, string.Format("Unknown directive '{0}'.", token));
            }

        }

        // Translate from an EJS template into a JavaScript package
        private static void Translate(TextReader reader, OpenIncludeFileDelegate openIncludeFile, string inFilename, JsWriter writer)
        {
            EjsScanner pump = new EjsScanner(reader, inFilename, 1);
            
            // Set context (for error reporting)
            writer.TrackInput(inFilename, 1);

            pump.SkipWhitespace();

            // Alternate pumping template and JavaScript
            for (; ; )
            {
                string token;

                token = pump.Peek(3);

                if (!token.StartsWith("<%", StringComparison.Ordinal))
                {
                    writer.Write(pump.InLineNo, sEJS_Literal);
                    pump.PumpTemplate(writer);
                    writer.Write(pump.InLineNo, sEJS_LiteralEnd);
                    if (pump.AtEnd) break;

                    // Peek again
                    token = pump.Peek(3);
                }

                // A directive
                if (token.StartsWith("<%#"))
                {
                    pump.Skip(3);
                    ProcessDirective(pump, openIncludeFile, writer);
                }
                // Evaluate and insert a JavaScript expression
                else if (token.StartsWith("<%=", StringComparison.Ordinal))
                {
                    pump.Skip(3);
                    writer.Write(pump.InLineNo, sEJS_Write);
                    pump.PumpJavaScript(writer);
                    writer.Write(pump.InLineNo, sEJS_WriteEnd);
                }
                // Insert literal JavaScript
                else if (token.StartsWith("<%", StringComparison.Ordinal))
                {
                    pump.Skip(2);
                    pump.PumpJavaScript(writer);
                    // Finish with a newline (in case there is something line-sensitive such as a comment)
                    writer.Write(pump.InLineNo, "\r\n");
                }
                else
                {
                    throw new EJSParseException(pump.InFilename, pump.InLineNo,
                        string.Format("Expected '<%' found '{0}'.", token));
                }

                token = pump.Peek(2);
                if (!token.Equals("%>", StringComparison.Ordinal))
                {
                    throw new EJSParseException(pump.InFilename, pump.InLineNo,
                        string.Format("Expected '%>' found '{0}'.", token));
                }
                pump.Skip(2);
            }
        }

        public static EJScript CompileEjs(TextReader reader, OpenIncludeFileDelegate openIncludeFile, string inFilename)
        {

            CompilerResults compilerResults;
            JsWriter jsWriter;
            string tempFileName = Path.GetTempFileName();
            try
            {
                using (StreamWriter writer = new StreamWriter(tempFileName))
                {
                    jsWriter = new JsWriter(writer);
                    jsWriter.TrackInput("{header}", 1);
                    jsWriter.Write(1, sEJS_Header);
                    Translate(reader, openIncludeFile, inFilename, jsWriter);
                    jsWriter.TrackInput("{footer}", 1);
                    jsWriter.Write(1, sEJS_Footer);
                    jsWriter.DetachTextWriter();
                }

                var parameters = new CompilerParameters();
                parameters.GenerateInMemory = true;
                parameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
                var provider = new JScriptCodeProvider();
                compilerResults = provider.CompileAssemblyFromFile(parameters, tempFileName);
            }
            finally
            {
                try
                {
#if DEBUG
                    Debug.WriteLine("Temp file is: " + tempFileName);
#endif
#if !INSERT_DEBUG_IN_OUTPUT || !DEBUG
                    File.Delete(tempFileName);
#endif
                }
                catch
                {
                    // Suppress errors here
                }
            }
            
            // Process any errors (but not warnings
            foreach (CompilerError err in compilerResults.Errors)
            {
                if (!err.IsWarning)
                {
                    throw new EJSParseException(err.FileName, err.Line, err.Column, err.ErrorText);
                }
            }

            if (compilerResults.CompiledAssembly == null)
            {
                throw new EJSParseException(inFilename, 0, "No code generated.");
            }

            return new EJScript(compilerResults.CompiledAssembly);
        }

        // Member variables
        Assembly mAssembly;
        Type mType;

        private EJScript(Assembly assembly)
        {
            Debug.Assert(assembly != null);
            mAssembly = assembly;
            mType = assembly.GetType(string.Concat(sEjs_Class));
        }

        public void Run(params object[] args)
        {
            var inst = Activator.CreateInstance(mType);
            mType.InvokeMember(sEjs_Function, BindingFlags.InvokeMethod, null, inst, args);
        }
    }

    class EJSParseException : ApplicationException
    {
        public string FileName { get; private set; }
        public int Line {get; private set; }
        public int Column { get; private set; }
        public string ParseMessage { get; private set; }

        public EJSParseException(string fileName, int line, string parseMessage)
            : base(String.Format("EJScript Parse Error: {0}({1}) : {2}", fileName, line, parseMessage))
        {
            FileName = fileName;
            Line = line;
            Column = 0;
            ParseMessage = parseMessage;
        }

        public EJSParseException(string fileName, int line, int column, string parseMessage)
            : base(String.Format("EJScript Parse Error: {0}({1},{2}) : {3}", fileName, line, column, parseMessage))
        {
            FileName = fileName;
            Line = line;
            Column = column;
            ParseMessage = parseMessage;
        }

    }
}