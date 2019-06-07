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

namespace com.google.cloud.tools.jib.filesystem {



/** Parses Unix-style paths. */
public class UnixPathParser {

  /**
   * Parses a Unix-style path into a list of path components.
   *
   * @param unixPath the Unix-style path
   * @return a list of path components
   */
  public static ImmutableList<string> parse(string unixPath) {
    ImmutableList.Builder<string> pathComponents = ImmutableList.builder();
    foreach (string pathComponent in Splitter.on('/').split(unixPath))
    {
      if (pathComponent.isEmpty()) {
        // Skips empty components.
        continue;
      }
      pathComponents.add(pathComponent);
    }
    return pathComponents.build();
  }

  private UnixPathParser() {}
}
}
