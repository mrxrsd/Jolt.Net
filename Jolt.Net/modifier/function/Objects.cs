/*
 * Copyright 2013 Bazaarvoice, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Jolt.Net.utils;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;



namespace Jolt.Net.Functions.Objects
{
    public class ToInteger : SingleFunction
    {
        protected override JsonNode ApplySingle(JsonNode arg)
        {
            if (arg.GetNodeKind() == JsonValueKind.Integer ||
                arg.GetNodeKind() == JsonValueKind.Float)
            {
                return arg.Value<int>();
            }
            if (arg.GetNodeKind() == JsonValueKind.String &&
                Int32.TryParse(arg.Value<string>(), out var intVal))
            {
                return intVal;
            }
            return null;
        }
    }

    public class ToLong : SingleFunction
    {
        protected override JsonNode ApplySingle(JsonNode arg)
        {
            if (arg.GetNodeKind() == JsonValueKind.Integer ||
                arg.GetNodeKind() == JsonValueKind.Float)
            {
                return arg.Value<long>();
            }
            if (arg.GetNodeKind() == JsonValueKind.String &&
                Int64.TryParse(arg.Value<string>(), out var longVal))
            {
                return longVal;
            }
            return null;
        }
    }

    public class ToDouble : SingleFunction
    {
        protected override JsonNode ApplySingle(JsonNode arg)
        {
            if (arg.GetNodeKind() == JsonValueKind.Integer ||
                arg.GetNodeKind() == JsonValueKind.Float)
            {
                return arg.Value<double>();
            }
            if (arg.GetNodeKind() == JsonValueKind.String &&
                Double.TryParse(arg.Value<string>(), out var doubleVal))
            {
                return doubleVal;
            }
            return null;
        }
    }

    public class ToBoolean : SingleFunction
    {
        protected override JsonNode ApplySingle(JsonNode arg)
        {
            if (arg.GetNodeKind() == JsonValueKind.Boolean)
            {
                return arg;
            }
            if (arg.GetNodeKind() == JsonValueKind.String)
            {
                string s = arg.Value<string>();
                if (s.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (s.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return null;
        }
    }

    public class ToString : SingleFunction
    {
        private string TokenToString(JsonNode arg)
        {
            if (arg.GetNodeKind() == JsonValueKind.String)
            {
                return arg.Value<string>();
            }
            if (arg.GetNodeKind() == JsonValueKind.Array)
            {
                var sb = new StringBuilder();
                sb.Append("[");
                foreach (var elt in (JsonArray)arg)
                {
                    sb.Append(TokenToString(elt));
                }
                sb.Append("]");
                return sb.ToString();
            }
            return arg.ToString();
        }

        protected override JsonNode ApplySingle(JsonNode arg)
        {
            if (arg.GetNodeKind() == JsonValueKind.String)
            {
                return arg;
            }
            return TokenToString(arg);
        }
    }

    public abstract class SquashFunction : IFunction
    {
        protected abstract JsonNode DoSquash(JsonNode arg);

        public JsonNode Apply(params JsonNode[] args)
        {
            if (args.Length == 0)
            {
                return null;
            }
            if (args.Length == 1)
            {
                return DoSquash(args[0]);
            }
            var arr = new JsonArray();
            foreach (var arg in args)
            {
                arr.Add(arg);
            }
            return DoSquash(arr);
        }
    }

    public class SquashNulls : SquashFunction
    {
        public static JsonNode Squash(JsonNode arg)
        {
            if (arg.GetNodeKind() == JsonValueKind.Array)
            {
                var arr = (JsonArray)arg;
                for (int i = 0; i < arr.Count;)
                {
                    if (arr[i].GetNodeKind() == JsonValueKind.Null)
                        arr.RemoveAt(i);
                    else
                        ++i;
                }
            }
            else if (arg.GetNodeKind() == JsonValueKind.Object)
            {
                var obj = (JsonObject)arg;
                var newObj = new JsonObject();
                foreach (var kv in obj)
                {
                    if (kv.Value.GetNodeKind() != JsonValueKind.Null)
                    {
                        newObj.Add(kv.Key, kv.Value);
                    }
                }
                return newObj;
            }
            return arg;
        }

        protected override JsonNode DoSquash(JsonNode arg) =>
            Squash(arg);
    }

    public class RecursivelySquashNulls : SquashFunction
    {
        public static JsonNode Squash(JsonNode arg)
        {
            // Makes two passes thru the data.
            arg = SquashNulls.Squash(arg);

            if (arg.GetNodeKind() == JsonValueKind.Array)
            {
                var arr = (JsonArray)arg;
                for (int i = 0; i < arr.Count; ++i)
                {
                    arr[i] = Squash(arr[i]);
                }
            }
            else if (arg.GetNodeKind() == JsonValueKind.Object)
            {
                var obj = (JsonObject)arg;
                foreach (var kv in obj)
                {
                    obj[kv.Key] = Squash(kv.Value);
                }
            }
            return arg;
        }

        protected override JsonNode DoSquash(JsonNode arg) =>
            Squash(arg);
    }

    /**
     * Size is a special snowflake and needs specific care
     */
    public class Size : IFunction
    {
        public JsonNode Apply(params JsonNode[] args)
        {
            if (args == null || args.Length == 0)
            {
                return null;
            }

            if (args.Length == 1)
            {
                if (args[0] == null)
                {
                    return null;
                }
                if (args[0].GetNodeKind() == JsonValueKind.Array)
                {
                    return ((JsonArray)args[0]).Count;
                }
                if (args[0].GetNodeKind() == JsonValueKind.String)
                {
                    return args[0].ToString().Length;
                }
                if (args[0].GetNodeKind() == JsonValueKind.Object)
                {
                    return ((JsonObject)args[0]).Count;
                }
                return null;
            }

            return args.Length;
        }
    }
}
