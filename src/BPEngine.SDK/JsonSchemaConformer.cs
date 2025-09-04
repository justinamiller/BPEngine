using System.Text.Json;
using System.Text.Json.Nodes;

namespace BPEngine.SDK;

internal static class JsonSchemaConformer
{
    // Extremely small subset: supports "type": "object", "properties": {}, "required": [], arrays of strings.
    // Expand as needed.
    public static JsonObject Conform(string schemaJson, IDictionary<string, string> extracted)
    {
        var schema = JsonNode.Parse(schemaJson)?.AsObject()
                     ?? throw new ArgumentException("Invalid JSON schema");

        var result = new JsonObject();

        var props = schema["properties"] as JsonObject ?? new JsonObject();
        var required = schema["required"] as JsonArray ?? new JsonArray();

        // Fill what we can from extracted map, coerce as strings/arrays
        foreach (var kv in props)
        {
            var key = kv.Key;
            var def = kv.Value!.AsObject();
            var type = def["type"]?.GetValue<string>() ?? "string";

            if (extracted.TryGetValue(key, out var val))
            {
                if (type == "string")
                    result[key] = val;
                else if (type == "array")
                {
                    var itemsType = def["items"]?["type"]?.GetValue<string>() ?? "string";
                    var arr = new JsonArray();
                    foreach (var line in SplitLines(val))
                        arr.Add(itemsType == "string" ? JsonValue.Create(line) : JsonValue.Create(line));
                    result[key] = arr;
                }
                else
                {
                    result[key] = val;
                }
            }
        }

        // Ensure required keys exist
        foreach (var req in required)
        {
            var key = req!.GetValue<string>();
            if (!result.ContainsKey(key))
            {
                var def = props[key]?.AsObject();
                var type = def?["type"]?.GetValue<string>() ?? "string";
                result[key] = type switch
                {
                    "string" => "",
                    "array" => new JsonArray(),
                    "object" => new JsonObject(),
                    _ => ""
                };
            }
        }

        return result;
    }

    public static bool TryValidate(string schemaJson, JsonObject obj, out string error)
    {
        // Minimal validation: check required fields present and types roughly match.
        error = string.Empty;
        var schema = JsonNode.Parse(schemaJson)?.AsObject();
        if (schema is null) { error = "Invalid schema"; return false; }

        var props = schema["properties"] as JsonObject ?? new JsonObject();
        var required = schema["required"] as JsonArray ?? new JsonArray();

        foreach (var req in required)
        {
            var key = req!.GetValue<string>();
            if (!obj.ContainsKey(key)) { error = $"Missing required '{key}'"; return false; }
        }

        foreach (var kv in props)
        {
            var key = kv.Key;
            if (!obj.ContainsKey(key)) continue;
            var expected = kv.Value!.AsObject();
            var type = expected["type"]?.GetValue<string>() ?? "string";

            if (type == "string" && obj[key] is not JsonValue) { error = $"'{key}' should be string"; return false; }
            if (type == "array" && obj[key] is not JsonArray) { error = $"'{key}' should be array"; return false; }
            if (type == "object" && obj[key] is not JsonObject) { error = $"'{key}' should be object"; return false; }
        }
        return true;
    }

    private static IEnumerable<string> SplitLines(string s)
        => s.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
