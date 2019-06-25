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

using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.http;
using Jib.Net.Core.Registry;
using NUnit.Framework;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.registry
{
    /** Integration tests for {@link AuthenticationMethodRetriever}. */
    public class AuthenticationMethodRetrieverIntegrationTest
    {
        [Test]
        public async Task testGetRegistryAuthenticatorAsync()
        {
            RegistryClient registryClient =
                RegistryClient.factory(EventHandlers.NONE, "registry.hub.docker.com", "library/busybox")
                    .newRegistryClient();
            RegistryAuthenticator registryAuthenticator = await registryClient.getRegistryAuthenticatorAsync().ConfigureAwait(false);
            Assert.IsNotNull(registryAuthenticator);
            Authorization authorization = await registryAuthenticator.authenticatePullAsync(null).ConfigureAwait(false);

            RegistryClient authorizedRegistryClient =
                RegistryClient.factory(EventHandlers.NONE, "registry.hub.docker.com", "library/busybox")
                    .setAuthorization(authorization)
                    .newRegistryClient();
            await authorizedRegistryClient.pullManifestAsync("latest").ConfigureAwait(false);
        }
    }
}
