using System.IO.Compression;
using System.Net;

namespace MobCode
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) return;

            Macros.LoadMacros();

            switch (args[0])
            {
                case "build": Build(); break;
                case "watch": Watch(); break;
                case "install": InstallMacros(args.Skip(1).ToArray()); break;
                default:
                    Console.WriteLine("Invalid argument " + args[0]);
                    break;
            }
        }

        static void InstallMacros(string[] urls)
        {
            foreach (var item in urls)
            {
                var client = new HttpClient();
                var stream = client.GetStreamAsync(item).GetAwaiter().GetResult();
                var zip = new ZipArchive(stream);
                try
                {
                    zip.ExtractToDirectory(Path.Combine(AppContext.BaseDirectory, "ComplierMacros"));
                    foreach (var file in zip.Entries)
                    {
                        Console.WriteLine("downloaded " + file.Name);
                    }
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("failed to dowload from link " + item + " with error: " + ex.Message);
                }
            }
        }

        static void Build()
        {
            foreach (var f in Directory.GetFiles(".", "*.mc"))
            {
                BuildFile(f);
            }
        }

        static void BuildFile(string file)
        {
            var rc = File.ReadAllText(file).Replace("\r", "").Replace("\t", "").Split('\n');
            string c = "";
            foreach (var item in rc)
            {
                var i = item.Trim() + " ";
                if (i.Contains("//"))
                {
                    var s = i.IndexOf("//");
                    i = i.Substring(0, s);
                }
                if (i.Contains("#"))
                {
                    var s = i.IndexOf("#");
                    i = i.Substring(0, s);
                }
                c += i;
            }
            var eltree = Parser.Parse(c);
            var entry = new DirEntry();
            entry.name = "data";
            FileTree.s.Add(entry);
            foreach (var item in eltree)
            {
                if (item.Type != "NamespaceElement")
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write("Error:");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(" root element must be a namespace");
                    Console.ResetColor();
                    continue;
                }
                item.Generate(entry);
            }
            FDNavigate(entry, "");

            Console.WriteLine("Compiled: " + file);
        }

        static bool WaitForFile(string path, int maxRetries = 10, int delayMs = 200)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using (var stream = File.Open(path,
                                                  FileMode.Open,
                                                  FileAccess.Read,
                                                  FileShare.None))
                    {
                        return true; // success
                    }
                }
                catch (IOException)
                {
                    Thread.Sleep(delayMs);
                }
            }
            return false; // still locked
        }
        static void Watch()
        {
            Build();
            var w = new FileSystemWatcher(".", "*.mc");
            w.NotifyFilter = NotifyFilters.FileName
                           | NotifyFilters.LastWrite
                           | NotifyFilters.Size;
            w.IncludeSubdirectories = true;
            w.Changed += (s, e) =>
            {
                if (WaitForFile(e.FullPath))
                    BuildFile(e.FullPath);

            };
            w.Created += (s, e) =>
            {
                if (WaitForFile(e.FullPath))
                    BuildFile(e.FullPath);

            };

            w.EnableRaisingEvents = true;
            while (true) ;
        }

        static void FDNavigate(FTEntry entry, string composedpath)
        {
            if (entry.GetType() == typeof(DirEntry))
            {
                Directory.CreateDirectory(composedpath + entry.name + "/");
                foreach (var item in ((DirEntry)entry).data)
                {
                    FDNavigate(item, composedpath + entry.name + "/");
                }
            }
            else
            {
                if (entry.modifiers.Contains("@load"))
                {
                    var p = ReplaceFirst(composedpath.Replace(@"data/", ""),'/',':').Replace(":function/",":");
                    Directory.CreateDirectory("data/minecraft/tags/function");
                    File.WriteAllText(@"data/minecraft/tags/function/load.json",$"{{\"values\": [\"{p+ entry.name}\"]}}");
                }
                if (entry.modifiers.Contains("@tick"))
                {
                    var p = ReplaceFirst(composedpath.Replace(@"data/", ""),'/',':').Replace(":function/",":");
                    Directory.CreateDirectory("data/minecraft/tags/function");
                    File.WriteAllText(@"data/minecraft/tags/function/tick.json",$"{{\"values\": [\"{p+ entry.name}\"]}}");
                }
                File.WriteAllText(composedpath + entry.name + (entry as FileEntry)!.extension, ((FileEntry)entry).data);
            }
        }

        static string ReplaceFirst(string s, char replacer, char replacee)
        {
            var a = s.IndexOf(replacer);
            var ns = s.ToCharArray();
            ns[a] = replacee;
            return new string(ns);
        }
    }
}
