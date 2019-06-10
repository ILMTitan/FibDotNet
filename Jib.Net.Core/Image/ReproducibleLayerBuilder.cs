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
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace com.google.cloud.tools.jib.image {

















/**
 * Builds a reproducible layer {@link Blob} from files. The reproducibility is implemented by strips
 * out all non-reproducible elements (modification time, group ID, user ID, user name, and group
 * name) from name-sorted tar archive entries.
 */
public class ReproducibleLayerBuilder {

  /**
   * Holds a list of {@link TarArchiveEntry}s with unique extraction paths. The list also includes
   * all parent directories for each extraction path.
   */
  private class UniqueTarArchiveEntries {

    /**
     * Uses the current directory to act as the file input to TarArchiveEntry (since all directories
     * are treated the same in {@link TarArchiveEntry#TarArchiveEntry(File, string)}, except for
     * modification time, which is wiped away in {@link #build}).
     */
    private static readonly FileInfo DIRECTORY_FILE = Paths.get(".").toFile();

    private readonly IList<TarEntry> entries = new List<TarEntry>();
    private readonly ISet<string> names = new HashSet<string>();

    /**
     * Adds a {@link TarArchiveEntry} if its extraction path does not exist yet. Also adds all of
     * the parent directories on the extraction path, if the parent does not exist. Parent will have
     * modified time to set to {@link LayerConfiguration#DEFAULT_MODIFIED_TIME}.
     *
     * @param tarArchiveEntry the {@link TarArchiveEntry}
     */
    public void add(TarEntry tarArchiveEntry) {
      if (names.contains(tarArchiveEntry.getName())) {
        return;
      }

      // Adds all directories along extraction paths to explicitly set permissions for those
      // directories.
      SystemPath namePath = Paths.get(tarArchiveEntry.getName());
      if (namePath.getParent() != namePath.getRoot()) {
                    TarEntry dir = TarEntry.CreateTarEntry(namePath.getParent().toString());
        dir.setModTime(LayerConfiguration.DEFAULT_MODIFIED_TIME.toEpochMilli());
        add(dir);
      }

      entries.add(tarArchiveEntry);
      names.add(tarArchiveEntry.getName());
    }

    public List<TarEntry> getSortedEntries() {
      List<TarEntry> sortedEntries = new List<TarEntry>(entries);
                sortedEntries.sort(Comparator.comparing((TarEntry e) => e.getName()));
      return sortedEntries;
    }
  }

  private readonly ImmutableArray<LayerEntry> layerEntries;

  public ReproducibleLayerBuilder(ImmutableArray<LayerEntry> layerEntries) {
    this.layerEntries = layerEntries;
  }

  /**
   * Builds and returns the layer {@link Blob}.
   *
   * @return the new layer
   */
  public Blob build() {
    UniqueTarArchiveEntries uniqueTarArchiveEntries = new UniqueTarArchiveEntries();

    // Adds all the layer entries as tar entries.
    foreach (LayerEntry layerEntry in layerEntries)
    {
                // Adds the entries to uniqueTarArchiveEntries, which makes sure all entries are unique and
                // adds parent directories for each extraction path.
                TarEntry entry =
                              TarEntry.CreateEntryFromFile(layerEntry.getSourceFile().toString());
                entry.Name = layerEntry.getExtractionPath().toString();

      // Sets the entry's permissions by masking out the permission bits from the entry's mode (the
      // lowest 9 bits) then using a bitwise OR to set them to the layerEntry's permissions.
      entry.setMode((entry.getMode() & ~0777) | layerEntry.getPermissions().getPermissionBits());
      entry.setModTime(layerEntry.getLastModifiedTime().toEpochMilli());

      uniqueTarArchiveEntries.add(entry);
    }

    // Gets the entries sorted by extraction path.
    IList<TarEntry> sortedFilesystemEntries = uniqueTarArchiveEntries.getSortedEntries();

    ISet<string> names = new HashSet<string>();

    // Adds all the files to a tar stream.
    TarStreamBuilder tarStreamBuilder = new TarStreamBuilder();
    foreach (TarEntry entry in sortedFilesystemEntries)
    {
      // Strips out all non-reproducible elements from tar archive entries.
      // Modified time is configured per entry
      entry.setGroupId(0);
      entry.setUserId(0);
      entry.setUserName("");
      entry.setGroupName("");

      Verify.verify(!names.contains(entry.getName()));
      names.add(entry.getName());

      tarStreamBuilder.addTarArchiveEntry(entry);
    }

    return Blobs.from(
        tarStreamBuilder.writeAsTarArchiveTo);
  }
}
}
