/*
 * Copyright 2017 Google LLC.
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
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.registry;
using com.google.cloud.tools.jib.tar;
using ICSharpCode.SharpZipLib.Tar;
using Jib.Net.Core;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Jib.Net.Core.Images
{
    /**
     * Builds a reproducible layer {@link Blob} from files. The reproducibility is implemented by strips
     * out all non-reproducible elements (modification time, group ID, user ID, user name, and group
     * name) from name-sorted tar archive entries.
     */
    public class ReproducibleLayerBuilder
    {
        /**
         * Holds a list of {@link TarArchiveEntry}s with unique extraction paths. The list also includes
         * all parent directories for each extraction path.
         */
        private class UniqueTarArchiveEntries
        {
            private readonly IList<TarEntry> entries = new List<TarEntry>();
            private readonly ISet<string> names = new HashSet<string>();

            /**
             * Adds a {@link TarArchiveEntry} if its extraction path does not exist yet. Also adds all of
             * the parent directories on the extraction path, if the parent does not exist. Parent will have
             * modified time to set to {@link LayerConfiguration#DEFAULT_MODIFIED_TIME}.
             *
             * @param tarArchiveEntry the {@link TarArchiveEntry}
             */
            public void Add(TarEntry tarArchiveEntry)
            {
                if (JavaExtensions.Contains(names, tarArchiveEntry.GetName()))
                {
                    return;
                }

                // Adds all directories along extraction paths to explicitly set permissions for those
                // directories.
                SystemPath namePath = Paths.Get(tarArchiveEntry.GetName());
                if (namePath.GetParent() != namePath.GetRoot())
                {
                    TarEntry dir = TarEntry.CreateTarEntry(JavaExtensions.ToString(namePath.GetParent()).Replace(Path.DirectorySeparatorChar, '/'));
                    dir.Name += "/";
                    dir.SetModTime(LayerConfiguration.DefaultModifiedTime.ToEpochMilli());
                    dir.TarHeader.Mode &= ~(int)PosixFilePermissions.All;
                    dir.TarHeader.Mode |= (int)(
                        PosixFilePermissions.OwnerAll
                        | PosixFilePermissions.GroupReadExecute
                        | PosixFilePermissions.OthersReadExecute);
                    dir.TarHeader.TypeFlag = TarHeader.LF_DIR;
                    Add(dir);
                }

                JavaExtensions.Add(entries, tarArchiveEntry);
                JavaExtensions.Add(names, tarArchiveEntry.GetName());
            }

            public List<TarEntry> GetSortedEntries()
            {
                List<TarEntry> sortedEntries = new List<TarEntry>(entries);
                JavaExtensions.Sort(sortedEntries, Comparator.Comparing((TarEntry e) => e.GetName()));
                return sortedEntries;
            }
        }

        private readonly ImmutableArray<LayerEntry> layerEntries;

        public ReproducibleLayerBuilder(ImmutableArray<LayerEntry> layerEntries)
        {
            this.layerEntries = layerEntries;
        }

        /**
         * Builds and returns the layer {@link Blob}.
         *
         * @return the new layer
         */
        public IBlob Build()
        {
            UniqueTarArchiveEntries uniqueTarArchiveEntries = new UniqueTarArchiveEntries();

            // Adds all the layer entries as tar entries.
            foreach (LayerEntry layerEntry in layerEntries)
            {
                // Adds the entries to uniqueTarArchiveEntries, which makes sure all entries are unique and
                // adds parent directories for each extraction path.
                TarEntry entry = TarEntry.CreateEntryFromFile(layerEntry.GetSourceFile());
                entry.Name = JavaExtensions.ToString(layerEntry.GetExtractionPath()).TrimStart('/');

                if (Directory.Exists(layerEntry.GetSourceFile()))
                {
                    entry.Name += '/';
                    entry.TarHeader.TypeFlag = TarHeader.LF_DIR;
                }

                // Sets the entry's permissions by masking out the permission bits from the entry's mode (the
                // lowest 9 bits) then using a bitwise OR to set them to the layerEntry's permissions.
                entry.SetMode((entry.GetMode() & ~PosixFilePermissions.All) | layerEntry.GetPermissions().GetPermissionBits());
                entry.SetModTime(layerEntry.GetLastModifiedTime().ToEpochMilli());

                uniqueTarArchiveEntries.Add(entry);
            }

            // Gets the entries sorted by extraction path.
            IList<TarEntry> sortedFilesystemEntries = uniqueTarArchiveEntries.GetSortedEntries();

            ISet<string> names = new HashSet<string>();

            // Adds all the files to a tar stream.
            TarStreamBuilder tarStreamBuilder = new TarStreamBuilder();
            foreach (TarEntry entry in sortedFilesystemEntries)
            {
                // Strips out all non-reproducible elements from tar archive entries.
                // Modified time is configured per entry
                entry.SetGroupId(0);
                entry.SetUserId(0);
                entry.SetUserName("");
                entry.SetGroupName("");

                Preconditions.CheckState(!JavaExtensions.Contains(names, entry.GetName()));
                JavaExtensions.Add(names, entry.GetName());

                tarStreamBuilder.AddTarArchiveEntry(entry);
            }

            return Blobs.From(tarStreamBuilder.WriteAsTarArchiveToAsync, -1);
        }
    }
}
