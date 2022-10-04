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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jolt.Net
{
    /**
     * Leaf level CardinalitySpec object.
     * <p/>
     * If this CardinalitySpec's PathElement matches the input (successful parallel tree walk)
     * this CardinalitySpec has the information needed to write the given data to the output object.
     */
    public class CardinalityLeafSpec : CardinalitySpec
    {
        public enum CardinalityRelationship
        {
            ONE,
            MANY
        }

        private CardinalityRelationship _cardinalityRelationship;

        public CardinalityLeafSpec(string rawKey, object rhs) :
            base(rawKey)
        {
            string s = rhs.ToString();
            if (!Enum.TryParse<CardinalityRelationship>(s, out _cardinalityRelationship))
            {
                throw new SpecException("Invalid Cardinality type :" + s);
            }
        }

        /**
         * If this CardinalitySpec matches the inputkey, then do the work of modifying the data and return true.
         *
         * @return true if this this spec "handles" the inputkey such that no sibling specs need to see it
         */
        public override bool ApplyCardinality(string inputKey, JsonNode input, WalkedPath walkedPath, JsonNode parentContainer)
        {
            MatchedElement thisLevel = GetMatch(inputKey, walkedPath);
            if (thisLevel == null)
            {
                return false;
            }
            PerformCardinalityAdjustment(inputKey, input, walkedPath, (JsonObject) parentContainer, thisLevel);
            return true;
        }

        /**
         * This should only be used by composite specs with an '@' child
         *
         * @return null if no work was done, otherwise returns the re-parented data
         */
        public JsonNode ApplyToParentContainer(string inputKey, JsonNode input, WalkedPath walkedPath, JsonNode parentContainer)
        {
            MatchedElement thisLevel = GetMatch(inputKey, walkedPath);
            if (thisLevel == null)
            {
                return null;
            }
            return PerformCardinalityAdjustment(inputKey, input, walkedPath, (JsonObject) parentContainer, thisLevel);
        }

        /**
         *
         * @return null if no work was done, otherwise returns the re-parented data
         */
        private JsonNode PerformCardinalityAdjustment(string inputKey, JsonNode input, WalkedPath walkedPath, JsonObject parentContainer, MatchedElement thisLevel)
        {
            // Add our the LiteralPathElement for this level, so that write path References can use it as &(0,0)
            walkedPath.Add(input, thisLevel);

            JsonNode returnValue = null;
            if (_cardinalityRelationship == CardinalityRelationship.MANY)
            {
                if (input is JsonArray)
                {
                    returnValue = input;
                }
                else if (input.GetNodeKind() == JsonValueKind.Object || input.GetNodeKind() == JsonValueKind.String || 
                         input.GetNodeKind() == JsonValueKind.Number || input.GetNodeKind() == JsonValueKind.True || 
                         input.GetNodeKind() == JsonValueKind.False)
                {
                    var one = parentContainer[inputKey];
                    parentContainer.Remove(inputKey);
                    var tempList = new JsonArray();
                    tempList.Add(one);
                    returnValue = tempList;
                }
                else if (input.GetNodeKind() == JsonValueKind.Null)
                {
                    returnValue = new JsonArray();
                }
                parentContainer[inputKey] = returnValue;
            }
            else if (_cardinalityRelationship == CardinalityRelationship.ONE)
            {
                if (input is JsonArray l)
                {
                    // The value is first removed from the array in order to prevent it
                    // from being cloned (JContainers clone their inputs if they are already
                    // part of an object graph)
                    if (l.Count > 0)
                    {
                        returnValue = l[0];
                        l.RemoveAt(0);
                    }
                    parentContainer[inputKey] = returnValue;
                }
            }

            walkedPath.RemoveLast();

            return returnValue;
        }

        private MatchedElement GetMatch(string inputKey, WalkedPath walkedPath)
        {
            return GetPathElement().Match(inputKey, walkedPath);
        }
    }
}
