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
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Jolt.Net.Functions.Strings
{
    public abstract class StringFunction : SingleFunction
    {
        protected override JsonNode ApplySingle(JsonNode arg)
        {
            if (arg.GetNodeKind() != JsonValueKind.String)
            {
                return null;
            }
            return ApplyString(arg.ToString());
        }

        protected abstract string ApplyString(string value);
    }

    public class ToLowerCase : StringFunction
    {
        protected override string ApplyString(string arg) =>
            arg.ToLowerInvariant();
    }

    public class ToUpperCase : StringFunction
    {
        protected override string ApplyString(string arg) =>
            arg.ToUpperInvariant();
    }

    public class Trim : StringFunction
    {
        protected override string ApplyString(string value) =>
            value.Trim();
    }

    public class Concat : ListFunction
    {
        protected override JsonNode ApplyList(JsonArray argList)
        {
            var sb = new StringBuilder();
            foreach (var arg in argList)
            {
                if (arg != null)
                {
                    sb.Append(arg.ToString());
                }
            }
            return sb.ToString();
        }
    }

    public class Substring : ListFunction
    {
        protected override JsonNode ApplyList(JsonArray argList)
        {
            // if argList is null or not the right size; bail
            if (argList == null || argList.Count != 3)
            {
                return null;
            }

            if (!(argList[0].GetNodeKind() == JsonValueKind.String &&
                  argList[1].GetNodeKind() == JsonValueKind.Integer &&
                  argList[2].GetNodeKind() == JsonValueKind.Integer))
            {
                return null;
            }

            // If we get here, then all these casts should work.
            string tuna = argList[0].Value<string>();
            int start = argList[1].Value<int>();
            int end = argList[2].Value<int>();

            // do start and end make sense?
            if (start >= end || start < 0 || end < 1 || end > tuna.Length)
            {
                return null;
            }

            return tuna.Substring(start, end - start);
        }
    }

    public class Join : ArgDrivenStringListFunction
    {
        protected override JsonNode ApplyList(string specialArg, JsonArray args)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < args.Count; ++i)
            {
                object arg = args[i];
                if (arg != null)
                {
                    string argString = arg.ToString();
                    if (!String.IsNullOrEmpty(argString))
                    {
                        sb.Append(argString);
                        if (i < args.Count - 1)
                        {
                            sb.Append(specialArg);
                        }
                    }
                }
            }
            return sb.ToString();
        }
    }

    public class Split : ArgDrivenSingleStringFunction
    {
        protected override JsonNode ApplySingle(string separator, JsonNode source)
        {
            if (source == null || separator == null || source.GetNodeKind() != JsonValueKind.String)
            {
                return null;
            }
            // only try to split input strings
            string inputString = source.ToString();
            return new JsonArray(Regex.Split(inputString, separator));
        }
    }

    public abstract class PadFunction : ArgDrivenStringListFunction
    {
        protected static JsonNode PadString(bool leftPad, string source, JsonArray args)
        {
            if (source == null || args == null || args.Count < 2 ||
                !(args[0].GetNodeKind() == JsonValueKind.Integer &&
                  args[1].GetNodeKind() == JsonValueKind.String))
            {
                return null;
            }

            int width = args[0].Value<int>();

            // if the width param is stupid; bail
            if (width <= 0 || width > 500)
            {
                return null;
            }

            string filler = args[1].ToString();

            // filler can only be a single char
            //  otherwise the math becomes hard
            if (filler.Length != 1)
            {
                return null;
            }

            char fillerChar = filler[0];

            // if the desired width of the overall padding is smaller than
            //  the source string, then just return the source string.
            if (width <= source.Length)
            {
                return source;
            }

            int padLength = width - source.Length;
            StringBuilder sb = new StringBuilder();

            if (leftPad)
            {
                sb.Append(fillerChar, padLength).Append(source);
            }
            else
            {
                sb.Append(source).Append(fillerChar, padLength);
            }
            return sb.ToString();
        }
    }

    public class LeftPad : PadFunction
    {
        protected override JsonNode ApplyList(string source, JsonArray args)
        {
            return PadString(true, source, args);
        }
    }

    public class RightPad : PadFunction
    {
        protected override JsonNode ApplyList(string source, JsonArray args)
        {
            return PadString(false, source, args);
        }
    }
}
