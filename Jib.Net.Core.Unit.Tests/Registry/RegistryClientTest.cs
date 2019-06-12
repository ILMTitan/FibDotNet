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
using Moq;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.registry
{
    /**
     * Tests for {@link RegistryClient}. More comprehensive tests can be found in the integration tests.
     */
    [RunWith(typeof(MockitoJUnitRunner))]
    public class RegistryClientTest
    {
        private EventHandlers eventHandlers = Mock.Of<EventHandlers>();
        private Authorization mockAuthorization = Mock.Of<Authorization>();

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
            Assert.IsTrue(
                testRegistryClientFactory
                    .setAuthorization(mockAuthorization)
                    .newRegistryClient()
                    .getUserAgent()
                    .startsWith("jib"));

            Assert.IsTrue(
                testRegistryClientFactory
                    .setAuthorization(mockAuthorization)
                    .setUserAgentSuffix(null)
                    .newRegistryClient()
                    .getUserAgent()
                    .startsWith("jib"));
        }

        [Test]
        public void testGetUserAgent()
        {
            RegistryClient registryClient =
                testRegistryClientFactory
                    .setAllowInsecureRegistries(true)
                    .setUserAgentSuffix("some user agent suffix")
                    .newRegistryClient();

            Assert.IsTrue(registryClient.getUserAgent().startsWith("jib "));
            Assert.IsTrue(registryClient.getUserAgent().endsWith(" some user agent suffix"));
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
