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
using Fib.Net.Core.FileSystem;
using Fib.Net.Test.Common;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Core.Integration.Tests.Api
{
    /** Integration tests for {@link Fib}. */
    public class FibIntegrationTest : IDisposable
    {
        public static readonly LocalRegistry localRegistry = new LocalRegistry(5002, "username", "password");

        private readonly TemporaryFolder cacheFolder = new TemporaryFolder();

        /**
         * Pulls a built image and attempts to run it.
         *
         * @param imageReference the image reference of the built image
         * @return the container output
         * @throws IOException if an I/O exception occurs
         * @throws InterruptedException if the process was interrupted
         */
        private static string PullAndRunBuiltImage(string imageReference)
        {
            localRegistry.Pull(imageReference);
            return new Command("docker", "run", "--rm", imageReference).Run();
        }

        [OneTimeSetUp]
        public async Task OneTimeSetUpAsync()
        {
            await localRegistry.StartAsync().ConfigureAwait(false);
        }

        [SetUp]
        public void SetUp()
        {
            Environment.SetEnvironmentVariable("sendCredentialsOverHttp", "true");
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("sendCredentialsOverHttp", null);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            localRegistry.Stop();
        }

        public void Dispose()
        {
            cacheFolder.Dispose();
        }

        [Test]
        public async Task TestBasic_HelloWorldAsync()
        {
            ImageReference targetImageReference =
                ImageReference.Of("localhost:5002", "fibdotnet-core", "basic-helloworld");
            FibContainer fibContainer =
                await FibContainerBuilder.From("busybox")
                    .SetEntrypoint("echo", "Hello World")
                    .ContainerizeAsync(
                        Containerizer.To(
                                RegistryImage.Named(targetImageReference)
                                    .AddCredentialRetriever(
                                        () => Maybe.Of(Credential.From("username", "password"))))
                            .SetAllowInsecureRegistries(true)
                            .AddEventHandler<IFibEvent>(e => TestContext.Out.WriteLine(e))).ConfigureAwait(false);

            Assert.AreEqual("Hello World\n", PullAndRunBuiltImage(targetImageReference.ToString()));
            Assert.AreEqual(
                "Hello World\n",
                PullAndRunBuiltImage(
                    targetImageReference.WithTag(fibContainer.GetDigest().ToString()).ToString()));
        }

        [Test]
        public async Task TestScratchAsync()
        {
            ImageReference targetImageReference =
                ImageReference.Of("localhost:5002", "fibdotnet-core", "basic-scratch");
            await FibContainerBuilder.FromScratch()
                .ContainerizeAsync(
                    Containerizer.To(
                            RegistryImage.Named(targetImageReference)
                                .AddCredentialRetriever(
                                    () => Maybe.Of(Credential.From("username", "password"))))
                        .SetAllowInsecureRegistries(true)).ConfigureAwait(false);

            // Check that resulting image has no layers
            localRegistry.Pull(targetImageReference.ToString());
            string inspectOutput = new Command("docker", "inspect", targetImageReference.ToString()).Run();
            Assert.That(inspectOutput, Does.Not.Contain("\"Layers\": ["), "docker inspect output contained layers: " + inspectOutput);
        }

        [Test]
        public async Task TestOfflineAsync()
        {
            SystemPath cacheDirectory = cacheFolder.GetRoot().ToPath();

            ImageReference targetImageReferenceOnline =
                ImageReference.Of("localhost:5001", "fibdotnet-core", "basic-online");
            ImageReference targetImageReferenceOffline =
                ImageReference.Of("localhost:5001", "fibdotnet-core", "basic-offline");

            FibContainerBuilder fibContainerBuilder =
                FibContainerBuilder.From("localhost:5001/busybox").SetEntrypoint("echo", "Hello World");

            // Should fail since Fib can't build to registry offline
            try
            {
                await fibContainerBuilder.ContainerizeAsync(
                    Containerizer.To(RegistryImage.Named(targetImageReferenceOffline)).SetOfflineMode(true)).ConfigureAwait(false);
                Assert.Fail();
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Cannot build to a container registry in offline mode", ex.Message);
            }

            // Should fail since Fib hasn't cached the base image yet
            try
            {
                await fibContainerBuilder.ContainerizeAsync(
                    Containerizer.To(DockerDaemonImage.Named(targetImageReferenceOffline))
                        .SetBaseImageLayersCache(cacheDirectory)
                        .SetOfflineMode(true)).ConfigureAwait(false);
                Assert.Fail();
            }
            catch (IOException ex)
            {
                Assert.AreEqual(
                    "Cannot run Fib in offline mode; localhost:5001/busybox not found in local Fib cache",
                    ex.Message);
            }
            using (LocalRegistry tempRegistry = new LocalRegistry(5001))
            {
                await tempRegistry.StartAsync().ConfigureAwait(false);
                tempRegistry.PullAndPushToLocal("busybox", "busybox");

                // Run online to cache the base image
                await fibContainerBuilder.ContainerizeAsync(
                    Containerizer.To(DockerDaemonImage.Named(targetImageReferenceOnline))
                        .SetBaseImageLayersCache(cacheDirectory)
                        .SetAllowInsecureRegistries(true)).ConfigureAwait(false);
            }

            // Run again in offline mode, should succeed this time
            await fibContainerBuilder.ContainerizeAsync(
                Containerizer.To(DockerDaemonImage.Named(targetImageReferenceOffline))
                    .SetBaseImageLayersCache(cacheDirectory)
                    .SetOfflineMode(true)).ConfigureAwait(false);

            // Verify output
            Assert.AreEqual(
                "Hello World\n",
                new Command("docker", "run", "--rm", targetImageReferenceOffline.ToString()).Run());
        }
    }
}
