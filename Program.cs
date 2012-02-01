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

			var lines = File
				.ReadAllLines(files.Item1, Encoding.Default)
				.Where(s => !string.IsNullOrWhiteSpace(s));

			string currSection = string.Empty;
			var dict = new Dictionary<string, Tuple<List<Tuple<string, List<string>>>, List<string>>>
			{
				{
					currSection, new Tuple<List<Tuple<string, List<string>>>, List<string>>(
						new List<Tuple<string, List<string>>>(), new List<string>())
				}
			};
			var comments = new List<string>();

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

			if (dict[string.Empty].Item1.Count == 0)
				dict.Remove(string.Empty);

			var doc = new XmlDocument();
			XmlElement root = doc.CreateElement("root");
			foreach (KeyValuePair<string, Tuple<List<Tuple<string, List<string>>>, List<string>>> kvp in dict)
			{
				foreach (string sectCommentName in kvp.Value.Item2)
					root.AppendChild(doc.CreateComment(sectCommentName));

				XmlElement sect = doc.CreateElement("section");
				XmlAttribute nameAttrib = doc.CreateAttribute("name");
				nameAttrib.Value = kvp.Key;
				sect.Attributes.Append(nameAttrib);

				foreach (Tuple<string, List<string>> item in kvp.Value.Item1)
				{
					foreach (string commentName in item.Item2)
						sect.AppendChild(doc.CreateComment(commentName));

					string[] pair = item.Item1.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
					XmlElement node = doc.CreateElement(pair[0].Trim());
					node.InnerText = pair[1].Trim();
					sect.AppendChild(node);
				}

				root.AppendChild(sect);
			}
			doc.AppendChild(root);
			doc.Save(files.Item2);
		}
	}
}
