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

using Fib.Net.Core.Api;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Fib.Net.Core.FileSystem
{
    /** Recursively applies a function to each file in a directory. */
    public class DirectoryWalker
    {
        private readonly SystemPath rootDir;

        private Func<SystemPath, bool> pathFilter = _ => true;

        /**
         * Initialize with a root directory to walk.
         *
         * @param rootDir the root directory.
         * @throws NotDirectoryException if the root directory is not a directory.
         */
        public DirectoryWalker(SystemPath rootDir)
        {
            rootDir = rootDir ?? throw new ArgumentNullException(nameof(rootDir));
            if (!Files.IsDirectory(rootDir))
            {
                throw new ArgumentException(rootDir + " is not a directory", nameof(rootDir));
            }
            this.rootDir = rootDir;
        }

        /**
         * Adds a filter to the walked paths.
         *
         * @param pathFilter the filter. {@code pathFilter} returns {@code true} if the path should be
         *     accepted and {@code false} otherwise.
         * @return this
         */
        public DirectoryWalker Filter(Func<SystemPath, bool> additionalPathFilter)
        {
            var currentPathFilter = pathFilter;
            pathFilter = p => currentPathFilter(p) && additionalPathFilter(p);
            return this;
        }

        /**
         * Filters away the {@code rootDir}.
         *
         * @return this
         */
        public DirectoryWalker FilterRoot()
        {
            Filter(path => !path.Equals(rootDir));
            return this;
        }

        /**
         * Walks {@link #rootDir} and applies {@code pathConsumer} to each file. Note that {@link
         * #rootDir} itself is visited as well.
         *
         * @param pathConsumer the consumer that is applied to each file.
         * @return a list of Paths that were walked.
         * @throws IOException if the walk fails.
         */
        public ImmutableArray<SystemPath> Walk(PathConsumer pathConsumer)
        {
            ImmutableArray<SystemPath> files = Walk();
            foreach (SystemPath path in files)
            {
                pathConsumer.Accept(path);
            }
            return files;
        }

        /**
         * Walks {@link #rootDir}.
         *
         * @return the walked files.
         * @throws IOException if walking the files fails.
         */
        public ImmutableArray<SystemPath> Walk()
        {
            IEnumerable<SystemPath> fileStream = Files.Walk(rootDir);
            {
                return fileStream.Where(pathFilter).OrderBy(i => i).ToImmutableArray();
            }
        }
    }
}
