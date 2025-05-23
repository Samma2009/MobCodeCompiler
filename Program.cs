namespace MobCode
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1) return;

            Macros.LoadMacros();

            if (args[0] == "build")
                Build();
            else if (args[0] == "watch")
                Watch();
            else
                Console.WriteLine("Invalid argument " + args[0]);
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
                var i = item.Trim()+" ";
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
            var w = new FileSystemWatcher(".","*.mc");
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
            while (true);
        }

        static void FDNavigate(FTEntry entry,string composedpath)
        {
            if (entry.GetType() == typeof(DirEntry))
            {
                Directory.CreateDirectory(composedpath+ entry.name+"/");
                foreach (var item in ((DirEntry)entry).data)
                {
                    FDNavigate(item, composedpath + entry.name + "/");
                }
            }
            else
            {
                File.WriteAllText(composedpath + entry.name+".mcfunction",((FileEntry)entry).data);
            }
        }
    }
}
