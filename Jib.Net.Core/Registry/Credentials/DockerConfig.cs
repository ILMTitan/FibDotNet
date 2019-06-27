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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.registry.credentials.json;
using Jib.Net.Core.Global;
using Jib.Net.Core.Registry.Credentials;
using System;
using System.Collections.Generic;
using System.Linq;
using static com.google.cloud.tools.jib.registry.credentials.json.DockerConfigTemplate;

namespace com.google.cloud.tools.jib.registry.credentials
{
    /** Handles getting useful information from a {@link DockerConfigTemplate}. */
    public class DockerConfig : IDockerConfig
    {
        /**
         * Returns the first entry matching the given key predicates (short-circuiting in the order of
         * predicates).
         */
        private static Maybe<KeyValuePair<K, T>> FindFirstInMapByKey<K, T>(IDictionary<K, T> map, IList<Func<K, bool>> keyMatches)
        {
            return keyMatches
                .Select(keyMatch => FindFirstInMapByKey(map, keyMatch))
                .Where(o => o.IsPresent())
                .FindFirst();
        }

        /** Returns the first entry matching the given key predicate. */
        private static Maybe<KeyValuePair<K, T>> FindFirstInMapByKey<K, T>(IDictionary<K, T> map, Func<K, bool> keyMatch)
        {
            return map
                .Where(entry => keyMatch(entry.GetKey()))
                .FindFirst();
        }

        private readonly DockerConfigTemplate dockerConfigTemplate;

        public DockerConfig(DockerConfigTemplate dockerConfigTemplate)
        {
            this.dockerConfigTemplate = dockerConfigTemplate;
        }

        /**
         * Returns the base64-encoded {@code Basic} authorization for {@code registry}, or {@code null} if
         * none exists. The order of lookup preference:
         *
         * <ol>
         *   <li>Exact registry name
         *   <li>https:// + registry name
         *   <li>registry name + / + arbitrary suffix
         *   <li>https:// + registry name + / arbitrary suffix
         * </ol>
         *
         * @param registry the registry to get the authorization for
         * @return the base64-encoded {@code Basic} authorization for {@code registry}, or {@code null} if
         *     none exists
         */
        public string GetAuthFor(string registry)
        {
            registry = registry ?? throw new ArgumentNullException(nameof(registry));
            KeyValuePair<string, AuthTemplate>? authEntry =
                FindFirstInMapByKey(dockerConfigTemplate.Auths, GetRegistryMatchersFor(registry)).AsNullable();
            return authEntry?.GetValue().Auth;
        }

        /**
         * Determines a {@link DockerCredentialHelper} to use for {@code registry}.
         *
         * <p>If there exists a matching registry entry (or its aliases) in {@code auths} for {@code
         * registry}, the credential helper is {@code credStore}; otherwise, if there exists a matching
         * registry entry (or its aliases) in {@code credHelpers}, the corresponding credential helper
         * suffix is used.
         *
         * <p>See {@link #getRegistryMatchersFor} for the alias lookup order.
         *
         * @param registry the registry to get the credential helpers for
         * @return the {@link DockerCredentialHelper} or {@code null} if none is found for the given
         *     registry
         */
        public IDockerCredentialHelper GetCredentialHelperFor(string registry)
        {
            registry = registry ?? throw new ArgumentNullException(nameof(registry));
            IList<Func<string, bool>> registryMatchers = GetRegistryMatchersFor(registry);

            KeyValuePair<string, AuthTemplate>? firstAuthMatch =
                FindFirstInMapByKey(dockerConfigTemplate.Auths, registryMatchers).AsNullable();
            if (firstAuthMatch != null && dockerConfigTemplate.CredsStore!= null)
            {
                return new DockerCredentialHelper(
                    firstAuthMatch.Value.GetKey(), dockerConfigTemplate.CredsStore);
            }

            KeyValuePair<string, string>? firstCredHelperMatch =
                FindFirstInMapByKey(dockerConfigTemplate.CredHelpers, registryMatchers).AsNullable();
            if (firstCredHelperMatch != null)
            {
                return new DockerCredentialHelper(
                    firstCredHelperMatch.Value.GetKey(), firstCredHelperMatch.Value.GetValue());
            }

            return null;
        }

        /**
         * Registry alias matches in the following order:
         *
         * <ol>
         *   <li>Exact registry name
         *   <li>https:// + registry name
         *   <li>registry name + / + arbitrary suffix
         *   <li>https:// + registry name + / + arbitrary suffix
         * </ol>
         *
         * @param registry the registry to get matchers for
         * @return the list of predicates to match possible aliases
         */
        private IList<Func<string, bool>> GetRegistryMatchersFor(string registry)
        {
            Func<string, bool> exactMatch = registry.Equals;
            Func<string, bool> withHttps = ("https://" + registry).Equals;
            bool withSuffix(string name) => JavaExtensions.StartsWith(name, registry + "/");
            bool withHttpsAndSuffix(string name) => JavaExtensions.StartsWith(name, "https://" + registry + "/");
            return new []{exactMatch, withHttps, withSuffix, withHttpsAndSuffix};
        }
    }
}
