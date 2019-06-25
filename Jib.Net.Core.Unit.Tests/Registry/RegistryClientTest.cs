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

using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.http;
using Jib.Net.Core.Global;
using Jib.Net.Core.Registry;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Net.Http.Headers;

namespace com.google.cloud.tools.jib.registry
{
    /**
     * Tests for {@link RegistryClient}. More comprehensive tests can be found in the integration tests.
     */
    public class RegistryClientTest
    {
        private readonly IEventHandlers eventHandlers = Mock.Of<IEventHandlers>();
        private readonly Authorization mockAuthorization = Authorization.fromBasicCredentials("username", "password");

        private RegistryClient.Factory testRegistryClientFactory;

        [SetUp]
        public void setUp()
        {
            testRegistryClientFactory =
                RegistryClient.factory(eventHandlers, "some.server.url", "some image name");
        }

        [Test]
        public void testGetUserAgent_null()
        {
            var defaultUserAgent = new ProductHeaderValue("jib", "0.0.1-alpha.1");
            Assert.AreEqual(defaultUserAgent,
                testRegistryClientFactory
                    .setAuthorization(mockAuthorization)
                    .newRegistryClient()
                    .getUserAgent().Single().Product);

            Assert.AreEqual(defaultUserAgent,
                testRegistryClientFactory
                    .setAuthorization(mockAuthorization)
                    .addUserAgentValue(null)
                    .newRegistryClient()
                    .getUserAgent().Single().Product);
        }

        [Test]
        public void testGetUserAgent()
        {
            var defaultUserAgent = new ProductHeaderValue("jib", "0.0.1-alpha.1");
            RegistryClient registryClient =
                testRegistryClientFactory
                    .setAllowInsecureRegistries(true)
                    .addUserAgentValue(new ProductInfoHeaderValue("someUserAgent", "someAgentVersion"))
                    .newRegistryClient();

            Assert.AreEqual(defaultUserAgent, registryClient.getUserAgent().First().Product);
            Assert.AreEqual("someUserAgent", registryClient.getUserAgent().Skip(1).First().Product.Name);
            Assert.AreEqual("someAgentVersion", registryClient.getUserAgent().Skip(1).First().Product.Version);
        }

        [Test]
        public void testGetApiRouteBase()
        {
            Assert.AreEqual(
                "some.server.url/v2/",
                testRegistryClientFactory
                    .setAllowInsecureRegistries(true)
                    .newRegistryClient()
                    .getApiRouteBase());
        }
    }
}
