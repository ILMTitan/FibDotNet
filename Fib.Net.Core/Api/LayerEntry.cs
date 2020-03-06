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

using Fib.Net.Core.FileSystem;
using Newtonsoft.Json;
using NodaTime;

namespace Fib.Net.Core.Api
{
    /**
     * Represents an entry in the layer. A layer consists of many entries that can be converted into tar
     * archive entries.
     */
    public class LayerEntry
    {
        /**
         * Gets the source file. The source file may be relative or absolute, so the caller should use
         * {@code getSourceFile().toAbsolutePath().toString()} for the serialized form since the
         * serialization could change independently of the path representation.
         *
         * @return the source file
         */
        public SystemPath SourceFile { get; }

        /**
         * Gets the extraction path.
         *
         * @return the extraction path
         */
        public AbsoluteUnixPath ExtractionPath { get; }

        /**
         * Gets the file permissions on the container.
         *
         * @return the file permissions on the container
         */
        public FilePermissions Permissions { get; }

        /**
         * Returns the modification time of the file in the entry.
         *
         * @return the modification time
         */
        public Instant LastModifiedTime { get; }

        /**
         * Instantiates with a source file and the path to place the source file in the container file
         * system.
         *
         * <p>For example, {@code new LayerEntry(Paths.get("typeof(HelloWorld)"),
         * AbsoluteUnixPath.get("/app/classes/typeof(HelloWorld)"))} adds a file {@code typeof(HelloWorld)} to
         * the container file system at {@code /app/classes/typeof(HelloWorld)}.
         *
         * <p>For example, {@code new LayerEntry(Paths.get("com"),
         * AbsoluteUnixPath.get("/app/classes/com"))} adds a directory to the container file system at
         * {@code /app/classes/com}. This does <b>not</b> add the contents of {@code com/}.
         *
         * <p>Note that:
         *
         * <ul>
         *   <li>Entry source files can be either files or directories.
         *   <li>Adding a directory does not include the contents of the directory. Each file under a
         *       directory must be added as a separate {@link LayerEntry}.
         * </ul>
         *
         * @param sourceFile the source file to add to the layer
         * @param extractionPath the path in the container file system corresponding to the {@code
         *     sourceFile}
         * @param permissions the file permissions on the container
         * @param lastModifiedTime the file modification time, default to 1 second since the epoch
         *     (https://github.com/GoogleContainerTools/jib/issues/1079)
         */
        [JsonConstructor]
        public LayerEntry(
            SystemPath sourceFile,
            AbsoluteUnixPath extractionPath,
            FilePermissions permissions = null,
            Instant? lastModifiedTime = null)
        {
            SourceFile = sourceFile;
            ExtractionPath = extractionPath;
            Permissions = permissions ?? LayerConfiguration.DefaultFilePermissionsProvider(SourceFile, ExtractionPath);
            LastModifiedTime =
                lastModifiedTime ?? LayerConfiguration.DefaultModifiedTimeProvider(SourceFile, ExtractionPath);
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }
            if (!(other is LayerEntry otherLayerEntry))
            {
                return false;
            }
            return Equals(SourceFile, otherLayerEntry.SourceFile)
                && Equals(ExtractionPath, otherLayerEntry.ExtractionPath)
                && Equals(Permissions, otherLayerEntry.Permissions)
                && Equals(LastModifiedTime, otherLayerEntry.LastModifiedTime);
        }

        public override int GetHashCode()
        {
            return Objects.Hash(SourceFile, ExtractionPath, Permissions, LastModifiedTime);
        }

        public override string ToString()
        {
            return $"{SourceFile} => {ExtractionPath}";
        }
    }
}
