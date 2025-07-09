using System;
using System.Collections.Generic;
using System.Linq;
using NCalc;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MobCode
{
    public abstract class HierarchyElement
    {
        public string Name { get; internal set; } = "";
        public string[] Modifiers;
        public List<HierarchyElement> Children;

        public HierarchyElement(string name)
        {
            Modifiers = [];
            Children = new List<HierarchyElement>();
            Name = name;
        }

        public abstract void Generate(FTEntry entry);
    }

    public class NamespaceElement : HierarchyElement
    {
        public NamespaceElement(string name) : base(name) {}
        public override void Generate(FTEntry entry)
        {
            if (entry.GetType() != typeof(DirEntry)) return;

            var d = new DirEntry();
            d.name = Name.ToLower().Replace(" ","")+"/${SubesetTemplate}$";

            ((DirEntry)entry).data.Add(d);

            foreach (var item in Children)
            {
                item.Generate(d);
            }
        }
    }
    public class ClassElement : HierarchyElement
    {
        public ClassElement(string name) : base(name) {}
        public override void Generate(FTEntry entry)
        {
            if (entry.GetType() != typeof(DirEntry)) return;

            var d = new DirEntry();
            d.name = Name.ToLower().Replace(" ", "");

            ((DirEntry)entry).data.Add(d);

            foreach (var item in Children)
            {
                item.Generate(d);
            }
        }
    }
    public class FunctionElement : HierarchyElement
    {
        public FunctionElement(string name) : base(name) {}
        public override void Generate(FTEntry entry)
        {
            if (entry.GetType() != typeof(DirEntry)) return;

            var d = new FileEntry();
            d.name = Name.ToLower().Replace(" ", "");
            d.extension = ".mcfunction";
            d.modifiers = Modifiers;
            d.GenerationSubSet = "function";

            ((DirEntry)entry).data.Add(d);

            CommandElement.ComTimeVariables.Clear();

            foreach (var item in Children)
            {
                item.Generate(d);
            }
        }
    }
    public class JsonElement : HierarchyElement
    {
        string Content;
        string EType;
        public JsonElement(string name, string content, string type) : base(name)
        {
            Content = content;
            EType = type;
        }
        public override void Generate(FTEntry entry)
        {
            if (entry.GetType() != typeof(DirEntry)) return;

            var d = new FileEntry();
            d.name = Name.ToLower().Replace(" ", "");
            d.extension = ".json";
            d.modifiers = Modifiers;
            d.GenerationSubSet = EType;

            ((DirEntry)entry).data.Add(d);
            d.data = Content;
        }
    }
    public class IfElement : HierarchyElement
    {
        public IfElement(string name) : base(name) {}        public override void Generate(FTEntry entry)
        {
            foreach (var item in CommandElement.ComTimeVariables)
            {
                Name = Name.Replace(item.Key, item.Value);
            }

            try
            {
                if (bool.Parse(new Expression(Name).Evaluate()!.ToString()!))
                    foreach (var item in Children)
                    {
                        item.Generate(entry);
                    }
            }
            catch (Exception) { }
        }
    }
    public class RepeatElement : HierarchyElement
    {
        const int MaxIter = 50000;
        
        public RepeatElement(string name) : base(name) { }
        public override void Generate(FTEntry entry)
        {

            foreach (var item in CommandElement.ComTimeVariables)
            {
                Name = Name.Replace(item.Key, item.Value);
            }

            try
            {
                int iter = int.Parse(new Expression(Name).Evaluate()!.ToString()!);
                for (int i = 0; i < Math.Min(iter,MaxIter); i++)
                {
                    foreach (var item in Children)
                    {
                        item.Generate(entry);
                    }
                }
            }
            catch (Exception) { }
        }
    }
    public class SwitchElement : HierarchyElement
    {
        public SwitchElement(string name) : base(name) {}
        public override void Generate(FTEntry entry)
        {

            foreach (var item in CommandElement.ComTimeVariables)
            {
                Name = Name.Replace(item.Key, item.Value);
            }

            foreach (var item in Children)
            {
                if (item.GetType() != typeof(CaseElement))
                {
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write("Error:");
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(" only case elements are allowed in switches");
                        Console.ResetColor();
                    continue;
                }
                    
                try
                {
                    if (bool.Parse(new Expression(Name + " == " + (item as CaseElement)!.Name).Evaluate()!.ToString()!))
                    {
                        item.Generate(entry);
                    }
                }
                catch (Exception)
                { }
            }
        }
    }
    public class CaseElement : HierarchyElement
    {
        public CaseElement(string name) : base(name) {}
        public override void Generate(FTEntry entry)
        {
            foreach (var item in Children)
            {
                item.Generate(entry);
            }
        }
    }
    public class WhileElement : HierarchyElement
    {
        const int MaxIter = 50000;

        public WhileElement(string name) : base(name) { }
        public override void Generate(FTEntry entry)
        {
            try
            {
                int breakout = 0;
                var n = Name;
                foreach (var item in CommandElement.ComTimeVariables)
                {
                    n = n.Replace(item.Key, item.Value);
                }
                while (bool.Parse(new Expression(n).Evaluate()!.ToString()!))
                {
                    if (breakout >= MaxIter)
                    {
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write("Error:");
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(" max iteration count reached (50000 iterations)");
                        Console.ResetColor();
                        return;
                    }
                    foreach (var item in Children)
                    {
                        item.Generate(entry);
                    }
                    n = Name;
                    foreach (var item in CommandElement.ComTimeVariables)
                    {
                        n = n.Replace(item.Key, item.Value);
                    }
                    breakout++;
                }
            }
            catch (Exception) { }
        }
    }
    public class CommandElement : HierarchyElement
    {
        public static Dictionary<string, string> ComTimeVariables = new();
        string Datad;
        public CommandElement(string Data) : base("")
        {
            this.Datad = Data;
        }
        static bool IsValidJson(string input)
        {
            try
            {
                JToken.Parse(input);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        static List<string> ExtractJsonPaths(JToken token, string currentPath = "")
        {
            var paths = new List<string>();

            paths.Add(currentPath);

            if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    var path = string.IsNullOrEmpty(currentPath) ? "."+property.Name : $"{currentPath}.{property.Name}";
                    paths.AddRange(ExtractJsonPaths(property.Value, path));
                }
            }
            else if (token is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    var path = $"{currentPath}[{i}]";
                    paths.AddRange(ExtractJsonPaths(array[i], path));
                }
            }
            return paths;
        }
        public override void Generate(FTEntry entry)
        {
            if (entry.GetType() != typeof(FileEntry)) return;
            var fe = (FileEntry)entry;
            string Data = Datad;
            bool newline = Data.Contains(@"\");
            if (newline)
            {
                var d = Data.Split(@"\");
                for (int i = 0; i < d.Length; i++)
                {
                    d[i] = d[i].Trim();
                }
                Data = string.Join(" ", d);
            }

            foreach (var item in ComTimeVariables)
            {
                Data = Data.Replace(item.Key, item.Value);
            }

            if (Data.Trim().StartsWith("$"))
            {
                var parts = Data.Split('=');
                if (parts.Length > 1 && !parts[0].Trim().Contains(" "))
                {
                    string varName = parts[0].Trim();
                    Data = Data.Trim();
                    string value = Data.Substring(Data.IndexOf('=') + 1).Trim();

                    try
                    {
                        value = new Expression(value).Evaluate()!.ToString()!;
                    }
                    catch (Exception) { }

                    ComTimeVariables[varName + "$"] = value;

                    if (IsValidJson(value))
                    {
                        var token = JToken.Parse(value);
                        var paths = ExtractJsonPaths(token);

                        foreach (var path in paths)
                        {
                            var fullVarName = varName+path;
                            var tokenValue = token.SelectToken(path);
                            if (tokenValue != null)
                            {
                                var s = tokenValue.ToString().Replace("\r", "").Replace("\t", "").Split("\n");
                                var res = "";
                                foreach (var item in s)
                                {
                                    res += item.Trim();
                                }
                                ComTimeVariables[fullVarName + "$"] = res;
                            }
                        }
                    }

                    return;
                }
            }

            fe.data += Macros.EvaluateMacro(Data) + "\n";
        }
    }
}
