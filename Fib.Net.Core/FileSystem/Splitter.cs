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

using System;
using System.Collections.Generic;

namespace Fib.Net.Core.FileSystem
{
    internal abstract class Splitter
    {
        private class CharSplitter : Splitter
        {
            private readonly char v;

            public CharSplitter(char v)
            {
                this.v = v;
            }

            internal override IList<string> SplitToList(string s)
            {
                return s.Split(new[] { v }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        internal static Splitter On(char v)
        {
            return new CharSplitter(v);
        }

        internal static Splitter On(string v)
        {
            return new StringSplitter(v);
        }

        internal abstract IList<string> SplitToList(string s);

        internal IEnumerable<string> Split(string s)
        {
            return SplitToList(s);
        }

        private class StringSplitter : Splitter
        {
            private readonly string v;

            public StringSplitter(string v)
            {
                this.v = v;
            }

            internal override IList<string> SplitToList(string s)
            {
                return s.Split(new[] { v }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }
}