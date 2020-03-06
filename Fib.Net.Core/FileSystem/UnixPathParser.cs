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

using System.Collections.Immutable;

namespace Fib.Net.Core.FileSystem
{
    /** Parses Unix-style paths. */
    public sealed class UnixPathParser
    {
        /**
         * Parses a Unix-style path into a list of path components.
         *
         * @param unixPath the Unix-style path
         * @return a list of path components
         */
        public static ImmutableArray<string> Parse(string unixPath)
        {
            ImmutableArray<string>.Builder pathComponents = ImmutableArray.CreateBuilder<string>();
            foreach (string pathComponent in Splitter.On('/').Split(unixPath))
            {
                if (string.IsNullOrEmpty(pathComponent))
                {
                    // Skips empty components.
                    continue;
                }
                pathComponents.Add(pathComponent);
            }
            return pathComponents.ToImmutable();
        }

        private UnixPathParser() { }
    }
}
