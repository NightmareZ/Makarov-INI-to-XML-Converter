/*
INI to XML Converter

Copyright (c) 2012-2022 Mykhailo Makarov

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

using System.Text;
using System.Xml;

namespace IniToXmlConverter;

public sealed class Program
{
    /// <summary>
    /// Read program arguments (input and output file names).
    /// </summary>
    private static Tuple<string, string> ReadArgs(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Invalid command syntax.");
            return null;
        }

        if (!File.Exists(args[0]))
        {
            Console.WriteLine("Input file not found.");
            return null;
        }

        return new Tuple<string, string>(args[0], args[1]);
    }

    public static void Main(string[] args)
    {
        Tuple<string, string> files = ReadArgs(args);

        if (files is null)
        {
            Environment.Exit(1);
        }

        // Read input file and remove empty lines.
        var lines = File
            .ReadAllLines(files.Item1, Encoding.Default)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s));

        string currentSection = string.Empty;
        // Dictionary (
        //    section name,
        //    pair (
        //        list of pairs (
        //            pair (
        //                item name,
        //                item value
        //            ),
        //            list of comments for item
        //        ),
        //        list of comments for section
        //    )
        // )
        var dict = new Dictionary<string, Tuple<List<Tuple<Tuple<string, string>, List<string>>>, List<string>>>
        {
            {
                currentSection, new Tuple<List<Tuple<Tuple<string, string>, List<string>>>, List<string>>(
                    new List<Tuple<Tuple<string, string>, List<string>>>(), new List<string>())
            }
        };

        var comments = new List<string>();

        // Parse input lines and fill dictionary.
        foreach (string line in lines)
        {
            if (line.StartsWith(";"))
            {
                comments.Add(line.Substring(1));
            }
            else if (line.StartsWith("//"))
            {
                comments.Add(line.Substring(2));
            }
            else if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line.Substring(1, line.Length - 2);

                if (!dict.ContainsKey(currentSection))
                {
                    dict.Add(currentSection, new Tuple<List<Tuple<Tuple<string, string>, List<string>>>, List<string>>(
                        new List<Tuple<Tuple<string, string>, List<string>>>(), comments));
                }

                comments = new List<string>();
            }
            else
            {
                string[] pair = line.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                dict[currentSection].Item1.Add(new Tuple<Tuple<string, string>, List<string>>(
                    new Tuple<string, string>(pair[0].Trim(), pair[1].Trim()),
                    comments));

                comments = new List<string>();
            }
        }

        // Remove default (nameless) section if they are empty.
        if (dict[string.Empty].Item1.Count == 0)
        {
            dict.Remove(string.Empty);
        }

        var xmlDocument = new XmlDocument();
        XmlElement root = xmlDocument.CreateElement("root");

        foreach (KeyValuePair<string, Tuple<List<Tuple<Tuple<string, string>, List<string>>>, List<string>>> kvp in dict)
        {
            foreach (string sectCommentText in kvp.Value.Item2)
            {
                root.AppendChild(xmlDocument.CreateComment(sectCommentText));
            }

            XmlElement sectNode = xmlDocument.CreateElement("section");
            XmlAttribute sectionNameAttribute = xmlDocument.CreateAttribute("name");
            sectionNameAttribute.Value = kvp.Key;
            sectNode.Attributes.Append(sectionNameAttribute);

            var createdItemsArrays = new List<string>();

            foreach (Tuple<Tuple<string, string>, List<string>> item in kvp.Value.Item1)
            {
                if (item.Item1.Item1.EndsWith("[]"))
                {
                    // Output array.

                    if (!createdItemsArrays.Contains(item.Item1.Item1))
                    {
                        string arrayName = item.Item1.Item1.Substring(0, item.Item1.Item1.Length - 2);
                        XmlElement arrayNode = xmlDocument.CreateElement("array");
                        XmlAttribute arrayNameAttribute = xmlDocument.CreateAttribute("name");
                        arrayNameAttribute.Value = arrayName;
                        arrayNode.Attributes.Append(arrayNameAttribute);

                        var arrayItems = from x in kvp.Value.Item1
                            where x.Item1.Item1.Substring(0, x.Item1.Item1.Length - 2) == arrayName
                            select x;

                        foreach (Tuple<Tuple<string, string>, List<string>> arrayItem in arrayItems)
                        {
                            foreach (string itemCommentText in arrayItem.Item2)
                            {
                                arrayNode.AppendChild(xmlDocument.CreateComment(itemCommentText));
                            }

                            XmlElement itemNode = xmlDocument.CreateElement("item");
                            itemNode.InnerText = arrayItem.Item1.Item2;
                            arrayNode.AppendChild(itemNode);
                        }

                        sectNode.AppendChild(arrayNode);
                        createdItemsArrays.Add(item.Item1.Item1);
                    }
                }
                else
                {
                    // Output single item.

                    foreach (string itemCommentText in item.Item2)
                    {
                        sectNode.AppendChild(xmlDocument.CreateComment(itemCommentText));
                    }

                    XmlElement itemNode = xmlDocument.CreateElement("item");
                    XmlAttribute itemNameAttribute = xmlDocument.CreateAttribute("name");
                    itemNameAttribute.Value = item.Item1.Item1;
                    itemNode.Attributes.Append(itemNameAttribute);
                    itemNode.InnerText = item.Item1.Item2;
                    sectNode.AppendChild(itemNode);
                }
            }

            root.AppendChild(sectNode);
        }

        xmlDocument.AppendChild(root);
        xmlDocument.Save(files.Item2);
    }
}
