// Copyright 2017 Google LLC.
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

using Fib.Net.Core.Api;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Http;
using Fib.Net.Core.Registry;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Fib.Net.Core.Integration.Tests.Registry
{
    /** Integration tests for {@link RegistryAuthenticator}. */
    public class RegistryAuthenticatorIntegrationTest
    {
        [Test]
        public async Task TestAuthenticateAsync()
        {
            ImageReference dockerHubImageReference = ImageReference.Parse("library/busybox");
            RegistryAuthenticator registryAuthenticator =
                await RegistryClient.CreateFactory(
                        EventHandlers.NONE,
                        dockerHubImageReference.GetRegistry(),
                        dockerHubImageReference.GetRepository())
                    .NewRegistryClient()
                    .GetRegistryAuthenticatorAsync().ConfigureAwait(false);
            Assert.IsNotNull(registryAuthenticator);
            Authorization authorization = await registryAuthenticator.AuthenticatePullAsync(null).ConfigureAwait(false);

            // Checks that some token was received.
            Assert.IsTrue(0 < authorization.GetToken().Length);
        }
    }
}
