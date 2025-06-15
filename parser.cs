using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobCode
{
    public static class Parser
    {
        public static List<HirarchyElemet> Parse(string input,bool parsejsongens = true) 
        {
            List<HirarchyElemet> elements = new List<HirarchyElemet>();

            int depth = 0;
            List<string> modifiers = new() {};
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
                        HirarchyElemet elem = null!;
                        switch (kw.Trim())
                        {
                            case "namespace":
                                elem = new NamespaceElement(name.Trim());
                                break;
                            case "function":
                                elem = new FunctionElement(name.Trim());
                                break;
                            case "while":
                                elem = new WhileElement(name.Trim());
                                break;
                            case "class":
                                elem = new ClassElement(name.Trim());
                                break;
                            case "if":
                                elem = new IfElement(name.Trim());
                                break;
                            case "times":
                                elem = new RepeatElement(name.Trim());
                                break;
                            case "switch":
                                elem = new SwitchElement(name.Trim());
                                break;
                            case "case":
                                elem = new CaseElement(name.Trim());
                                break;
                            default:
                                if (parsejsongens)
                                    elem = new JsonElement(name.Trim(),"{" + buffer + "}",kw.Trim());
                                else
                                    commandbuffer += kw + name + "{" + buffer + "}";
                                break;
                        }
                        if (elem != null)
                        {
                            elem.Modifiers = modifiers.ToArray();
                            if(elem.GetType() != typeof(JsonElement))
                                elem.Children = Parse(buffer,elem.GetType() != typeof(FunctionElement));
                            elements.Add(elem);
                        }

                        kw = "";
                        name = "";
                        buffer = "";
                        depth = 0;
                        modifiers.Clear();
                    }
                }
                else if (depth == 0 && item == ';')
                {
                    if (commandbuffer != "")
                    {
                        CommandElement elem = new(commandbuffer + kw + name);
                        elem.Modifiers = modifiers.ToArray();
                        elements.Add(elem);
                    }
                    else
                    {
                        CommandElement elem = new(kw + name + buffer);
                        elem.Modifiers = modifiers.ToArray();
                        elements.Add(elem);
                    }

                    kw = "";
                    name = "";
                    buffer = "";
                    commandbuffer = "";
                    modifiers.Clear();
                }
                else if (depth > 0)
                {
                    buffer += item;
                }
                else if (kw.Trim().Length > 0 && kw.EndsWith(" "))
                {
                    if (kw.Trim().StartsWith("@"))
                    {
                        modifiers.Add(kw.Trim());
                        kw = item.ToString();
                    }
                    else
                        name += item;
                }
                else
                {
                    kw += item;
                }
            }

            if (kw.Trim() != "")
            {
                Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.Write("Warning:");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(" trailing data encountered \"" + kw + name + buffer + "\"");
                Console.ResetColor();
            }

            if (depth > 0)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.Write("Error:");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" missing \"}\"");
                Console.ResetColor();
            }

            return elements;
        }
    }
}
