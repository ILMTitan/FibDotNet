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
using com.google.cloud.tools.jib.builder.steps;
using com.google.cloud.tools.jib.cache;
using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.docker
{
    /** Tests for {@link DockerClient}. */
    public class DockerClientTest
    {
        private IProcessBuilder mockProcessBuilder;
        private IProcess mockProcess;
        private IImageTarball imageTarball;

        [SetUp]
        public void setUp()
        {
            mockProcessBuilder = Mock.Of<IProcessBuilder>();
            mockProcess = Mock.Of<IProcess>();
            imageTarball = Mock.Of<IImageTarball>();
            Mock.Get(mockProcessBuilder).Setup(m => m.start()).Returns(mockProcess);

            Mock.Get(imageTarball).Setup(i => i.writeToAsync(It.IsAny<Stream>()))
                .Returns<Stream>(async s => await s.WriteAsync("jib".getBytes(StandardCharsets.UTF_8)));
        }

        [Test]
        public void testIsDockerInstalled_fail()
        {
            Assert.IsFalse(DockerClient.isDockerInstalled(Paths.get("path/to/nonexistent/file")));
        }

        [Test]
        public async Task testLoadAsync()
        {
            DockerClient testDockerClient =
                new DockerClient(
                    subcommand =>
                    {
                        Assert.AreEqual(Collections.singletonList("load"), subcommand);
                        return mockProcessBuilder;
                    });
            Mock.Get(mockProcess).Setup(m => m.waitFor()).Returns(0);

            // Captures stdin.
            MemoryStream byteArrayOutputStream = new MemoryStream();
            Mock.Get(mockProcess).Setup(m => m.getOutputStream()).Returns(byteArrayOutputStream);

            // Simulates stdout.
            Mock.Get(mockProcess).Setup(m => m.getInputStream()).Returns(new MemoryStream("output".getBytes(StandardCharsets.UTF_8)));

            string output = await testDockerClient.loadAsync(imageTarball).ConfigureAwait(false);

            Assert.AreEqual(
                "jib", StandardCharsets.UTF_8.GetString(byteArrayOutputStream.toByteArray()));
            Assert.AreEqual("output", output);
        }

        [Test]
        public async System.Threading.Tasks.Task testLoad_stdinFailAsync()
        {
            DockerClient testDockerClient = new DockerClient(_ => mockProcessBuilder);

            Mock.Get(mockProcess)
                .Setup(m =>
                    m.getOutputStream().WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Throws<IOException>();

            Mock.Get(mockProcess).Setup(m => m.GetErrorReader().ReadToEnd()).Returns("error");

            try
            {
                await testDockerClient.loadAsync(imageTarball).ConfigureAwait(false);
                Assert.Fail("Write should have failed");
            }
            catch (IOException ex)
            {
                Assert.AreEqual("'docker load' command failed with error: error", ex.getMessage());
            }
        }

        [Test]
        public async System.Threading.Tasks.Task testLoad_stdinFail_stderrFailAsync()
        {
            DockerClient testDockerClient = new DockerClient(_ => mockProcessBuilder);
            IOException expectedIOException = new IOException();

            Mock.Get(mockProcess)
                .Setup(m =>
                    m.getOutputStream().WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Throws(expectedIOException);

            Mock.Get(mockProcess).Setup(m => m.GetErrorReader().ReadToEnd()).Throws<IOException>();
            try
            {
                await testDockerClient.loadAsync(imageTarball).ConfigureAwait(false);
                Assert.Fail("Write should have failed");
            }
            catch (IOException ex)
            {
                Assert.AreSame(expectedIOException, ex);
            }
        }

        [Test]
        public async Task testLoad_stdoutFailAsync()
        {
            DockerClient testDockerClient = new DockerClient(_ => mockProcessBuilder);
            Mock.Get(mockProcess).Setup(m => m.waitFor()).Returns(1);

            Mock.Get(mockProcess).Setup(m => m.getOutputStream()).Returns(Stream.Null);

            Mock.Get(mockProcess).Setup(m => m.getInputStream()).Returns(new MemoryStream("ignored".getBytes(StandardCharsets.UTF_8)));

            Mock.Get(mockProcess).Setup(m => m.getErrorStream()).Returns(new MemoryStream("error".getBytes(StandardCharsets.UTF_8)));

            try
            {
                await testDockerClient.loadAsync(imageTarball).ConfigureAwait(false);
                Assert.Fail("Process should have failed");
            }
            catch (IOException ex)
            {
                Assert.AreEqual("'docker load' command failed with output: error", ex.getMessage());
            }
        }

        [Test]
        public void testTag()
        {
            DockerClient testDockerClient =
                new DockerClient(
                    subcommand =>
                    {
                        Assert.AreEqual(Arrays.asList("tag", "original", "new"), subcommand);
                        return mockProcessBuilder;
                    });
            Mock.Get(mockProcess).Setup(m => m.waitFor()).Returns(0);

            testDockerClient.tag(ImageReference.of(null, "original", null), ImageReference.parse("new"));
        }

        [Test]
        public void testDefaultProcessorBuilderFactory_customExecutable()
        {
            ProcessBuilder processBuilder =
                DockerClient.defaultProcessBuilderFactory("docker-executable", ImmutableDictionary.Create<string, string>())
                    .apply(Arrays.asList("sub", "command"));

            Assert.AreEqual("docker-executable sub command", processBuilder.command());
            CollectionAssert.AreEquivalent(
                Environment.GetEnvironmentVariables()
                    .Cast<DictionaryEntry>()
                    .ToDictionary(e => e.Key.ToString(), e=>e.Value.ToString()),
                processBuilder.environment());
        }

        [Test]
        public void testDefaultProcessorBuilderFactory_customEnvironment()
        {
            ImmutableDictionary<string, string> environment = ImmutableDic.of("Key1", "Value1");

            var expectedEnvironment = new Dictionary<string, string>(
                Environment.GetEnvironmentVariables()
                    .Cast<DictionaryEntry>()
                    .Select(e => new KeyValuePair<string, string>(e.Key?.ToString(), e.Value?.ToString())));
            expectedEnvironment.putAll(environment);

            ProcessBuilder processBuilder =
                DockerClient.defaultProcessBuilderFactory("docker", environment)
                    .apply(Collections.emptyList<string>());

            CollectionAssert.AreEquivalent(expectedEnvironment, processBuilder.environment());
        }

        [Test]
        public void testTag_fail()
        {
            DockerClient testDockerClient =
                new DockerClient(
                    subcommand =>
                    {
                        Assert.AreEqual(Arrays.asList("tag", "original", "new"), subcommand);
                        return mockProcessBuilder;
                    });
            Mock.Get(mockProcess).Setup(m => m.waitFor()).Returns(1);

            Mock.Get(mockProcess).Setup(m => m.getErrorStream()).Returns(new MemoryStream("error".getBytes(StandardCharsets.UTF_8)));

            try
            {
                testDockerClient.tag(ImageReference.of(null, "original", null), ImageReference.parse("new"));
                Assert.Fail("docker tag should have failed");
            }
            catch (IOException ex)
            {
                Assert.AreEqual("'docker tag' command failed with error: error", ex.getMessage());
            }
        }
    }
}
