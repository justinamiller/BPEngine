using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BPEngine.SDK.Chat
{
    /// <summary>
    /// Minimal function-calling registry. Tools are async delegates that accept a JsonObject of args.
    /// </summary>
    public static class ToolRegistry
    {
        public delegate Task<string> ToolHandler(JsonObject args);

        private static readonly Dictionary<string, ToolHandler> _tools =
            new(StringComparer.OrdinalIgnoreCase);

        public static void Register(string name, ToolHandler handler)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("tool name required", nameof(name));
            _tools[name] = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public static bool TryGet(string name, out ToolHandler handler) => _tools.TryGetValue(name, out handler);

        /// <summary>
        /// Try to parse a tool call payload:
        /// {"tool":"name","args":{...}}
        /// Returns null if it doesn't look like a tool call JSON.
        /// </summary>
        public static (string tool, JsonObject args)? TryParseToolJson(string text)
        {
            try
            {
                var node = JsonNode.Parse(text) as JsonObject;
                if (node is null) return null;
                var tool = node["tool"]?.GetValue<string>();
                var args = node["args"] as JsonObject;
                if (string.IsNullOrWhiteSpace(tool) || args is null) return null;
                return (tool, args);
            }
            catch
            {
                return null;
            }
        }
    }
}
