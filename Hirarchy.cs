using System;
using System.Collections.Generic;
using System.Linq;
using NCalc;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MobCode
{
    public abstract class HirarchyElemet
    {
        public string Type;
        public List<HirarchyElemet> Children;

        public HirarchyElemet()
        {
            Type = this.GetType().Name;
            Children = new List<HirarchyElemet>();
        }

        public abstract void Generate(FTEntry entry);
    }

    public class NamespaceElement : HirarchyElemet
    {
        string Name;
        public NamespaceElement(string name) : base()
        {
            Name = name;
        }
        public override void Generate(FTEntry entry)
        {
            if (entry.GetType() != typeof(DirEntry)) return;

            var d = new DirEntry();
            d.name = Name.ToLower().Replace(" ","")+"/function";

            ((DirEntry)entry).data.Add(d);

            foreach (var item in Children)
            {
                item.Generate(d);
            }
        }
    }
    public class ClassElement : HirarchyElemet
    {
        string Name;
        public ClassElement(string name) : base()
        {
            Name = name;
        }
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
    public class FunctionElement : HirarchyElemet
    {
        string Name;
        public FunctionElement(string name) : base()
        {
            Name = name;
        }
        public override void Generate(FTEntry entry)
        {
            if (entry.GetType() != typeof(DirEntry)) return;

            var d = new FileEntry();
            d.name = Name.ToLower().Replace(" ", "");

            ((DirEntry)entry).data.Add(d);

            CommandElement.ComTimeVariables.Clear();

            foreach (var item in Children)
            {
                item.Generate(d);
            }
        }
    }
    public class IfElement : HirarchyElemet
    {
        string Name;
        public IfElement(string name) : base()
        {
            Name = name;
        }
        public override void Generate(FTEntry entry)
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
    public class RepeatElement : HirarchyElemet
    {
        string Name;
        public RepeatElement(string name) : base()
        {
            Name = name;
        }
        public override void Generate(FTEntry entry)
        {

            foreach (var item in CommandElement.ComTimeVariables)
            {
                Name = Name.Replace(item.Key, item.Value);
            }

            try
            {
                int iter = int.Parse(new Expression(Name).Evaluate()!.ToString()!);
                for (int i = 0; i < iter; i++)
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
    public class CommandElement : HirarchyElemet
    {
        public static Dictionary<string, string> ComTimeVariables = new();
        string Datad;
        public CommandElement(string Data) : base()
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
                    var path = string.IsNullOrEmpty(currentPath) ? property.Name : $"{currentPath}.{property.Name}";
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
            var fe = ((FileEntry)entry);
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
                            var fullVarName = $"{varName}.{path}";
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
