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

namespace com.google.cloud.tools.jib.registry {








/** Provides known aliases and alternative hosts for a given registry. */
public class RegistryAliasGroup {

  private static readonly ImmutableList<ImmutableSet<string>> REGISTRY_ALIAS_GROUPS =
      ImmutableList.of(
          // Docker Hub alias group (https://github.com/moby/moby/pull/28100)
          ImmutableSet.of(
              "registry.hub.docker.com", "index.docker.io", "registry-1.docker.io", "docker.io"));

  /** Some registry names are symbolic. */
  private static readonly ImmutableMap<string, string> REGISTRY_HOST_MAP =
      ImmutableMap.of(
          // https://github.com/docker/hub-feedback/issues/1767
          "docker.io", "registry-1.docker.io");

  /**
   * Returns the list of registry aliases for the given {@code registry}, including {@code registry}
   * as the first element.
   *
   * @param registry the registry for which the alias group is requested
   * @return non-empty list of registries where {@code registry} is the first element
   */
  public static List<string> getAliasesGroup(string registry) {
    foreach (ImmutableSet<string> aliasGroup in REGISTRY_ALIAS_GROUPS)
    {
      if (aliasGroup.contains(registry)) {
        // Found a group. Move the requested "registry" to the front before returning it.
        Stream<string> self = Stream.of(registry);
        Stream<string> withoutSelf = aliasGroup.stream().filter(alias => !registry.equals(alias));
        return Stream.concat(self, withoutSelf).collect(Collectors.toList());
      }
    }

    return Collections.singletonList(registry);
  }

  /**
   * Returns the server host name to use for the given registry.
   *
   * @param registry the name of the registry
   * @return the registry host
   */
  public static string getHost(string registry) {
    return REGISTRY_HOST_MAP.getOrDefault(registry, registry);
  }
}
}
