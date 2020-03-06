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

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Fib.Net.Core.Registry.Credentials.Json
{
    /**
     * Template for a Docker config file.
     *
     * <p>Example:
     *
     * <pre>{@code
     * {
     *   "auths": {
     *     "registry": {
     *       "auth": "username:password in base64"
     *     },
     *     "anotherregistry": {},
     *     ...
     *   },
     *   "credsStore": "credential helper name",
     *   "credHelpers": {
     *     "registry": "credential helper name",
     *     "anotherregistry": "another credential helper name",
     *     ...
     *   }
     * }
     * }</pre>
     *
     * If an {@code auth} is defined for a registry, that is a valid {@code Basic} authorization to use
     * for that registry.
     *
     * <p>If {@code credsStore} is defined, is a credential helper that stores authorizations for all
     * registries listed under {@code auths}.
     *
     * <p>Each entry in {@code credHelpers} is a mapping from a registry to a credential helper that
     * stores the authorization for that registry.
     *
     * @see <a
     *     href="https://www.projectatomic.io/blog/2016/03/docker-credentials-store/">https://www.projectatomic.io/blog/2016/03/docker-credentials-store/</a>
     */
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class DockerConfigTemplate
    {
        /** Template for an {@code auth} defined for a registry under {@code auths}. */
        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public class AuthTemplate
        {
            public string Auth { get; set; }
        }

        /** Maps from registry to its {@link AuthTemplate}. */
        public IDictionary<string, AuthTemplate> Auths { get; } = new Dictionary<string, AuthTemplate>();

        public string CredsStore { get; set; }

        /** Maps from registry to credential helper name. */
        public IDictionary<string, string> CredHelpers { get; } = new Dictionary<string, string>();
    }
}
