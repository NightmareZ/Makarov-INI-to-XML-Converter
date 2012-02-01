/*
Makarov-INI-to-XML-Converter

Copyright (c) 2012 Michael Makarov, <http://nightmarez.net>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace IniToXml
{
    public sealed class Program
    {
        /// <summary>
        /// Read program arguments (input and output filenames).
        /// </summary>
        private static Tuple<string, string> ReadArgs(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.WriteLine(@"Invalid command syntax.");
                return null;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine(@"Input file not found.");
                return null;
            }

            return new Tuple<string, string>(args[0], args[1]);
        }

        public static void Main(string[] args)
        {
            Tuple<string, string> files = ReadArgs(args);
            if (files == null)
                Environment.Exit(1);

            // Read input file and remove empty lines.
            var lines = File
                .ReadAllLines(files.Item1, Encoding.Default)
                .Where(s => !string.IsNullOrWhiteSpace(s));

            string currSection = string.Empty;
            // Dictionary (
            //    section name,
            //    pair (
            //        list of pairs (
            //            item name,
            //            list of comments for item
            //        ),
            //        list of comments for section
            //    )
            // )
            var dict = new Dictionary<string, Tuple<List<Tuple<string, List<string>>>, List<string>>>
            {
                {
                    currSection, new Tuple<List<Tuple<string, List<string>>>, List<string>>(
                        new List<Tuple<string, List<string>>>(), new List<string>())
                }
            };
            var comments = new List<string>();

            // Parse input lines and fill dictionary.
            foreach (string line in lines)
            {
                if (line.StartsWith(";"))
                    comments.Add(line.Substring(1));
                else if (line.StartsWith("//"))
                    comments.Add(line.Substring(2));
                else if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currSection = line.Substring(1, line.Length - 2);
                    if (!dict.ContainsKey(currSection))
                        dict.Add(currSection, new Tuple<List<Tuple<string, List<string>>>, List<string>>(
                            new List<Tuple<string, List<string>>>(), comments));
                    comments = new List<string>();
                }
                else
                {
                    dict[currSection].Item1.Add(new Tuple<string, List<string>>(line, comments));
                    comments = new List<string>();
                }
            }

            // Remove default (nameless) section if they are empty.
            if (dict[string.Empty].Item1.Count == 0)
                dict.Remove(string.Empty);

            var doc = new XmlDocument();
            XmlElement root = doc.CreateElement("root");
            foreach (KeyValuePair<string, Tuple<List<Tuple<string, List<string>>>, List<string>>> kvp in dict)
            {
                foreach (string sectCommentText in kvp.Value.Item2)
                    root.AppendChild(doc.CreateComment(sectCommentText));

                XmlElement sectNode = doc.CreateElement("section");
                XmlAttribute sectNameAttrib = doc.CreateAttribute("name");
                sectNameAttrib.Value = kvp.Key;
                sectNode.Attributes.Append(sectNameAttrib);

                foreach (Tuple<string, List<string>> item in kvp.Value.Item1)
                {
                    foreach (string itemCommentText in item.Item2)
                        sectNode.AppendChild(doc.CreateComment(itemCommentText));

                    string[] pair = item.Item1.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                    XmlElement itemNode = doc.CreateElement("item");
                    XmlAttribute itemNameAttrib = doc.CreateAttribute("name");
                    itemNameAttrib.Value = pair[0].Trim();
                    itemNode.Attributes.Append(itemNameAttrib);
                    itemNode.InnerText = pair[1].Trim();
                    sectNode.AppendChild(itemNode);
                }

                root.AppendChild(sectNode);
            }
            doc.AppendChild(root);
            doc.Save(files.Item2);
        }
    }
}
