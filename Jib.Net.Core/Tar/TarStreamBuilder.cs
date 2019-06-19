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

using com.google.cloud.tools.jib.blob;
using ICSharpCode.SharpZipLib.Tar;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.tar
{
    /** Builds a tarball archive. */
    public class TarStreamBuilder
    {
        /**
         * Maps from {@link TarArchiveEntry} to a {@link Blob}. The order of the entries is the order they
         * belong in the tarball.
         */
        private readonly Dictionary<TarEntry, Blob> archiveMap = new Dictionary<TarEntry, Blob>();

        /**
         * Writes each entry in the filesystem to the tarball archive stream.
         *
         * @param out the stream to write to.
         * @throws IOException if building the tarball fails.
         */
        public async Task writeAsTarArchiveToAsync(Stream @out)
        {
            using (TarOutputStream tarArchiveOutputStream = new TarOutputStream(@out))
            {
                foreach (KeyValuePair<TarEntry, Blob> entry in archiveMap.entrySet())
                {
                    tarArchiveOutputStream.putArchiveEntry(entry.getKey());
                    await entry.getValue().writeToAsync(tarArchiveOutputStream);
                    tarArchiveOutputStream.closeArchiveEntry();
                }
            }
        }

        /**
         * Adds a {@link TarArchiveEntry} to the archive.
         *
         * @param entry the {@link TarArchiveEntry}
         */
        public void addTarArchiveEntry(TarEntry entry)
        {
            archiveMap.put(
                entry, entry.isFile() ? Blobs.from(entry.getFile().toPath()) : Blobs.from(_ => Task.CompletedTask, 0));
        }

        /**
         * Adds a blob to the archive. Note that this should be used with raw bytes and not file contents;
         * for adding files to the archive, use {@link #addTarArchiveEntry}.
         *
         * @param contents the bytes to add to the tarball
         * @param name the name of the entry (i.e. filename)
         */
        public void addByteEntry(byte[] contents, string name)
        {
            TarEntry entry = TarEntry.CreateTarEntry(name);
            entry.setSize(contents.Length);
            archiveMap.put(entry, Blobs.from(contents));
        }

        /**
         * Adds a blob to the archive. Note that this should be used with non-file {@link Blob}s; for
         * adding files to the archive, use {@link #addTarArchiveEntry}.
         *
         * @param blob the {@link Blob} to add to the tarball
         * @param size the size (in bytes) of {@code blob}
         * @param name the name of the entry (i.e. filename)
         */
        public void addBlobEntry(Blob blob, long size, string name)
        {
            TarEntry entry = TarEntry.CreateTarEntry(name);
            entry.setSize(size);
            archiveMap.put(entry, blob);
        }

        internal static TarEntry CreateEntryFromFile(FileSystemInfo info, string name)
        {
            var entry = TarEntry.CreateEntryFromFile(info.FullName);
            entry.Name = name;
            return entry;
        }
    }
}
