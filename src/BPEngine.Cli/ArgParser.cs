using System;
using System.Collections.Generic;
using System.Linq;

namespace BPEngine.Cli
{
    internal static class ArgParser
    {
        /// <summary>
        /// Splits the argv into: command (first token) + the rest.
        /// Works on older language versions (no tuples).
        /// </summary>
        public static void SplitCommand(string[] args, out string command, out string[] rest)
        {
            if (args is null || args.Length == 0)
            {
                command = string.Empty;
                rest = Array.Empty<string>();
                return;
            }
            command = args[0];
            rest = args.Skip(1).ToArray();
        }

        /// <summary>
        /// Parses flags of the form:
        ///   --key value
        ///   --key=value
        /// Repeated flags are allowed (values are collected).
        /// Non-flag tokens are returned as "positionals".
        /// </summary>
        public static (Dictionary<string, List<string>> Flags, List<string> Positionals) Parse(string[] args)
        {
            var flags = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var pos = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];

                if (a.StartsWith("--", StringComparison.Ordinal))
                {
                    // Support --key=value
                    var eq = a.IndexOf('=', StringComparison.Ordinal);
                    if (eq > 2)
                    {
                        var key = a.Substring(0, eq);
                        var val = a.Substring(eq + 1);
                        Add(flags, key, val);
                        continue;
                    }

                    // Support --key value   (value optional)
                    var keyOnly = a;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        Add(flags, keyOnly, args[++i]);
                    }
                    else
                    {
                        // boolean-style switch
                        Add(flags, keyOnly, "");
                    }
                }
                else
                {
                    pos.Add(a);
                }
            }

            return (flags, pos);

            static void Add(Dictionary<string, List<string>> d, string k, string v)
            {
                if (!d.TryGetValue(k, out var list))
                {
                    list = new List<string>();
                    d[k] = list;
                }
                list.Add(v);
            }
        }

        // ---- small helpers for commands ----

        public static string Require(this Dictionary<string, List<string>> flags, string name, string? err = null)
        {
            if (!flags.TryGetValue(name, out var list) || list.Count == 0 || list[0].IsNullOrWhiteSpace())
                throw new ArgumentException(err ?? $"Missing required flag {name}");
            return list[0];
        }

        public static string? Optional(this Dictionary<string, List<string>> flags, string name)
            => flags.TryGetValue(name, out var list) && list.Count > 0 ? list[0] : null;

        public static IEnumerable<string> Multi(this Dictionary<string, List<string>> flags, string name)
            => flags.TryGetValue(name, out var list) ? list : Array.Empty<string>();
    }
}
