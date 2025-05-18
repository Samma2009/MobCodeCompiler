using System.Text;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace MobCode
{
    public static class Macros
    {
        public static Dictionary<string, Macro> MacroList = new Dictionary<string, Macro>(StringComparer.OrdinalIgnoreCase);

        public static void LoadMacros()
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "ComplierMacros");
            if (!Directory.Exists(dir)) return;

            foreach (var item in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    var m = JsonConvert.DeserializeObject<Macro>(File.ReadAllText(item));
                    MacroList[m.evaluator] = m;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading macro from file: {item}; {ex}");
                }
            }
        }

        public static string EvaluateMacro(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var sb = new StringBuilder();
            int pos = 0, len = input.Length;

            while (pos < len)
            {
                int openParen = input.IndexOf('(', pos);
                if (openParen < 0)
                {
                    sb.Append(input.Substring(pos));
                    break;
                }

                int startName = openParen - 1;
                while (startName >= pos && (char.IsLetterOrDigit(input[startName]) || input[startName] == '.' || input[startName] == '_'))
                    startName--;
                startName++;

                string evaluator = input.Substring(startName, openParen - startName);
                if (!MacroList.TryGetValue(evaluator, out var macro))
                {
                    sb.Append(input.Substring(pos, openParen - pos + 1));
                    pos = openParen + 1;
                    continue;
                }

                int depth = 1;
                int i = openParen + 1;
                for (; i < len; i++)
                {
                    if (input[i] == '(') depth++;
                    else if (input[i] == ')')
                    {
                        depth--;
                        if (depth == 0) break;
                    }
                }

                if (i >= len)
                {
                    sb.Append(input.Substring(pos));
                    break;
                }

                string argsPart = input.Substring(openParen + 1, i - openParen - 1);
                string evaluated = EvaluateMacroCall(evaluator, argsPart);

                sb.Append(input.Substring(pos, startName - pos));
                sb.Append(evaluated);
                pos = i + 1;
            }

            return sb.ToString().Trim();
        }

        private static string EvaluateMacroCall(string evaluator, string argsPart)
        {
            var macro = MacroList[evaluator];
            var args = ParseArguments(argsPart);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < macro.arguments.Length; i++)
            {
                string name = macro.arguments[i];
                string value = i < args.Count ? args[i] : string.Empty;
                if (value.Length >= 2 && ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'"))))
                    value = value.Substring(1, value.Length - 2);
                values[name] = value;
            }

            string result = macro.result;
            foreach (var kvp in values)
                result = result.Replace("%" + kvp.Key + "%", kvp.Value);

            return result;
        }

        private static List<string> ParseArguments(string input)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(input)) return list;

            var sb = new StringBuilder();
            bool inQuotes = false;
            char quoteChar = '\0';
            int braceDepth = 0, bracketDepth = 0;

            foreach (char c in input)
            {
                if (inQuotes)
                {
                    sb.Append(c);
                    if (c == quoteChar && sb[sb.Length - 2] != '\\') inQuotes = false;
                }
                else
                {
                    switch (c)
                    {
                        case '"':
                        case '\'':
                            inQuotes = true;
                            quoteChar = c;
                            sb.Append(c);
                            break;
                        case '{': braceDepth++; sb.Append(c); break;
                        case '}': braceDepth = Math.Max(0, braceDepth - 1); sb.Append(c); break;
                        case '[': bracketDepth++; sb.Append(c); break;
                        case ']': bracketDepth = Math.Max(0, bracketDepth - 1); sb.Append(c); break;
                        case ',' when braceDepth == 0 && bracketDepth == 0:
                            list.Add(sb.ToString().Trim()); sb.Clear(); break;
                        default:
                            sb.Append(c); break;
                    }
                }
            }
            if (sb.Length > 0) list.Add(sb.ToString().Trim());
            return list;
        }
    }

    public struct Macro
    {
        [JsonInclude]
        public string evaluator;
        [JsonInclude]
        public string[] arguments;
        [JsonInclude]
        public string result;
    }
}
