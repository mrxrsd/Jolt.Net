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
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jolt.Net
{
    class SelfLookupArg : FunctionArg
    {
        public readonly TransposePathElement _pathElement;

        public SelfLookupArg(PathEvaluatingTraversal traversal)
        {
            IPathElement pathElement = traversal.Get(traversal.Size() - 1);
            if (pathElement is TransposePathElement tpe)
            {
                _pathElement = tpe;
            }
            else
            {
                throw new SpecException("Expected @ path element here");
            }
        }

        public override JsonNode EvaluateArg(WalkedPath walkedPath, JsonObject context)
        {
            return _pathElement.ObjectEvaluate(walkedPath);
        }
    }

    class ContextLookupArg : FunctionArg
    {
        private readonly PathEvaluatingTraversal _traversal;

        public ContextLookupArg(PathEvaluatingTraversal traversal)
        {
            _traversal = traversal;
        }

        public override JsonNode EvaluateArg(WalkedPath walkedPath, JsonObject context)
        {
            return _traversal.Read(context, walkedPath);
        }
    }

    class LiteralArg : FunctionArg
    {
        private JsonNode _returnValue;

        public LiteralArg(JsonNode rv)
        {
            _returnValue = rv;
        }

        public override JsonNode EvaluateArg(WalkedPath walkedPath, JsonObject context)
        {
            return _returnValue;
        }
    }


    public abstract class FunctionArg
    {
        public static FunctionArg ForSelf(PathEvaluatingTraversal traversal)
        {
            return new SelfLookupArg(traversal);
        }

        public static FunctionArg ForContext(PathEvaluatingTraversal traversal)
        {
            return new ContextLookupArg(traversal);
        }

        public static FunctionArg ForLiteral(JsonNode obj, bool parseArg)
        {
            if (parseArg)
            {
                if (obj.GetNodeKind() == JsonValueKind.String)
                {
                    string arg = obj.ToString();
                    if (arg.Length == 0)
                    {
                        return new LiteralArg(null);
                    }
                    else if (arg.StartsWith("'") && arg.EndsWith("'"))
                    {
                        return new LiteralArg(JsonValue.CreateString(arg.Substring(1, arg.Length - 2)));
                    }
                    else if (arg.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        return new LiteralArg(new JsonValue(true));
                    }
                    else if (arg.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        return new LiteralArg(new JsonValue(false));
                    }
                    else
                    {
                        if (Int64.TryParse(arg, out var lVal))
                        {
                            return new LiteralArg(new JsonValue(lVal));
                        }
                        if (Double.TryParse(arg, out var numVal))
                        {
                            return new LiteralArg(new JsonValue(numVal));
                        }
                        return new LiteralArg(arg);
                    }
                }
                else
                {
                    return new LiteralArg(obj);
                }
            }
            else
            {
                return new LiteralArg(obj);
            }
        }

        public abstract JsonNode EvaluateArg(WalkedPath walkedPath, JsonObject context);
    }
}
