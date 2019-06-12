/*
 * Copyright 2018 Google LLC.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

using com.google.cloud.tools.jib.filesystem;
using Jib.Net.Core.Global;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.api
{
    /**
     * Represents a Unix-style path in relative form (does not start at the file system root {@code /}).
     *
     * <p>This class is immutable and thread-safe.
     */

    public sealed class RelativeUnixPath
    {
        /**
         * Gets a new {@link RelativeUnixPath} from a Unix-style path in relative form. The {@code path}
         * must be relative (does not begin with a leading slash {@code /}).
         *
         * @param relativePath the relative path
         * @return a new {@link RelativeUnixPath}
         */
        public static RelativeUnixPath get(string relativePath)
        {
            Preconditions.checkArgument(
                !relativePath.StartsWith("/"), "Path starts with forward slash (/): " + relativePath);

            return new RelativeUnixPath(UnixPathParser.parse(relativePath));
        }

        private readonly ImmutableArray<string> pathComponents;

        /** Instantiate with {@link #get}. */
        private RelativeUnixPath(ImmutableArray<string> pathComponents)
        {
            this.pathComponents = pathComponents;
        }

        /**
         * Gets the relative Unix path this represents, in a list of components.
         *
         * @return the relative path this represents, in a list of components
         */
        public ImmutableArray<string> getRelativePathComponents()
        {
            return pathComponents;
        }
    }
}
