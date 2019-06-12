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

using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace com.google.cloud.tools.jib.api
{
    /** Configures how to build a layer in the container image. Instantiate with {@link #builder}. */
    public sealed class LayerConfiguration
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
            public Builder setName(string name)
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
            public Builder addEntry(LayerEntry entry)
            {
                layerEntries.add(entry);
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
            public Builder addEntry(SystemPath sourceFile, AbsoluteUnixPath pathInContainer)
            {
                return addEntry(
                    sourceFile,
                    pathInContainer,
                    DEFAULT_FILE_PERMISSIONS_PROVIDER.apply(sourceFile, pathInContainer));
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
            public Builder addEntry(
                SystemPath sourceFile, AbsoluteUnixPath pathInContainer, FilePermissions permissions)
            {
                return addEntry(
                    sourceFile,
                    pathInContainer,
                    permissions,
                    DEFAULT_MODIFIED_TIME_PROVIDER.apply(sourceFile, pathInContainer));
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
            public Builder addEntry(
                SystemPath sourceFile,
                AbsoluteUnixPath pathInContainer,
                FilePermissions permissions,
                Instant lastModifiedTime)
            {
                return addEntry(new LayerEntry(sourceFile, pathInContainer, permissions, lastModifiedTime));
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
            public Builder addEntryRecursive(SystemPath sourceFile, AbsoluteUnixPath pathInContainer)
            {
                return addEntryRecursive(sourceFile, pathInContainer, DEFAULT_FILE_PERMISSIONS_PROVIDER);
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
            public Builder addEntryRecursive(
                SystemPath sourceFile,
                AbsoluteUnixPath pathInContainer,
                Func<SystemPath, AbsoluteUnixPath, FilePermissions> filePermissionProvider)
            {
                return addEntryRecursive(
                    sourceFile, pathInContainer, filePermissionProvider, DEFAULT_MODIFIED_TIME_PROVIDER);
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
            public Builder addEntryRecursive(
                SystemPath sourceFile,
                AbsoluteUnixPath pathInContainer,
                Func<SystemPath, AbsoluteUnixPath, FilePermissions> filePermissionProvider,
                Func<SystemPath, AbsoluteUnixPath, Instant> lastModifiedTimeProvider)
            {
                FilePermissions permissions = filePermissionProvider.apply(sourceFile, pathInContainer);
                Instant modifiedTime = lastModifiedTimeProvider.apply(sourceFile, pathInContainer);
                addEntry(sourceFile, pathInContainer, permissions, modifiedTime);
                if (!Files.isDirectory(sourceFile))
                {
                    return this;
                }
                IEnumerable<SystemPath> files = Files.list(sourceFile);
                {
                    foreach (SystemPath file in files.ToList())
                    {
                        addEntryRecursive(
                            file,
                            pathInContainer.resolve(file.getFileName()),
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
            public LayerConfiguration build()
            {
                return new LayerConfiguration(name, layerEntries.build());
            }
        }

        /** Provider that returns default file permissions (644 for files, 755 for directories). */
        public static readonly Func<SystemPath, AbsoluteUnixPath, FilePermissions>
            DEFAULT_FILE_PERMISSIONS_PROVIDER =
                (sourcePath, destinationPath) =>
                    Files.isDirectory(sourcePath)
                        ? FilePermissions.DEFAULT_FOLDER_PERMISSIONS
                        : FilePermissions.DEFAULT_FILE_PERMISSIONS;

        /** Default file modification time (EPOCH + 1 second). */
        public static readonly Instant DEFAULT_MODIFIED_TIME = Instant.FromUnixTimeSeconds(1);

        /** Provider that returns default file modification time (EPOCH + 1 second). */
        public static readonly Func<SystemPath, AbsoluteUnixPath, Instant> DEFAULT_MODIFIED_TIME_PROVIDER =
            (sourcePath, destinationPath) => DEFAULT_MODIFIED_TIME;

        /**
         * Gets a new {@link Builder} for {@link LayerConfiguration}.
         *
         * @return a new {@link Builder}
         */
        public static Builder builder()
        {
            return new Builder();
        }

        private readonly ImmutableArray<LayerEntry> layerEntries;
        private readonly string name;

        /**
         * Use {@link #builder} to instantiate.
         *
         * @param name an optional name for the layer
         * @param layerEntries the list of {@link LayerEntry}s
         */
        private LayerConfiguration(string name, ImmutableArray<LayerEntry> layerEntries)
        {
            this.name = name;
            this.layerEntries = layerEntries;
        }

        /**
         * Gets the name.
         *
         * @return the name
         */
        public string getName()
        {
            return name;
        }

        /**
         * Gets the list of layer entries.
         *
         * @return the list of layer entries
         */
        public ImmutableArray<LayerEntry> getLayerEntries()
        {
            return layerEntries;
        }
    }
}
