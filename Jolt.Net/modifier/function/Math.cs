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
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jolt.Net.Functions.Math
{
    /**
     * Given a list of objects, returns the max value in its appropriate type
     * also, interprets string as Number and returns appropriately
     *
     * max(1,2l,3d) == Optional.of(3d)
     * max(1,2l,"3.0") == Optional.of(3.0)
     * max("a", "b", "c") == Optional.empty()
     * max([]) == Optional.empty()
     */
    public class NumberListCompare : ListFunction
    {
        private readonly Func<long?, long, long?> _longCompareFn;
        private readonly Func<double?, double, double?> _doubleCompareFn;
        private readonly Func<double, long, bool> _doubleLongCompareFn;

        public NumberListCompare(
            Func<long?, long, long?> longCompareFn,
            Func<double?, double, double?> doubleCompareFn,
            Func<double, long, bool> doubleLongCompareFn
        )
        {
            _doubleCompareFn = doubleCompareFn;
            _doubleLongCompareFn = doubleLongCompareFn;
            _longCompareFn = longCompareFn;
        }

        protected override JsonNode ApplyList(JsonArray input)
        {
            if (input == null || input.Count == 0)
            {
                return null;
            }

            long? curLong = null;
            double? curDouble = null;

            foreach (var arg in input)
            {
                if (arg.GetNodeKind() == JsonValueKind.Integer)
                {
                    curLong = _longCompareFn(curLong, arg.Value<long>());
                }
                else if (arg.GetNodeKind() == JsonValueKind.Float)
                {
                    curDouble = _doubleCompareFn(curDouble, arg.Value<double>());
                }
                else if (arg.GetNodeKind() == JsonValueKind.String)
                {
                    string s = arg.Value<string>();
                    if (Int64.TryParse(s, out var longVal))
                    {
                        curLong = _longCompareFn(curLong, longVal);
                    }
                    else if (Double.TryParse(s, out var doubleVal))
                    {
                        curDouble = _doubleCompareFn(curDouble, doubleVal);
                    }
                }
            }

            if (curLong.HasValue)
            {
                if (curDouble.HasValue && _doubleLongCompareFn(curDouble.Value, curLong.Value))
                {
                    return curDouble.Value;
                }
                return curLong.Value;
            }
            if (curDouble.HasValue)
            {
                return curDouble.Value;
            }
            return null;
        }
    }

    public class Max : NumberListCompare
    {
        public Max() : base(
            (long? max, long val) => max.HasValue ? System.Math.Max(max.Value, val) : val,
            (double? max, double val) => max.HasValue ? System.Math.Max(max.Value, val) : val,
            (double a, long b) => a > b)
        {
        }
    }

    public class Min : NumberListCompare
    {
        public Min() : base(
            (long? min, long val) => min.HasValue ? System.Math.Min(min.Value, val) : val,
            (double? min, double val) => min.HasValue ? System.Math.Min(min.Value, val) : val,
            (double a, long b) => a < b)
        {
        }
    }

    /**
    * Given any object, returns, if possible. its absolute value wrapped in Optional
    * Interprets string as Number
    *
    * abs("-123") == Optional.of(123)
    * abs("123") == Optional.of(123)
    * abs("12.3") == Optional.of(12.3)
    *
    * abs("abc") == Optional.empty()
    * abs(null) == Optional.empty()
    *
    */
    public class Abs : SingleFunction
    {
        protected override JsonNode ApplySingle(JsonNode arg)
        {
            if (arg.GetNodeKind() == JsonValueKind.Integer)
            {
                return System.Math.Abs(arg.Value<long>());
            }
            if (arg.GetNodeKind() == JsonValueKind.Float)
            {
                return System.Math.Abs(arg.Value<double>());
            }
            if (arg.GetNodeKind() == JsonValueKind.String)
            {
                string s = arg.Value<string>();
                if (Int64.TryParse(s, out var longVal))
                {
                    return System.Math.Abs(longVal);
                }
                if (Int64.TryParse(s, out var doubleVal))
                {
                    return System.Math.Abs(doubleVal);
                }
            }
            return null;
        }
    }

    /**
     * Given a list of numbers, returns their avg as double
     * any value in the list that is not a valid number is ignored
     *
     * avg(2,"2","abc") == Optional.of(2.0)
     */
    public class Avg : ListFunction
    {
        protected override JsonNode ApplyList(JsonArray args)
        {
            double sum = 0.0;
            int count = 0;
            foreach (var arg in args)
            {

                if (arg.GetNodeKind() == JsonValueKind.Integer || arg.GetNodeKind() == JsonValueKind.Float)
                {
                    sum += arg.Value<double>();
                    ++count;
                }
                else if (arg.GetNodeKind() == JsonValueKind.String &&
                         Double.TryParse(arg.Value<string>(), out var doubleVal))
                {
                    sum += doubleVal;
                    ++count;
                }
            }
            if (count == 0)
            {
                return null;
            }
            return sum / count;
        }
    }

    public class IntSum : ListFunction
    {
        protected override JsonNode ApplyList(JsonArray args)
        {
            int sum = 0;
            foreach (var arg in args)
            {
                if (arg.GetNodeKind() == JsonValueKind.Integer ||
                    arg.GetNodeKind() == JsonValueKind.Float)
                {
                    sum += arg.Value<int>();
                }
                else if (arg.GetNodeKind() == JsonValueKind.String &&
                         Double.TryParse(arg.Value<string>(), out var doubleVal))
                {
                    sum += (int)doubleVal;
                }
            }
            return sum;
        }
    }

    public class LongSum : ListFunction
    {
        protected override JsonNode ApplyList(JsonArray args)
        {
            long sum = 0;
            foreach (var arg in args)
            {
                if (arg.GetNodeKind() == JsonValueKind.Integer || arg.GetNodeKind() == JsonValueKind.Float)
                {
                    sum += arg.Value<long>();
                }
                else if (arg.GetNodeKind() == JsonValueKind.String &&
                         Double.TryParse(arg.Value<string>(), out var doubleVal))
                {
                    sum += (long)doubleVal;
                }
            }
            return sum;
        }
    }

    public class DoubleSum : ListFunction
    {
        protected override JsonNode ApplyList(JsonArray args)
        {
            double sum = 0;
            foreach (var arg in args)
            {
                if (arg.Type == JsonValueKind.Integer || arg.Type == JsonValueKind.Float)
                {
                    sum += arg.Value<double>();
                }
                else if (arg.Type == JsonValueKind.String &&
                         Double.TryParse(arg.Value<string>(), out var intVal))
                {
                    sum += intVal;
                }
            }
            return sum;
        }
    }

    public class IntSubtract : ListFunction
    {
        protected override JsonNode ApplyList(JsonArray args)
        {
            if (args == null || args.Count != 2 ||
                args[0].GetNodeKind() != JsonValueKind.Integer ||
                args[1].GetNodeKind() != JsonValueKind.Integer)
            {
                return null;
            }
            return args[0].Value<int>() - args[1].Value<int>();
        }
    }

    public class LongSubtract : ListFunction
    {
        protected override JsonNode ApplyList(JsonArray args)
        {
            if (args == null || args.Count != 2 ||
                args[0].GetNodeKind() != JsonValueKind.Integer ||
                args[1].GetNodeKind() != JsonValueKind.Integer)
            {
                return null;
            }
            return args[0].Value<long>() - args[1].Value<long>();
        }
    }

    public class DoubleSubtract : ListFunction
    {
        protected override JsonNode ApplyList(JsonArray args)
        {
            if (args == null || args.Count != 2 ||
                args[0].GetNodeKind() != JsonValueKind.Float ||
                args[1].GetNodeKind() != JsonValueKind.Float)
            {
                return null;
            }
            
            return args[0].Value<double>() - args[1].Value<double>();
        }
    }

    public class Divide : ListFunction
    {
        public static JsonNode DividePair(JsonArray args)
        {
            if (args == null || args.Count != 2 ||
                (args[0].GetNodeKind() != JsonValueKind.Integer && args[0].GetNodeKind() != JsonValueKind.Float) ||
                (args[1].GetNodeKind() != JsonValueKind.Integer && args[1].GetNodeKind() != JsonValueKind.Float))
            {
                return null;
            }

            double denominator = args[1].Value<double>();
            if (denominator == 0)
            {
                return null;
            }
            double numerator = args[0].Value<double>();
            return numerator / denominator;
        }

        protected override JsonNode ApplyList(JsonArray args) =>
            Divide.DividePair(args);
    }

    public class DivideAndRound : ArgDrivenIntListFunction
    {
        protected override JsonNode ApplyList(int specialArg, JsonArray args)
        {
            JsonNode result = Divide.DividePair(args);
            if (result != null)
            {
                return System.Math.Round(result.Value<double>(), specialArg, MidpointRounding.AwayFromZero);
            }
            return result;
        }
    }
}
