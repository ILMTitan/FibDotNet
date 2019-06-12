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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.filesystem;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System.Collections.Immutable;

namespace Jib.Net.Core.Api
{
    /**
     * Represents a Unix-style path in absolute form (containing all path components relative to the
     * file system root {@code /}).
     *
     * <p>This class is immutable and thread-safe.
     */
    public sealed class AbsoluteUnixPath
    {
        /**
         * Gets a new {@link AbsoluteUnixPath} from a Unix-style path string. The path must begin with a
         * forward slash ({@code /}).
         *
         * @param unixPath the Unix-style path string in absolute form
         * @return a new {@link AbsoluteUnixPath}
         */
        public static AbsoluteUnixPath get(string unixPath)
        {
            Preconditions.checkArgument(
                unixPath.StartsWith("/"), "Path does not start with forward slash (/): " + unixPath);

            return new AbsoluteUnixPath(UnixPathParser.parse(unixPath));
        }

        /**
         * Gets a new {@link AbsoluteUnixPath} from a {@link Path}. The {@code path} must be absolute
         * (indicated by a non-null {@link Path#getRoot}).
         *
         * @param path the absolute {@link Path} to convert to an {@link AbsoluteUnixPath}.
         * @return a new {@link AbsoluteUnixPath}
         */
        public static AbsoluteUnixPath fromPath(SystemPath path)
        {
            Preconditions.checkArgument(
                path.getRoot() != null, "Cannot create AbsoluteUnixPath from non-absolute Path: " + path);

            ImmutableArray<string>.Builder pathComponents =
                ImmutableArray.CreateBuilder<string>();
            foreach (SystemPath pathComponent in path)
            {
                pathComponents.Add(pathComponent.ToString());
            }
            return new AbsoluteUnixPath(pathComponents.ToImmutable());
        }

        /** Path components after the file system root. This should always match {@link #unixPath}. */
        private readonly ImmutableArray<string> pathComponents;

        /**
         * Unix-style path, in absolute form. Does not end with trailing slash, except for the file system
         * root ({@code /}). This should always match {@link #pathComponents}.
         */
        private readonly string unixPath;

        private AbsoluteUnixPath(ImmutableArray<string> pathComponents)
        {
            this.pathComponents = pathComponents;

            StringJoiner pathJoiner = new StringJoiner("/", "/", "");
            foreach (string pathComponent in pathComponents)
            {
                pathJoiner.add(pathComponent);
            }
            unixPath = pathJoiner.toString();
        }

        /**
         * Resolves this path against another relative path.
         *
         * @param relativeUnixPath the relative path to resolve against
         * @return a new {@link AbsoluteUnixPath} representing the resolved path
         */
        public AbsoluteUnixPath resolve(RelativeUnixPath relativeUnixPath)
        {
            ImmutableArray<string>.Builder newPathComponents =
                ImmutableArray.CreateBuilder<string>();
            newPathComponents.AddRange(pathComponents);
            newPathComponents.AddRange(relativeUnixPath.getRelativePathComponents());
            return new AbsoluteUnixPath(newPathComponents.ToImmutable());
        }

        /**
         * Resolves this path against another relative path (by the name elements of {@code
         * relativePath}).
         *
         * @param relativePath the relative path to resolve against
         * @return a new {@link AbsoluteUnixPath} representing the resolved path
         */
        public AbsoluteUnixPath resolve(SystemPath relativePath)
        {
            Preconditions.checkArgument(
                relativePath.getRoot() == null, "Cannot resolve against absolute Path: " + relativePath);

            return AbsoluteUnixPath.fromPath(Paths.get(unixPath).resolve(relativePath));
        }

        /**
         * Resolves this path against another relative Unix path in string form.
         *
         * @param relativeUnixPath the relative path to resolve against
         * @return a new {@link AbsoluteUnixPath} representing the resolved path
         */
        public AbsoluteUnixPath resolve(string relativeUnixPath)
        {
            return resolve(RelativeUnixPath.get(relativeUnixPath));
        }

        /**
         * Returns the string form of the absolute Unix-style path.
         *
         * @return the string form
         */

        public override string ToString()
        {
            return unixPath;
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }
            if (!(other is AbsoluteUnixPath))
            {
                return false;
            }
            AbsoluteUnixPath otherAbsoluteUnixPath = (AbsoluteUnixPath)other;
            return unixPath.Equals(otherAbsoluteUnixPath.unixPath);
        }

        public override int GetHashCode()
        {
            return unixPath.GetHashCode();
        }
    }
}