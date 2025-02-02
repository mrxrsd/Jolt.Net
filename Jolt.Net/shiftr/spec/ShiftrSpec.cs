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


using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Jolt.Net
{

    /**
     * A Spec object represents a single line from the JSON Shiftr Spec.
     *
     * At a minimum a single Spec has :
     *   Raw LHS spec value
     *   Some kind of PathElement (based off that raw LHS value)
     *
     * Additionally there are 2 distinct subclasses of the base Spec
     *  LeafSpec : where the RHS is a string or Array of Strings, that specify an write path for the data from this level in the tree
     *  CompositeSpec : where the RHS is a map of children Specs
     *
     * Mapping of JSON Shiftr Spec to Spec objects :
     * {
     *   rating-*" : {      // CompositeSpec with one child and a Star PathElement
     *     "&(1)" : {       // CompositeSpec with one child and a Reference PathElement
     *       "foo: {        // CompositeSpec with one child and a Literal PathElement
     *         "value" : "Rating-&1.value"  // OutputtingSpec with a Literal PathElement and one write path
     *       }
     *     }
     *   }
     * }
     *
     * The tree structure of formed by the CompositeSpecs is what is used during Shiftr transforms
     *  to do the parallel tree walk with the input data tree.
     *
     * During the parallel tree walk a stack of data (a WalkedPath) is maintained, and used when
     *  a tree walk encounters an Outputting spec to evaluate the wildcards in the write DotNotationPath.
     */
    public abstract class ShiftrSpec : IBaseSpec
    {
        // The processed key from the JSON config
        protected readonly IMatchablePathElement _pathElement;

        public ShiftrSpec(string rawJsonKey)
        {
            _pathElement = PathElementBuilder.BuildMatchablePathElement(rawJsonKey);
        }

        public IMatchablePathElement GetPathElement()
        {
            return _pathElement;
        }

        public abstract bool Apply(string inputKey, JsonNode inputOptional, WalkedPath walkedPath, JsonObject output, JsonObject context);
    }
}
