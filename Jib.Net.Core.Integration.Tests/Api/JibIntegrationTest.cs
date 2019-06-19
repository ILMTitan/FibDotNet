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
using System.IO;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.api
{
    /** Integration tests for {@link Jib}. */
    public class JibIntegrationTest
    {
        public static readonly LocalRegistry localRegistry = new LocalRegistry(5002, "username", "password");

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

        [OneTimeSetUp]
        public async Task OneTimeSetUpAsync()
        {
            await localRegistry.startAsync().ConfigureAwait(false);
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

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            localRegistry.stop();
        }

        [Test]
        public async Task testBasic_helloWorldAsync()
        {
            ImageReference targetImageReference =
                ImageReference.of("localhost:5002", "jib-core", "basic-helloworld");
            JibContainer jibContainer =
                await Jib.from("busybox")
                    .setEntrypoint("echo", "Hello World")
                    .containerizeAsync(
                        Containerizer.to(
                                RegistryImage.named(targetImageReference)
                                    .addCredentialRetriever(
                                        () => Optional.of(Credential.from("username", "password"))))
                            .setAllowInsecureRegistries(true)
                            .addEventHandler<JibEvent>(e=>TestContext.Out.WriteLine(e))).ConfigureAwait(false);

            Assert.AreEqual("Hello World\n", pullAndRunBuiltImage(targetImageReference.toString()));
            Assert.AreEqual(
                "Hello World\n",
                pullAndRunBuiltImage(
                    targetImageReference.withTag(jibContainer.getDigest().toString()).toString()));
        }

        [Test]
        public async Task testScratchAsync()
        {
            ImageReference targetImageReference =
                ImageReference.of("localhost:5002", "jib-core", "basic-scratch");
            await Jib.fromScratch()
                .containerizeAsync(
                    Containerizer.to(
                            RegistryImage.named(targetImageReference)
                                .addCredentialRetriever(
                                    () => Optional.of(Credential.from("username", "password"))))
                        .setAllowInsecureRegistries(true)).ConfigureAwait(false);

            // Check that resulting image has no layers
            localRegistry.pull(targetImageReference.toString());
            string inspectOutput = new Command("docker", "inspect", targetImageReference.toString()).run();
            Assert.IsFalse(inspectOutput.contains("\"Layers\": ["), "docker inspect output contained layers: " + inspectOutput);
        }

        [Test]
        public async Task testOfflineAsync()
        {
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
                await jibContainerBuilder.containerizeAsync(
                    Containerizer.to(RegistryImage.named(targetImageReferenceOffline)).setOfflineMode(true)).ConfigureAwait(false);
                Assert.Fail();
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Cannot build to a container registry in offline mode", ex.getMessage());
            }

            // Should fail since Jib hasn't cached the base image yet
            try
            {
                await jibContainerBuilder.containerizeAsync(
                    Containerizer.to(DockerDaemonImage.named(targetImageReferenceOffline))
                        .setBaseImageLayersCache(cacheDirectory)
                        .setOfflineMode(true)).ConfigureAwait(false);
                Assert.Fail();
            }
            catch (IOException ex)
            {
                Assert.AreEqual(
                    "Cannot run Jib in offline mode; localhost:5001/busybox not found in local Jib cache",
                    ex.getMessage());
            }
            using (LocalRegistry tempRegistry = new LocalRegistry(5001))
            {
                await tempRegistry.startAsync().ConfigureAwait(false);
                tempRegistry.pullAndPushToLocal("busybox", "busybox");

                // Run online to cache the base image
                await jibContainerBuilder.containerizeAsync(
                    Containerizer.to(DockerDaemonImage.named(targetImageReferenceOnline))
                        .setBaseImageLayersCache(cacheDirectory)
                        .setAllowInsecureRegistries(true)).ConfigureAwait(false);
            }

            // Run again in offline mode, should succeed this time
            await jibContainerBuilder.containerizeAsync(
                Containerizer.to(DockerDaemonImage.named(targetImageReferenceOffline))
                    .setBaseImageLayersCache(cacheDirectory)
                    .setOfflineMode(true)).ConfigureAwait(false);

            // Verify output
            Assert.AreEqual(
                "Hello World\n",
                new Command("docker", "run", "--rm", targetImageReferenceOffline.toString()).run());
        }
    }
}
