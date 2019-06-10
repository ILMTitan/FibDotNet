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
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.filesystem {








/** Recursively applies a function to each file in a directory. */
public class DirectoryWalker {

  private readonly SystemPath rootDir;

  private Func<SystemPath, bool> pathFilter = path => true;

  /**
   * Initialize with a root directory to walk.
   *
   * @param rootDir the root directory.
   * @throws NotDirectoryException if the root directory is not a directory.
   */
  public DirectoryWalker(SystemPath rootDir) {
    if (!Files.isDirectory(rootDir)) {
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
  public DirectoryWalker filter(Func<SystemPath, bool> pathFilter) {
    this.pathFilter = this.pathFilter.and(pathFilter);
    return this;
  }

  /**
   * Filters away the {@code rootDir}.
   *
   * @return this
   */
  public DirectoryWalker filterRoot() {
    filter(path => !path.Equals(rootDir));
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
  public ImmutableArray<SystemPath> walk(PathConsumer pathConsumer) {
    ImmutableArray<SystemPath> files = walk();
    foreach (SystemPath path in files)
    {
      pathConsumer.accept(path);
    }
    return files;
  }

  /**
   * Walks {@link #rootDir}.
   *
   * @return the walked files.
   * @throws IOException if walking the files fails.
   */
  public ImmutableArray<SystemPath> walk() {
            IEnumerable<SystemPath> fileStream = Files.walk(rootDir);
            {
                return fileStream.filter(pathFilter).sorted().ToImmutableArray();
    }
  }
}
}
