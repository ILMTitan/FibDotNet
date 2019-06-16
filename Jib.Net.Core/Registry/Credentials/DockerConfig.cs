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
using System;
using System.Collections.Generic;
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
        private static Optional<KeyValuePair<K, T>> findFirstInMapByKey<K, T>(IDictionary<K, T> map, IList<Func<K, bool>> keyMatches)
        {
            return keyMatches
                .stream()
                .map(keyMatch => findFirstInMapByKey(map, keyMatch))
                .filter(o => o.isPresent())
                .findFirst();
        }

        /** Returns the first entry matching the given key predicate. */
        private static Optional<KeyValuePair<K, T>> findFirstInMapByKey<K, T>(IDictionary<K, T> map, Func<K, bool> keyMatch)
        {
            return map.entrySet()
                .stream()
                .filter(entry => keyMatch(entry.getKey()))
                .findFirst();
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
        public string getAuthFor(string registry)
        {
            KeyValuePair<string, AuthTemplate>? authEntry =
                findFirstInMapByKey(dockerConfigTemplate.getAuths(), getRegistryMatchersFor(registry)).asNullable();
            return authEntry?.getValue().getAuth();
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
        public IDockerCredentialHelper getCredentialHelperFor(string registry)
        {
            IList<Func<string, bool>> registryMatchers = getRegistryMatchersFor(registry);

            KeyValuePair<string, AuthTemplate>? firstAuthMatch =
                findFirstInMapByKey(dockerConfigTemplate.getAuths(), registryMatchers).asNullable();
            if (firstAuthMatch != null && dockerConfigTemplate.getCredsStore() != null)
            {
                return new DockerCredentialHelper(
                    firstAuthMatch.Value.getKey(), dockerConfigTemplate.getCredsStore());
            }

            KeyValuePair<string, string>? firstCredHelperMatch =
                findFirstInMapByKey(dockerConfigTemplate.getCredHelpers(), registryMatchers).asNullable();
            if (firstCredHelperMatch != null)
            {
                return new DockerCredentialHelper(
                    firstCredHelperMatch.Value.getKey(), firstCredHelperMatch.Value.getValue());
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
        private IList<Func<string, bool>> getRegistryMatchersFor(string registry)
        {
            Func<string, bool> exactMatch = registry.equals;
            Func<string, bool> withHttps = ("https://" + registry).equals;
            Func<string, bool> withSuffix = name => name.startsWith(registry + "/");
            Func<string, bool> withHttpsAndSuffix = name => name.startsWith("https://" + registry + "/");
            return Arrays.asList(exactMatch, withHttps, withSuffix, withHttpsAndSuffix);
        }
    }
}
