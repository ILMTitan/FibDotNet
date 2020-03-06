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

using Fib.Net.Core.Api;
using Fib.Net.Core.BuildSteps;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Events;
using Fib.Net.Core.Events.Progress;
using Fib.Net.Core.Registry.Credentials;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fib.Net.Core.Unit.Tests.BuildSteps
{
    /** Tests for {@link RetrieveRegistryCredentialsStep}. */
    public class RetrieveRegistryCredentialsStepTest
    {
        private readonly IEventHandlers mockEventHandlers = Mock.Of<IEventHandlers>();

        [Test]
        public void TestCall_retrieved()
        {
            BuildConfiguration buildConfiguration =
                MakeFakeBuildConfiguration(
                    new List<CredentialRetriever>
                    {
                        Maybe.Empty<Credential>,
                        () => Maybe.Of(Credential.From("baseusername", "basepassword"))
                    },
                    new List<CredentialRetriever>
                    {
                        () => Maybe.Of(Credential.From("targetusername", "targetpassword")),
                        () => Maybe.Of(Credential.From("ignored", "ignored"))
                    });

            Assert.AreEqual(
                Credential.From("baseusername", "basepassword"),
                RetrieveRegistryCredentialsStep.ForBaseImage(
                        buildConfiguration,
                        ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer())
                    .Call());
            Assert.AreEqual(
                Credential.From("targetusername", "targetpassword"),
                RetrieveRegistryCredentialsStep.ForTargetImage(
                        buildConfiguration,
                        ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer())
                    .Call());
        }

        [Test]
        public void TestCall_none()
        {
            BuildConfiguration buildConfiguration =
                MakeFakeBuildConfiguration(
                    new List<CredentialRetriever> { Maybe.Empty<Credential>, Maybe.Empty<Credential> },
                    new List<CredentialRetriever>());
            Assert.IsNull(
                RetrieveRegistryCredentialsStep.ForBaseImage(
                        buildConfiguration,
                        ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer())
                    .Call());

            Mock.Get(mockEventHandlers).Verify(e => e.Dispatch(It.IsAny<ProgressEvent>()), Times.AtLeastOnce);
            Mock.Get(mockEventHandlers).Verify(m => m.Dispatch(LogEvent.Info("No credentials could be retrieved for registry baseregistry")));

            Assert.IsNull(
                RetrieveRegistryCredentialsStep.ForTargetImage(
                        buildConfiguration,
                        ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer())
                    .Call());

            Mock.Get(mockEventHandlers).Verify(m => m.Dispatch(LogEvent.Info("No credentials could be retrieved for registry baseregistry")));
        }

        [Test]
        public async Task TestCall_exceptionAsync()
        {
            CredentialRetrievalException credentialRetrievalException =
                Mock.Of<CredentialRetrievalException>();
            BuildConfiguration buildConfiguration =
                MakeFakeBuildConfiguration(
                    new List<CredentialRetriever> { () => throw credentialRetrievalException },
                    new List<CredentialRetriever>());
            try
            {
                await RetrieveRegistryCredentialsStep.ForBaseImage(
                        buildConfiguration,
                        ProgressEventDispatcher.NewRoot(mockEventHandlers, "ignored", 1).NewChildProducer())
                    .GetFuture().ConfigureAwait(false);
                Assert.Fail("Should have thrown exception");
            }
            catch (CredentialRetrievalException ex)
            {
                Assert.AreSame(credentialRetrievalException, ex);
            }
        }

        private BuildConfiguration MakeFakeBuildConfiguration(
            List<CredentialRetriever> baseCredentialRetrievers,
            IList<CredentialRetriever> targetCredentialRetrievers)
        {
            ImageReference baseImage = ImageReference.Of("baseregistry", "ignored", null);
            ImageReference targetImage = ImageReference.Of("targetregistry", "ignored", null);
            return BuildConfiguration.CreateBuilder()
                .SetEventHandlers(mockEventHandlers)
                .SetBaseImageConfiguration(
                    ImageConfiguration.CreateBuilder(baseImage)
                        .SetCredentialRetrievers(baseCredentialRetrievers)
                        .Build())
                .SetTargetImageConfiguration(
                    ImageConfiguration.CreateBuilder(targetImage)
                        .SetCredentialRetrievers(targetCredentialRetrievers)
                        .Build())
                .SetBaseImageLayersCacheDirectory(Paths.Get("ignored"))
                .SetApplicationLayersCacheDirectory(Paths.Get("ignored"))
                .Build();
        }
    }
}
