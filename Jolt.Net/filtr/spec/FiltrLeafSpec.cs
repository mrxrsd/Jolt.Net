﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Jolt.Net
{
    interface IValueFiltr
    {
        bool Match(JsonNode value);
    }

    class RegexFiltr : IValueFiltr
    {
        private readonly Regex _regex;

        public RegexFiltr(string re)
        {
            _regex = new Regex(re);
        }

        public bool Match(JsonNode value) =>
            _regex.IsMatch(value.Value<string>());
    }

    class ValueFiltr : IValueFiltr
    {
        private readonly JsonNode _value;

        public ValueFiltr(JsonNode value)
        {
            _value = value;
        }

        public bool Match(JsonNode value) =>
            value.Equals(_value);
    }

    class FiltrLeafSpec
    {
        private readonly IReadOnlyList<KeyValuePair<string, IValueFiltr>> _filters;

        public FiltrLeafSpec(IReadOnlyList<KeyValuePair<string, JsonNode>> filters)
        {
            _filters = filters.Select(x =>
                new KeyValuePair<string, IValueFiltr>(x.Key,
                    x.Value.Type == JsonNodeType.String ? (IValueFiltr)
                        new RegexFiltr(x.Value.Value<string>()) :
                        new ValueFiltr(x.Value))).ToList().AsReadOnly();
        }

        public bool Matches(JsonNode input)
        {
            if (input is JsonArray arr)
            {
                foreach (var filter in _filters)
                {
                    if (Int32.TryParse(filter.Key, out var index) &&
                        index >= 0 && index < arr.Count)
                    {
                        if (filter.Value.Match(arr[index]))
                        {
                            return true;
                        }
                    }
                }
            }
            else if (input is JsonObject obj)
            {
                foreach (var filter in _filters)
                {
                    if (obj.TryGetValue(filter.Key, out var value))
                    {
                        if (filter.Value.Match(value))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                foreach (var filter in _filters)
                {
                    if (filter.Value.Match(input))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
