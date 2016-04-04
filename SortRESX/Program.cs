using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SortRESX
{
    //
    // 0 command line parameters ==> input is from stdin and output is stdout.
    // 1 command line parameter  ==> input is a source .resx file (arg[0]) and output is stdout.
    // 2 command line parameters ==> input is a source .resx file (arg[0]) and output is to a target .resx file (arg[1])
    //
    // -l ==> input is a list of source .resx files and output is themselves
    //
    // The program reads the source and writes a sorted version of it to the output.
    //
    class Program
    {
        static void Main(string[] args)
        {
            args = new String[]{"-l", "FrmMain.resx"};
            // Initializing parameters
            List<String> largs = new List<String>(args);
            bool fileList = false;
            bool stdinInput = false;
            bool consoleOutput = false;
            string outputFile = "";
            string inputFile ="";
            XmlReader inputStream = null;

            // Check parameters
            if (largs.Count > 0){
                string arg0 = largs.ElementAt(0).ToLower();

                // help
                if( arg0.StartsWith(@"-h") || arg0.StartsWith(@"/?")) {
                    ShowHelp();
                    return;
                }

                // file list mode
                if (arg0.StartsWith(@"-l")) {
                    largs.RemoveAt(0);
                    if (largs.Count == 0)
                    {
                        ShowHelp();
                        return;
                    } else
                        fileList = true;
                }
            }

            // Outputs
            if (!fileList) {
                stdinInput = largs.Count == 0;
                consoleOutput = largs.Count == 0 || largs.Count == 1;
                inputFile = largs.Count > 0 ? args.ElementAt(0) : null;
                outputFile = largs.Count == 2 ? args.ElementAt(1) : null;

                // stdin handling
                if(stdinInput)
                {
                    try 
                    {
                        Stream s = Console.OpenStandardInput();
                        inputStream = XmlReader.Create(s);
                    }
                    catch (Exception ex) 
                    {
                        Console.WriteLine("Error reading from stdin: {0}", ex.Message);
                        return;
                    }
                }
                // file handling
                else {
                    try
                    {
                      inputStream = XmlReader.Create(inputFile);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Error opening file '{0}': {1}", args.ElementAt(0), ex.Message);
                    }
                }

                // console output
                if(consoleOutput)
                    HandleSorting<TextWriter>(inputStream, Console.Out);
                // file output
                else
                    HandleSorting<String>(inputStream, outputFile);
            }
            else {
                foreach (string path in largs) {
                    try
                    {
                      inputStream = XmlReader.Create(path);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Error opening file '{0}': {1}", largs.ElementAt(0), ex.Message);
                    }
                    HandleSorting<String>(inputStream, path);
                }
            }

            return;
        }

        private static void HandleSorting<T>(XmlReader inputStream, T ouput)
        {
            try
            {
                // Create a linq XML document from the source.
                XDocument doc = XDocument.Load(inputStream);
                // Create a sorted version of the XML
                XDocument sortedDoc = SortDataByName(doc);
                // Closing input stream before saving in order to avoid access conflicts
                inputStream.Close();
                //Save the sorted resx
                SaveStream(sortedDoc, ouput);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        private static void SaveStream<T>(XDocument xdoc, T output)
        {
            if (typeof(T) == typeof(TextWriter))
                xdoc.Save(output as TextWriter);
            else
                xdoc.Save(output as string);
        }

        //
        // Use Linq to sort the elements.  The comment, schema, resheader, assembly, metadata, data appear in that order, 
        // with resheader, assembly, metadata and data elements sorted by name attribute.
        private static XDocument SortDataByName(XDocument resx)
        {
            return new XDocument(
                new XElement(resx.Root.Name,
                    from comment in resx.Root.Nodes() where comment.NodeType == XmlNodeType.Comment select comment,
                    from schema in resx.Root.Elements() where schema.Name.LocalName == "schema" select schema,
                    from resheader in resx.Root.Elements("resheader") orderby (string)resheader.Attribute("name") select resheader,
                    from assembly in resx.Root.Elements("assembly") orderby (string)assembly.Attribute("name") select assembly,
                    from metadata in resx.Root.Elements("metadata") orderby (string)metadata.Attribute("name") select metadata,
                    from data in resx.Root.Elements("data") orderby (string)data.Attribute("name") select data
                )
            );
        }

        //
        // Write invocation instructions to stderr.
        //
        private static void ShowHelp()
        {
            Console.Error.WriteLine(
            "0 arguments ==> Input from STDIN.  Output to STDOUT.\n" +
            "1 argument  ==> Input from specified .resx file.  Output to STDOUT.\n" +
            "2 arguments ==> Input from first specified .resx file.  Output to second specified .resx file.\n" +
            "-l and n arguments ==> Input is a list of source .resx files and output is themselves.");
        }
    }
}
