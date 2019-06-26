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
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.registry.credentials;
using Jib.Net.Core.Api;
using Jib.Net.Core.Events.Progress;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{
    /** Tests for {@link RetrieveRegistryCredentialsStep}. */
    public class RetrieveRegistryCredentialsStepTest
    {
        private readonly IEventHandlers mockEventHandlers = Mock.Of<IEventHandlers>();

        [Test]
        public void testCall_retrieved()
        {
            BuildConfiguration buildConfiguration =
                makeFakeBuildConfiguration(
                    Arrays.asList<CredentialRetriever>(
                        Option.Empty<Credential>,
                        () => Option.Of(Credential.from("baseusername", "basepassword"))),
                    Arrays.asList<CredentialRetriever>(
                        () => Option.Of(Credential.from("targetusername", "targetpassword")),
                        () => Option.Of(Credential.from("ignored", "ignored"))));

            Assert.AreEqual(
                Credential.from("baseusername", "basepassword"),
                RetrieveRegistryCredentialsStep.forBaseImage(
                        buildConfiguration,
                        ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer())
                    .call());
            Assert.AreEqual(
                Credential.from("targetusername", "targetpassword"),
                RetrieveRegistryCredentialsStep.forTargetImage(
                        buildConfiguration,
                        ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer())
                    .call());
        }

        [Test]
        public void testCall_none()
        {
            BuildConfiguration buildConfiguration =
                makeFakeBuildConfiguration(
                    Arrays.asList<CredentialRetriever>(Option.Empty<Credential>, Option.Empty<Credential>),
                    new List<CredentialRetriever>());
            Assert.IsNull(
                RetrieveRegistryCredentialsStep.forBaseImage(
                        buildConfiguration,
                        ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer())
                    .call());

            Mock.Get(mockEventHandlers).Verify(e => e.Dispatch(It.IsAny<ProgressEvent>()), Times.AtLeastOnce);
            Mock.Get(mockEventHandlers).Verify(m => m.Dispatch(LogEvent.info("No credentials could be retrieved for registry baseregistry")));

            Assert.IsNull(
                RetrieveRegistryCredentialsStep.forTargetImage(
                        buildConfiguration,
                        ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer())
                    .call());

            Mock.Get(mockEventHandlers).Verify(m => m.Dispatch(LogEvent.info("No credentials could be retrieved for registry baseregistry")));
        }

        [Test]
        public async Task testCall_exceptionAsync()
        {
            CredentialRetrievalException credentialRetrievalException =
                Mock.Of<CredentialRetrievalException>();
            BuildConfiguration buildConfiguration =
                makeFakeBuildConfiguration(
                    new List<CredentialRetriever> { () => throw credentialRetrievalException },
                    new List<CredentialRetriever>());
            try
            {
                await RetrieveRegistryCredentialsStep.forBaseImage(
                        buildConfiguration,
                        ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer())
                    .getFuture().ConfigureAwait(false);
                Assert.Fail("Should have thrown exception");
            }
            catch (CredentialRetrievalException ex)
            {
                Assert.AreSame(credentialRetrievalException, ex);
            }
        }

        private BuildConfiguration makeFakeBuildConfiguration(
            List<CredentialRetriever> baseCredentialRetrievers,
            IList<CredentialRetriever> targetCredentialRetrievers)
        {
            ImageReference baseImage = ImageReference.of("baseregistry", "ignored", null);
            ImageReference targetImage = ImageReference.of("targetregistry", "ignored", null);
            return BuildConfiguration.builder()
                .setEventHandlers(mockEventHandlers)
                .setBaseImageConfiguration(
                    ImageConfiguration.builder(baseImage)
                        .setCredentialRetrievers(baseCredentialRetrievers)
                        .build())
                .setTargetImageConfiguration(
                    ImageConfiguration.builder(targetImage)
                        .setCredentialRetrievers(targetCredentialRetrievers)
                        .build())
                .setBaseImageLayersCacheDirectory(Paths.get("ignored"))
                .setApplicationLayersCacheDirectory(Paths.get("ignored"))
                .build();
        }
    }
}
