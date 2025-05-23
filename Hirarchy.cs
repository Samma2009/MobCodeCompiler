using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            d.name = Name.ToLower().Replace(" ", "")  ;

            ((DirEntry)entry).data.Add(d);

            foreach (var item in Children)
            {
                item.Generate(d);
            }
        }
    }
    public class CommandElement : HirarchyElemet
    {
        string Data;
        public CommandElement(string data) : base()
        {
            this.Data = data;
        }
        public override void Generate(FTEntry entry)
        {
            if (entry.GetType() != typeof(FileEntry)) return;
            var fe = ((FileEntry)entry);
            bool newline = Data.Contains(@"\");
            if (newline)
            {
                var d = Data.Split(@"\");
                for (int i = 0; i < d.Length; i++)
                {
                    d[i] = d[i].Trim();
                }
                Data = string.Join(" ",d);
            }

            string processedData = Parser.SubstituteVariables(Data);
            fe.data += Macros.EvaluateMacro(processedData) + "\n";
        }
    }
}
