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

using Jib.Net.Core.Api;
using Jib.Net.Core.Events;
using Jib.Net.Core.Events.Progress;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Core.Integration.Tests.Registry;
using Jib.Net.Test.Common;
using Jib.Net.Test.LocalRegistry;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Jib.Net.Core.Integration.Tests.Api
{
    // TODO: now it looks like we can move everything here into JibIntegrationTest.
    /** Integration tests for {@link Containerizer}. */
    public class ContainerizerIntegrationTest : HttpRegistryTest, IDisposable
    {
        /**
         * Helper class to hold a {@link ProgressEventHandler} and verify that it handles a full progress.
         */
        private class ProgressChecker : IDisposable
        {
            public readonly ProgressEventHandler progressEventHandler;

            private double lastProgress = 0.0;
            private volatile bool areTasksFinished = false;

            public ProgressChecker()
            {
                progressEventHandler = new ProgressEventHandler(update =>
                {
                    lastProgress = update.GetProgress();
                    areTasksFinished = update.GetUnfinishedLeafTasks().Length == 0;
                });
            }

            public void CheckCompletion()
            {
                Assert.AreEqual(1.0, lastProgress, DOUBLE_ERROR_MARGIN);
                Assert.IsTrue(areTasksFinished);
            }

            public void Dispose()
            {
                progressEventHandler.Dispose();
            }
        }

        private readonly TemporaryFolder temporaryFolder = new TemporaryFolder();

        private ProgressChecker progressChecker;

        private static readonly Logger logger = new Logger(TestContext.Out);

        private const string DISTROLESS_DIGEST =
            "sha256:f488c213f278bc5f9ffe3ddf30c5dbb2303a15a74146b738d12453088e662880";

        private const double DOUBLE_ERROR_MARGIN = 1e-10;

        public static readonly ImmutableArray<ILayerConfiguration> fakeLayerConfigurations =
                ImmutableArray.Create(
                    MakeLayerConfiguration("core/application/dependencies", "/app/libs/"),
                    MakeLayerConfiguration("core/application/resources", "/app/resources/"),
                    MakeLayerConfiguration("core/application/classes", "/app/classes/"));

        [SetUp]
        public void InitProgressChecker()
        {
            progressChecker = new ProgressChecker();
        }

        public void Dispose()
        {
            temporaryFolder.Dispose();
            progressChecker?.Dispose();
        }

        [TearDown]
        public void DisposeProgressChecker()
        {
            progressChecker.Dispose();
        }

        /**
         * Lists the files in the {@code resourcePath} resources directory and builds a {@link
         * LayerConfiguration} from those files.
         */
        private static ILayerConfiguration MakeLayerConfiguration(
            string resourcePath, string pathInContainer)
        {
            IEnumerable<SystemPath> fileStream =
                Files.List(Paths.Get(TestResources.GetResource(resourcePath).ToURI()));
            {
                LayerConfiguration.Builder layerConfigurationBuilder = LayerConfiguration.CreateBuilder();
                foreach (SystemPath i in fileStream)
                {
                    ((Func<SystemPath, LayerConfiguration.Builder>)(sourceFile =>
                        layerConfigurationBuilder.AddEntry(
                            sourceFile, AbsoluteUnixPath.Get(pathInContainer + sourceFile.GetFileName()))))(i);
                }
                return layerConfigurationBuilder.Build();
            }
        }

        private static void AssertDockerInspect(string imageReference)
        {
            string dockerContainerConfig = new Command("docker", "inspect", imageReference).Run();
            Assert.That(
                dockerContainerConfig, Does.Contain(
                    "            \"ExposedPorts\": {\n"
                        + "                \"1000/tcp\": {},\n"
                        + "                \"2000/tcp\": {},\n"
                        + "                \"2001/tcp\": {},\n"
                        + "                \"2002/tcp\": {},\n"
                        + "                \"3000/udp\": {}"));
            Assert.That(
                dockerContainerConfig, Does.Contain(
                    "            \"Labels\": {\n"
                        + "                \"key1\": \"value1\",\n"
                        + "                \"key2\": \"value2\"\n"
                        + "            }"));
            string dockerConfigEnv =
                new Command("docker", "inspect", "-f", "{{.Config.Env}}", imageReference).Run();
            Assert.That(dockerConfigEnv, Does.Contain("env1=envvalue1"));

            Assert.That(dockerConfigEnv, Does.Contain("env2=envvalue2"));

            string history = new Command("docker", "history", imageReference).Run();
            Assert.That(history, Does.Contain("jib-integration-test"));

            Assert.That(history, Does.Contain("bazel build ..."));
        }

        private static void AssertLayerSizer(int expected, string imageReference)
        {
            Command command =
                new Command("docker", "inspect", "-f", "\"{{json .RootFS.Layers}}\"", imageReference);
            string layers = command.Run().Trim();
            Assert.AreEqual(expected, JsonConvert.DeserializeObject<List<string>>(layers).Count);
        }

        [Test]
        public async Task TestSteps_ForBuildToDockerRegistryAsync()
        {
            Stopwatch s = Stopwatch.StartNew();
            JibContainer image1 =
                await BuildRegistryImageAsync(
                    ImageReference.Of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
                    ImageReference.Of("localhost:5000", "testimage", "testtag"),
                    new List<string>()).ConfigureAwait(false);

            progressChecker.CheckCompletion();

            logger.Info("Initial build time: " + s.Elapsed);
            s.Restart();
            JibContainer image2 =
                await BuildRegistryImageAsync(
                    ImageReference.Of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
                    ImageReference.Of("localhost:5000", "testimage", "testtag"),
                    new List<string>()).ConfigureAwait(false);

            logger.Info("Secondary build time: " + s.Elapsed);

            Assert.AreEqual(image1, image2);

            const string imageReference = "localhost:5000/testimage:testtag";
            localRegistry.Pull(imageReference);
            AssertDockerInspect(imageReference);
            AssertLayerSizer(7, imageReference);
            Assert.AreEqual(
                "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).Run());

            string imageReferenceByDigest = "localhost:5000/testimage@" + image1.GetDigest();
            localRegistry.Pull(imageReferenceByDigest);
            AssertDockerInspect(imageReferenceByDigest);
            Assert.AreEqual(
                "Hello, world. An argument.\n",
                new Command("docker", "run", "--rm", imageReferenceByDigest).Run());
        }

        [Test]
        public async Task TestSteps_ForBuildToDockerRegistry_MultipleTagsAsync()
        {
            await BuildRegistryImageAsync(
                ImageReference.Of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
                ImageReference.Of("localhost:5000", "testimage", "testtag"),
                new[] { "testtag2", "testtag3" }).ConfigureAwait(false);

            progressChecker.CheckCompletion();

            const string imageReference = "localhost:5000/testimage:testtag";
            localRegistry.Pull(imageReference);
            AssertDockerInspect(imageReference);
            Assert.AreEqual(
                "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).Run());

            const string imageReference2 = "localhost:5000/testimage:testtag2";
            localRegistry.Pull(imageReference2);
            AssertDockerInspect(imageReference2);
            Assert.AreEqual(
                "Hello, world. An argument.\n",
                new Command("docker", "run", "--rm", imageReference2).Run());

            const string imageReference3 = "localhost:5000/testimage:testtag3";
            localRegistry.Pull(imageReference3);
            AssertDockerInspect(imageReference3);
            Assert.AreEqual(
                "Hello, world. An argument.\n",
                new Command("docker", "run", "--rm", imageReference3).Run());
        }

        [Test]
        public async Task TestBuildToDockerRegistry_DockerHubBaseImageAsync()
        {
            await BuildRegistryImageAsync(
                ImageReference.Parse("openjdk:8-jre-alpine"),
                ImageReference.Of("localhost:5000", "testimage", "testtag"),
                new List<string>()).ConfigureAwait(false);

            progressChecker.CheckCompletion();
            const string imageReference = "localhost:5000/testimage:testtag";
            new Command("docker", "pull", imageReference).Run();
            Assert.AreEqual(
                "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).Run());
        }

        [Test]
        public async Task TestBuildToDockerDaemonAsync()
        {
            await BuildDockerDaemonImageAsync(
                ImageReference.Of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
                ImageReference.Of(null, "testdocker", null),
                new List<string>()).ConfigureAwait(false);

            progressChecker.CheckCompletion();

            AssertDockerInspect("testdocker");
            AssertLayerSizer(7, "testdocker");
            Assert.AreEqual(
                "Hello, world. An argument.\n", new Command("docker", "run", "--rm", "testdocker").Run());
        }

        [Test]
        public async Task TestBuildToDockerDaemon_MultipleTagsAsync()
        {
            const string imageReference = "testdocker";
            await BuildDockerDaemonImageAsync(
                ImageReference.Of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
                ImageReference.Of(null, imageReference, null),
                new[] { "testtag2", "testtag3" }).ConfigureAwait(false);

            progressChecker.CheckCompletion();
            AssertDockerInspect(imageReference);
            Assert.AreEqual(
                "Hello, world. An argument.\n", new Command("docker", "run", "--rm", imageReference).Run());
            AssertDockerInspect(imageReference + ":testtag2");
            Assert.AreEqual(
                "Hello, world. An argument.\n",
                new Command("docker", "run", "--rm", imageReference + ":testtag2").Run());
            AssertDockerInspect(imageReference + ":testtag3");
            Assert.AreEqual(
                "Hello, world. An argument.\n",
                new Command("docker", "run", "--rm", imageReference + ":testtag3").Run());
        }

        [Test]
        public async Task TestBuildTarballAsync()
        {
            SystemPath outputPath = temporaryFolder.NewFolder().ToPath().Resolve("test.tar");
            await BuildTarImageAsync(
                ImageReference.Of("gcr.io", "distroless/java", DISTROLESS_DIGEST),
                ImageReference.Of(null, "testtar", null),
                outputPath,
                new List<string>()).ConfigureAwait(false);

            progressChecker.CheckCompletion();

            new Command("docker", "load", "--input", JavaExtensions.ToString(outputPath)).Run();
            AssertLayerSizer(7, "testtar");
            Assert.AreEqual(
                "Hello, world. An argument.\n", new Command("docker", "run", "--rm", "testtar").Run());
        }

        private async Task<JibContainer> BuildRegistryImageAsync(
            ImageReference baseImage, ImageReference targetImage, IList<string> additionalTags)
        {
            return await BuildImageAsync(
                baseImage, Containerizer.To(RegistryImage.Named(targetImage)), additionalTags).ConfigureAwait(false);
        }

        private async Task<JibContainer> BuildDockerDaemonImageAsync(
            ImageReference baseImage, ImageReference targetImage, IList<string> additionalTags)
        {
            return await BuildImageAsync(
                baseImage, Containerizer.To(DockerDaemonImage.Named(targetImage)), additionalTags).ConfigureAwait(false);
        }

        private async Task<JibContainer> BuildTarImageAsync(
            ImageReference baseImage,
            ImageReference targetImage,
            SystemPath outputPath,
            List<string> additionalTags)
        {
            return await BuildImageAsync(
                baseImage,
                Containerizer.To(TarImage.Named(targetImage).SaveTo(outputPath)),
                additionalTags).ConfigureAwait(false);
        }

        private async Task<JibContainer> BuildImageAsync(
            ImageReference baseImage, Containerizer containerizer, IList<string> additionalTags)
        {
            JibContainerBuilder containerBuilder =
                JibContainerBuilder.From(baseImage)
                    .SetEntrypoint(new[] { "java", "-cp", "/app/resources:/app/classes:/app/libs/*", "HelloWorld" })
                    .SetProgramArguments(new List<string> { "An argument." })
                    .SetEnvironment(ImmutableDic.Of("env1", "envvalue1", "env2", "envvalue2"))
                    .SetExposedPorts(Port.Parse(new[] { "1000", "2000-2002/tcp", "3000/udp" }))
                    .SetLabels(ImmutableDic.Of("key1", "value1", "key2", "value2"))
                    .SetLayers(fakeLayerConfigurations);

            SystemPath cacheDirectory = temporaryFolder.NewFolder().ToPath();
            containerizer
                .SetBaseImageLayersCache(cacheDirectory)
                .SetApplicationLayersCache(cacheDirectory)
                .SetAllowInsecureRegistries(true)
                .SetToolName("jib-integration-test")
                .AddEventHandler<ProgressEvent>(progressChecker.progressEventHandler.Accept);
            foreach (string i in additionalTags)
            {
                containerizer.WithAdditionalTag(i);
            }
            return await containerBuilder.ContainerizeAsync(containerizer).ConfigureAwait(false);
        }
    }
}
