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

namespace com.google.cloud.tools.jib.builder.steps {

























/** Tests for {@link RetrieveRegistryCredentialsStep}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class RetrieveRegistryCredentialsStepTest {

  [Mock] private EventHandlers mockEventHandlers;
  [Mock] private ListeningExecutorService mockListeningExecutorService;

  [TestMethod]
  public void testCall_retrieved() {
    BuildConfiguration buildConfiguration =
        makeFakeBuildConfiguration(
            Arrays.asList(
                Optional.empty,
                () => Optional.of(Credential.from("baseusername", "basepassword"))),
            Arrays.asList(
                () => Optional.of(Credential.from("targetusername", "targetpassword")),
                () => Optional.of(Credential.from("ignored", "ignored"))));

    Assert.assertEquals(
        Credential.from("baseusername", "basepassword"),
        RetrieveRegistryCredentialsStep.forBaseImage(
                mockListeningExecutorService,
                buildConfiguration,
                ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer())
            .call());
    Assert.assertEquals(
        Credential.from("targetusername", "targetpassword"),
        RetrieveRegistryCredentialsStep.forTargetImage(
                mockListeningExecutorService,
                buildConfiguration,
                ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer())
            .call());
  }

  [TestMethod]
  public void testCall_none() {
    BuildConfiguration buildConfiguration =
        makeFakeBuildConfiguration(
            Arrays.asList(Optional::empty, Optional.empty), Collections.emptyList());
    Assert.assertNull(
        RetrieveRegistryCredentialsStep.forBaseImage(
                mockListeningExecutorService,
                buildConfiguration,
                ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer())
            .call());

    Mockito.verify(mockEventHandlers, Mockito.atLeastOnce())
        .dispatch(Mockito.any(typeof(ProgressEvent)));
    Mockito.verify(mockEventHandlers)
        .dispatch(LogEvent.info("No credentials could be retrieved for registry baseregistry"));

    Assert.assertNull(
        RetrieveRegistryCredentialsStep.forTargetImage(
                mockListeningExecutorService,
                buildConfiguration,
                ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer())
            .call());

    Mockito.verify(mockEventHandlers)
        .dispatch(LogEvent.info("No credentials could be retrieved for registry baseregistry"));
  }

  [TestMethod]
  public void testCall_exception() {
    CredentialRetrievalException credentialRetrievalException =
        Mockito.mock(typeof(CredentialRetrievalException));
    BuildConfiguration buildConfiguration =
        makeFakeBuildConfiguration(
            Collections.singletonList(
                () => {
                  throw credentialRetrievalException;
                }),
            Collections.emptyList());
    try {
      RetrieveRegistryCredentialsStep.forBaseImage(
              mockListeningExecutorService,
              buildConfiguration,
              ProgressEventDispatcher.newRoot(mockEventHandlers, "ignored", 1).newChildProducer())
          .call();
      Assert.fail("Should have thrown exception");

    } catch (CredentialRetrievalException ex) {
      Assert.assertSame(credentialRetrievalException, ex);
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
        .setExecutorService(MoreExecutors.newDirectExecutorService())
        .build();
  }
}
}
