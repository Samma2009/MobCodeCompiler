using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobCode
{
    public static class Parser
    {
        public static List<HirarchyElemet> Parse(string input) 
        {
            List<HirarchyElemet> elements = new List<HirarchyElemet>();

            int depth = 0;
            string kw = "";
            string name = "";
            string buffer = "";
            string commandbuffer = "";
            foreach (var item in input)
            {
                if (item == '{')
                {
                    depth++;
                    if (depth > 1) buffer += item;
                }
                else if (item == '}')
                {
                    depth--;

                    if (depth > 0) buffer += item;

                    if (depth == 0)
                    {
                        if (kw.Trim() == "namespace")
                        {
                            NamespaceElement elem = new(name.Trim());
                            elem.Children = Parse(buffer);
                            elements.Add(elem);
                        }
                        else if (kw.Trim() == "class")
                        {
                            ClassElement elem = new(name.Trim());
                            elem.Children = Parse(buffer);
                            elements.Add(elem);
                        }
                        else if (kw.Trim() == "function")
                        {
                            FunctionElement elem = new(name.Trim());
                            elem.Children = Parse(buffer);
                            elements.Add(elem);
                        }
                        else
                        {
                            commandbuffer += kw + name +"{"+buffer+"}";
                        }
                        //Console.WriteLine("Buffer: " + buffer);
                        //Console.WriteLine("depth: " + depth);
                        //Console.WriteLine("name: " + name);
                        //Console.WriteLine("kw: " + kw);
                        //Console.WriteLine();

                        kw = "";
                        name = "";
                        buffer = "";
                        depth = 0;
                    }
                }
                else if (depth == 0 && item == ';')
                {
                    if (commandbuffer != "")
                    {
                        CommandElement elem = new(commandbuffer+kw + name);
                        elements.Add(elem);
                    }
                    else
                    {
                        CommandElement elem = new(kw + name + buffer);
                        elements.Add(elem);
                    }
                    
                    //Console.WriteLine("Buffer: " + buffer);
                    //Console.WriteLine("depth: " + depth);
                    //Console.WriteLine("name: " + name);
                    //Console.WriteLine("kw: " + kw);
                    //Console.WriteLine();

                    kw = "";
                    name = "";
                    buffer = "";
                    commandbuffer = "";
                }
                else if (depth > 0)
                {
                    buffer += item;
                }
                else if (kw.Length > 0 && kw.EndsWith(" "))
                {
                    name += item;
                }
                else
                {
                    kw += item;
                }
            }

            return elements;
        }
    }
}
