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

using Jib.Net.Core.Configuration;
using Jib.Net.Core.Http;
using Jib.Net.Core.Registry;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Net.Http.Headers;

namespace Jib.Net.Core.Unit.Tests.Registry
{
    /**
     * Tests for {@link RegistryClient}. More comprehensive tests can be found in the integration tests.
     */
    public class RegistryClientTest
    {
        private readonly IEventHandlers eventHandlers = Mock.Of<IEventHandlers>();
        private readonly Authorization mockAuthorization = Authorization.FromBasicCredentials("username", "password");

        private RegistryClient.Factory testRegistryClientFactory;

        [SetUp]
        public void SetUp()
        {
            testRegistryClientFactory =
                RegistryClient.CreateFactory(eventHandlers, "some.server.url", "some image name");
        }

        [Test]
        public void TestGetUserAgent_null()
        {
            var defaultUserAgent = new ProductHeaderValue("jib", "0.0.1-alpha.1");
            Assert.AreEqual(defaultUserAgent,
                testRegistryClientFactory
                    .SetAuthorization(mockAuthorization)
                    .NewRegistryClient()
                    .GetUserAgent().Single().Product);

            Assert.AreEqual(defaultUserAgent,
                testRegistryClientFactory
                    .SetAuthorization(mockAuthorization)
                    .AddUserAgentValue(null)
                    .NewRegistryClient()
                    .GetUserAgent().Single().Product);
        }

        [Test]
        public void TestGetUserAgent()
        {
            var defaultUserAgent = new ProductHeaderValue("jib", "0.0.1-alpha.1");
            RegistryClient registryClient =
                testRegistryClientFactory
                    .SetAllowInsecureRegistries(true)
                    .AddUserAgentValue(new ProductInfoHeaderValue("someUserAgent", "someAgentVersion"))
                    .NewRegistryClient();

            Assert.AreEqual(defaultUserAgent, registryClient.GetUserAgent().First().Product);
            Assert.AreEqual("someUserAgent", registryClient.GetUserAgent().Skip(1).First().Product.Name);
            Assert.AreEqual("someAgentVersion", registryClient.GetUserAgent().Skip(1).First().Product.Version);
        }

        [Test]
        public void TestGetApiRouteBase()
        {
            Assert.AreEqual(
                "some.server.url/v2/",
                testRegistryClientFactory
                    .SetAllowInsecureRegistries(true)
                    .NewRegistryClient()
                    .GetApiRouteBase());
        }
    }
}
