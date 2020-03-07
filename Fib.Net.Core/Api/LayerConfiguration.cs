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
using NodaTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Fib.Net.Core.Api
{
    /** Configures how to build a layer in the container image. Instantiate with {@link #builder}. */
    public sealed class LayerConfiguration : ILayerConfiguration
    {
        /** Builds a {@link LayerConfiguration}. */
        public class Builder
        {
            private readonly ImmutableArray<LayerEntry>.Builder layerEntries = ImmutableArray.CreateBuilder<LayerEntry>();
            private string name = "";

            public Builder() { }

            /**
             * Sets a name for this layer. This name does not affect the contents of the layer.
             *
             * @param name the name
             * @return this
             */
            public Builder SetName(string name)
            {
                this.name = name;
                return this;
            }

            /**
             * Adds an entry to the layer.
             *
             * @param entry the layer entry to add
             * @return this
             */
            public Builder AddEntry(LayerEntry entry)
            {
                layerEntries.Add(entry);
                return this;
            }

            /**
             * Adds an entry to the layer. Only adds the single source file to the exact path in the
             * container file system.
             *
             * <p>For example, {@code addEntry(Paths.get("myfile"),
             * AbsoluteUnixPath.get("/path/in/container"))} adds a file {@code myfile} to the container file
             * system at {@code /path/in/container}.
             *
             * <p>For example, {@code addEntry(Paths.get("mydirectory"),
             * AbsoluteUnixPath.get("/path/in/container"))} adds a directory {@code mydirectory/} to the
             * container file system at {@code /path/in/container/}. This does <b>not</b> add the contents
             * of {@code mydirectory}.
             *
             * @param sourceFile the source file to add to the layer
             * @param pathInContainer the path in the container file system corresponding to the {@code
             *     sourceFile}
             * @return this
             */
            public Builder AddEntry(string sourceFile, AbsoluteUnixPath pathInContainer)
            {
                return AddEntry(new SystemPath(sourceFile), pathInContainer);
            }

            /**
             * Adds an entry to the layer. Only adds the single source file to the exact path in the
             * container file system.
             *
             * <p>For example, {@code addEntry(Paths.get("myfile"),
             * AbsoluteUnixPath.get("/path/in/container"))} adds a file {@code myfile} to the container file
             * system at {@code /path/in/container}.
             *
             * <p>For example, {@code addEntry(Paths.get("mydirectory"),
             * AbsoluteUnixPath.get("/path/in/container"))} adds a directory {@code mydirectory/} to the
             * container file system at {@code /path/in/container/}. This does <b>not</b> add the contents
             * of {@code mydirectory}.
             *
             * @param sourceFile the source file to add to the layer
             * @param pathInContainer the path in the container file system corresponding to the {@code
             *     sourceFile}
             * @return this
             */
            public Builder AddEntry(SystemPath sourceFile, AbsoluteUnixPath pathInContainer)
            {
                return AddEntry(
                    sourceFile,
                    pathInContainer,
                    DefaultFilePermissionsProvider(sourceFile, pathInContainer));
            }

            /**
             * Adds an entry to the layer with the given permissions. Only adds the single source file to
             * the exact path in the container file system. See {@link Builder#addEntry(Path,
             * AbsoluteUnixPath)} for more information.
             *
             * @param sourceFile the source file to add to the layer
             * @param pathInContainer the path in the container file system corresponding to the {@code
             *     sourceFile}
             * @param permissions the file permissions on the container
             * @return this
             * @see Builder#addEntry(Path, AbsoluteUnixPath)
             * @see FilePermissions#DEFAULT_FILE_PERMISSIONS
             * @see FilePermissions#DEFAULT_FOLDER_PERMISSIONS
             */
            public Builder AddEntry(
                SystemPath sourceFile, AbsoluteUnixPath pathInContainer, FilePermissions permissions)
            {
                return AddEntry(
                    sourceFile,
                    pathInContainer,
                    permissions,
                    DefaultModifiedTimeProvider(sourceFile, pathInContainer));
            }

            /**
             * Adds an entry to the layer with the given permissions. Only adds the single source file to
             * the exact path in the container file system. See {@link Builder#addEntry(Path,
             * AbsoluteUnixPath)} for more information.
             *
             * @param sourceFile the source file to add to the layer
             * @param pathInContainer the path in the container file system corresponding to the {@code
             *     sourceFile}
             * @param permissions the file permissions on the container
             * @param lastModifiedTime the file modification timestamp
             * @return this
             * @see Builder#addEntry(Path, AbsoluteUnixPath)
             * @see FilePermissions#DEFAULT_FILE_PERMISSIONS
             * @see FilePermissions#DEFAULT_FOLDER_PERMISSIONS
             */
            public Builder AddEntry(
                SystemPath sourceFile,
                AbsoluteUnixPath pathInContainer,
                FilePermissions permissions,
                Instant lastModifiedTime)
            {
                return AddEntry(new LayerEntry(sourceFile, pathInContainer, permissions, lastModifiedTime));
            }

            /**
             * Adds an entry to the layer. If the source file is a directory, the directory and its contents
             * will be added recursively.
             *
             * <p>For example, {@code addEntryRecursive(Paths.get("mydirectory",
             * AbsoluteUnixPath.get("/path/in/container"))} adds {@code mydirectory} to the container file
             * system at {@code /path/in/container} such that {@code mydirectory/subfile} is found at {@code
             * /path/in/container/subfile}.
             *
             * @param sourceFile the source file to add to the layer recursively
             * @param pathInContainer the path in the container file system corresponding to the {@code
             *     sourceFile}
             * @return this
             * @throws IOException if an exception occurred when recursively listing the directory
             */
            public Builder AddEntryRecursive(SystemPath sourceFile, AbsoluteUnixPath pathInContainer)
            {
                return AddEntryRecursive(sourceFile, pathInContainer, DefaultFilePermissionsProvider);
            }

            /**
             * Adds an entry to the layer. If the source file is a directory, the directory and its contents
             * will be added recursively.
             *
             * @param sourceFile the source file to add to the layer recursively
             * @param pathInContainer the path in the container file system corresponding to the {@code
             *     sourceFile}
             * @param filePermissionProvider a provider that takes a source path and destination path on the
             *     container and returns the file permissions that should be set for that path
             * @return this
             * @throws IOException if an exception occurred when recursively listing the directory
             */
            public Builder AddEntryRecursive(
                SystemPath sourceFile,
                AbsoluteUnixPath pathInContainer,
                Func<SystemPath, AbsoluteUnixPath, FilePermissions> filePermissionProvider)
            {
                return AddEntryRecursive(
                    sourceFile, pathInContainer, filePermissionProvider, DefaultModifiedTimeProvider);
            }

            /**
             * Adds an entry to the layer. If the source file is a directory, the directory and its contents
             * will be added recursively.
             *
             * @param sourceFile the source file to add to the layer recursively
             * @param pathInContainer the path in the container file system corresponding to the {@code
             *     sourceFile}
             * @param filePermissionProvider a provider that takes a source path and destination path on the
             *     container and returns the file permissions that should be set for that path
             * @param lastModifiedTimeProvider a provider that takes a source path and destination path on
             *     the container and returns the file modification time that should be set for that path
             * @return this
             * @throws IOException if an exception occurred when recursively listing the directory
             */
            public Builder AddEntryRecursive(
                SystemPath sourceFile,
                AbsoluteUnixPath pathInContainer,
                Func<SystemPath, AbsoluteUnixPath, FilePermissions> filePermissionProvider,
                Func<SystemPath, AbsoluteUnixPath, Instant> lastModifiedTimeProvider)
            {
                FilePermissions permissions = filePermissionProvider?.Invoke(sourceFile, pathInContainer);
                Instant modifiedTime = lastModifiedTimeProvider(sourceFile, pathInContainer);
                AddEntry(sourceFile, pathInContainer, permissions, modifiedTime);
                if (!Files.IsDirectory(sourceFile))
                {
                    return this;
                }
                IEnumerable<SystemPath> files = Files.List(sourceFile);
                {
                    foreach (SystemPath file in files.ToList())
                    {
                        AddEntryRecursive(
                            file,
                            pathInContainer.Resolve(file.GetFileName()),
                            filePermissionProvider,
                            lastModifiedTimeProvider);
                    }
                }
                return this;
            }

            /**
             * Returns the built {@link LayerConfiguration}.
             *
             * @return the built {@link LayerConfiguration}
             */
            public ILayerConfiguration Build()
            {
                return new LayerConfiguration(name, layerEntries.ToImmutable());
            }
        }

        /** Provider that returns default file permissions (644 for files, 755 for directories). */
        public static readonly Func<SystemPath, AbsoluteUnixPath, FilePermissions>
            DefaultFilePermissionsProvider =
                (sourcePath, _) =>
                    Files.IsDirectory(sourcePath)
                        ? FilePermissions.DefaultFolderPermissions
                        : FilePermissions.DefaultFilePermissions;

        /** Default file modification time (EPOCH + 1 second). */
        public static readonly Instant DefaultModifiedTime = Instant.FromUnixTimeSeconds(1);

        /** Provider that returns default file modification time (EPOCH + 1 second). */
        public static readonly Func<SystemPath, AbsoluteUnixPath, Instant> DefaultModifiedTimeProvider =
            (_, __) => DefaultModifiedTime;

        /**
         * Gets a new {@link Builder} for {@link LayerConfiguration}.
         *
         * @return a new {@link Builder}
         */
        public static Builder CreateBuilder()
        {
            return new Builder();
        }

        /**
         * Gets the name.
         *
         * @return the name
         */
        public string Name { get; }

        /**
         * Gets the list of layer entries.
         *
         * @return the list of layer entries
         */
        public ImmutableArray<LayerEntry> LayerEntries { get; }

        /**
         * Use {@link #builder} to instantiate.
         *
         * @param name an optional name for the layer
         * @param layerEntries the list of {@link LayerEntry}s
         */
        public LayerConfiguration(string name, ImmutableArray<LayerEntry> layerEntries)
        {
            Name = name;
            LayerEntries = layerEntries;
        }
    }
}
