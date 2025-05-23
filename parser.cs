using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobCode
{
    public static class Parser
    {
        private static Dictionary<string, string> Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static List<HirarchyElemet> Parse(string input) 
        {
            List<HirarchyElemet> elements = new List<HirarchyElemet>();
            Variables.Clear(); // Clear variables for each new parse

            int depth = 0;
            string kw = "";
            string name = "";
            string buffer = "";
            string commandbuffer = "";
            bool isVariableDeclaration = false;

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
                            commandbuffer += kw + name + "{" + buffer + "}";
                        }
                        kw = "";
                        name = "";
                        buffer = "";
                        depth = 0;
                    }
                }
                else if (depth == 0 && item == ';')
                {
                    string fullCommand = commandbuffer + kw + name + buffer;
                    fullCommand = fullCommand.Trim();
                    
                    if (fullCommand.StartsWith("$") && fullCommand.Contains("="))
                    {
                        var parts = fullCommand.Split('=', 2);
                        string varName = parts[0].Trim().TrimStart('$').TrimEnd(';').Trim();
                        string varValue = parts.Length > 1 ? parts[1].Trim().TrimEnd(';').Trim() : "";
                        // Remove quotes from string literals if present
                        if (varValue.StartsWith("\"") && varValue.EndsWith("\""))
                            varValue = varValue.Substring(1, varValue.Length - 2);
                        Variables[varName] = varValue;
                    }
                    else
                    {
                        CommandElement elem = new(fullCommand);
                        elements.Add(elem);
                    }

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
            
            if (!string.IsNullOrEmpty(commandbuffer + kw + name + buffer))
            {
                string fullCommand = (commandbuffer + kw + name + buffer).Trim();
                if (fullCommand.StartsWith("$") && fullCommand.Contains("="))
                {
                    var parts = fullCommand.Split('=', 2);
                    string varName = parts[0].Trim().TrimStart('$').TrimEnd(';').Trim();
                    string varValue = parts.Length > 1 ? parts[1].Trim().TrimEnd(';').Trim() : "";
                    if (varValue.StartsWith("\"") && varValue.EndsWith("\""))
                        varValue = varValue.Substring(1, varValue.Length - 2);
                    Variables[varName] = varValue;
                }
                else
                {
                    CommandElement elem = new(fullCommand);
                    elements.Add(elem);
                }
            }

            return elements;
        }
        
        public static string SubstituteVariables(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            string result = input;
            foreach (var variable in Variables)
            {
                string placeholder = $"${variable.Key}$";
                result = result.Replace(placeholder, variable.Value, StringComparison.OrdinalIgnoreCase);
            }
            return result;
        }
    }
}
