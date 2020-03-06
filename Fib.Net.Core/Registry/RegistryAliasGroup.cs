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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Fib.Net.Core.Registry
{
    /** Provides known aliases and alternative hosts for a given registry. */
    public static class RegistryAliasGroup
    {
        private static readonly ImmutableArray<ImmutableHashSet<string>> REGISTRY_ALIAS_GROUPS =
            ImmutableArray.Create(
                // Docker Hub alias group (https://github.com/moby/moby/pull/28100)
                ImmutableHashSet.Create("registry.hub.docker.com", "index.docker.io", "registry-1.docker.io", "docker.io"));

        /** Some registry names are symbolic. */
        private static readonly ImmutableDictionary<string, string> REGISTRY_HOST_MAP =
            // https://github.com/docker/hub-feedback/issues/1767
            ImmutableDictionary.CreateRange(new Dictionary<string, string>
            {
                ["docker.io"] = "registry-1.docker.io"
            });

        /**
         * Returns the list of registry aliases for the given {@code registry}, including {@code registry}
         * as the first element.
         *
         * @param registry the registry for which the alias group is requested
         * @return non-empty list of registries where {@code registry} is the first element
         */
        public static IList<string> GetAliasesGroup(string registry)
        {
            foreach (ImmutableHashSet<string> aliasGroup in REGISTRY_ALIAS_GROUPS)
            {
                if (aliasGroup.Contains(registry, StringComparer.Ordinal))
                {
                    // Found a group. Move the requested "registry" to the front before returning it.
                    IEnumerable<string> self = new[] { registry };
                    IEnumerable<string> withoutSelf = aliasGroup.Where(alias => registry != alias);
                    return self.Concat(withoutSelf).ToList();
                }
            }

            return new List<string> { registry };
        }

        /**
         * Returns the server host name to use for the given registry.
         *
         * @param registry the name of the registry
         * @return the registry host
         */
        public static string GetHost(string registry)
        {
            if (REGISTRY_HOST_MAP.TryGetValue(registry, out var host)) {
                return host;
            } else {
                return registry;
            }
        }
    }
}
