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

using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.registry;
using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;

namespace com.google.cloud.tools.jib.api
{






    /** Integration tests for {@link Jib}. */
    public class JibIntegrationTest
    {
        [ClassRule]
        public static readonly LocalRegistry localRegistry = new LocalRegistry(5000, "username", "password");

        [Rule] public readonly TemporaryFolder cacheFolder = new TemporaryFolder();

        /**
         * Pulls a built image and attempts to run it.
         *
         * @param imageReference the image reference of the built image
         * @return the container output
         * @throws IOException if an I/O exception occurs
         * @throws InterruptedException if the process was interrupted
         */
        private static string pullAndRunBuiltImage(string imageReference)
        {
            localRegistry.pull(imageReference);
            return new Command("docker", "run", "--rm", imageReference).run();
        }

        [SetUp]
        public void setUp()
        {
            Environment.SetEnvironmentVariable("sendCredentialsOverHttp", "true");
        }

        [TearDown]
        public void tearDown()
        {
            Environment.SetEnvironmentVariable("sendCredentialsOverHttp", null);
        }

        [Test]
        public void testBasic_helloWorld()
        {
            ImageReference targetImageReference =
                ImageReference.of("localhost:5000", "jib-core", "basic-helloworld");
            JibContainer jibContainer =
                Jib.from("busybox")
                    .setEntrypoint("echo", "Hello World")
                    .containerize(
                        Containerizer.to(
                                RegistryImage.named(targetImageReference)
                                    .addCredentialRetriever(
                                        () => Optional.of(Credential.from("username", "password"))))
                            .setAllowInsecureRegistries(true));

            Assert.AreEqual("Hello World\n", pullAndRunBuiltImage(targetImageReference.toString()));
            Assert.AreEqual(
                "Hello World\n",
                pullAndRunBuiltImage(
                    targetImageReference.withTag(jibContainer.getDigest().toString()).toString()));
        }

        [Test]
        public void testScratch()
        {
            ImageReference targetImageReference =
                ImageReference.of("localhost:5000", "jib-core", "basic-scratch");
            Jib.fromScratch()
                .containerize(
                    Containerizer.to(
                            RegistryImage.named(targetImageReference)
                                .addCredentialRetriever(
                                    () => Optional.of(Credential.from("username", "password"))))
                        .setAllowInsecureRegistries(true));

            // Check that resulting image has no layers
            localRegistry.pull(targetImageReference.toString());
            string inspectOutput = new Command("docker", "inspect", targetImageReference.toString()).run();
            Assert.IsFalse(inspectOutput.contains("\"Layers\": ["), "docker inspect output contained layers: " + inspectOutput);
        }

        [Test]
        public void testOffline()
        {
            LocalRegistry tempRegistry = new LocalRegistry(5001);
            tempRegistry.start();
            tempRegistry.pullAndPushToLocal("busybox", "busybox");
            SystemPath cacheDirectory = cacheFolder.getRoot().toPath();

            ImageReference targetImageReferenceOnline =
                ImageReference.of("localhost:5001", "jib-core", "basic-online");
            ImageReference targetImageReferenceOffline =
                ImageReference.of("localhost:5001", "jib-core", "basic-offline");

            JibContainerBuilder jibContainerBuilder =
                Jib.from("localhost:5001/busybox").setEntrypoint("echo", "Hello World");

            // Should fail since Jib can't build to registry offline
            try
            {
                jibContainerBuilder.containerize(
                    Containerizer.to(RegistryImage.named(targetImageReferenceOffline)).setOfflineMode(true));
                Assert.Fail();
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Cannot build to a container registry in offline mode", ex.getMessage());
            }

            // Should fail since Jib hasn't cached the base image yet
            try
            {
                jibContainerBuilder.containerize(
                    Containerizer.to(DockerDaemonImage.named(targetImageReferenceOffline))
                        .setBaseImageLayersCache(cacheDirectory)
                        .setOfflineMode(true));
                Assert.Fail();
            }
            catch (ExecutionException ex)
            {
                Assert.AreEqual(
                    "Cannot run Jib in offline mode; localhost:5001/busybox not found in local Jib cache",
                    ex.getCause().getMessage());
            }

            // Run online to cache the base image
            jibContainerBuilder.containerize(
                Containerizer.to(DockerDaemonImage.named(targetImageReferenceOnline))
                    .setBaseImageLayersCache(cacheDirectory)
                    .setAllowInsecureRegistries(true));

            // Run again in offline mode, should succeed this time
            tempRegistry.stop();
            jibContainerBuilder.containerize(
                Containerizer.to(DockerDaemonImage.named(targetImageReferenceOffline))
                    .setBaseImageLayersCache(cacheDirectory)
                    .setOfflineMode(true));

            // Verify output
            Assert.AreEqual(
                "Hello World\n",
                new Command("docker", "run", "--rm", targetImageReferenceOffline.toString()).run());
        }
    }
}
