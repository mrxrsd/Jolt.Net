using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jolt.Net.utils
{
    public static class JsonNodeExtensions
    {
        public static JsonValueKind GetNodeKind(this JsonNode @node)
        {
            return @node.GetValue<JsonElement>().ValueKind;
        }
    }
}
