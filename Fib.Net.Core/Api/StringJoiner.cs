// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using System.Collections.Generic;

namespace Fib.Net.Core.Api
{
    internal class StringJoiner
    {
        private readonly List<string> strings = new List<string>();
        private readonly string joiner;
        private readonly string prefix;
        private readonly string postfix;

        public StringJoiner(string separator, string prefix, string postfix)
        {
            this.joiner = separator;
            this.prefix = prefix;
            this.postfix = postfix;
        }

        internal void Add(string value)
        {
            strings.Add(value);
        }

        public override string ToString()
        {
            return prefix + string.Join(joiner, strings) + postfix;
        }
    }
}