// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using Fib.Net.Core.Api;
using Fib.Net.Core.Docker;
using Fib.Net.Test.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fib.Net.Core.Unit.Tests.Docker
{
    /** Tests for {@link DockerClient}. */
    public class DockerClientTest
    {
        private IProcessBuilder mockProcessBuilder;
        private IProcess mockProcess;
        private IImageTarball imageTarball;

        [SetUp]
        public void SetUp()
        {
            mockProcessBuilder = Mock.Of<IProcessBuilder>();
            mockProcess = Mock.Of<IProcess>();
            imageTarball = Mock.Of<IImageTarball>();
            Mock.Get(mockProcessBuilder).Setup(m => m.Start()).Returns(mockProcess);

            Mock.Get(imageTarball).Setup(i => i.WriteToAsync(It.IsAny<Stream>()))
                .Returns<Stream>(async s => await s.WriteAsync(Encoding.UTF8.GetBytes("fib")));
        }

        [Test]
        public void TestIsDockerInstalled_fail()
        {
            Assert.IsFalse(DockerClient.IsDockerInstalled(Paths.Get("path/to/nonexistent/file")));
        }

        [Test]
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Memory Streams do not need to be disposed")]
        public async Task TestLoadAsync()
        {
            DockerClient testDockerClient =
                new DockerClient(
                    subcommand =>
                    {
                        Assert.AreEqual(new List<string> { "load" }, subcommand);
                        return mockProcessBuilder;
                    });
            Mock.Get(mockProcess).Setup(m => m.WaitFor()).Returns(0);

            // Captures stdin.
            MemoryStream byteArrayOutputStream = new MemoryStream();
            Mock.Get(mockProcess).Setup(m => m.GetOutputStream()).Returns(byteArrayOutputStream);

            // Simulates stdout.
            Mock.Get(mockProcess).Setup(m => m.GetInputStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes("output")));

            string output = await testDockerClient.LoadAsync(imageTarball).ConfigureAwait(false);

            Assert.AreEqual(
                "fib", Encoding.UTF8.GetString(byteArrayOutputStream.ToArray()));
            Assert.AreEqual("output", output);
        }

        [Test]
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "StringReader does not need to be disposed")]
        public async Task TestLoad_stdinFailAsync()
        {
            DockerClient testDockerClient = new DockerClient(_ => mockProcessBuilder);

            Mock.Get(mockProcess)
                .Setup(m =>
                    m.GetOutputStream().WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Throws<IOException>();

            Mock.Get(mockProcess).Setup(m => m.GetErrorReader()).Returns(new StringReader("error"));

            try
            {
                await testDockerClient.LoadAsync(imageTarball).ConfigureAwait(false);
                Assert.Fail("Write should have failed");
            }
            catch (IOException ex)
            {
                Assert.AreEqual("'docker load' command failed with error: error", ex.Message);
            }
        }

        [Test]
        public async Task TestLoad_stdinFail_stderrFailAsync()
        {
            DockerClient testDockerClient = new DockerClient(_ => mockProcessBuilder);
            IOException expectedIOException = new IOException();

            Mock.Get(mockProcess)
                .Setup(m =>
                    m.GetOutputStream().WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Throws(expectedIOException);

            Mock.Get(mockProcess).Setup(m => m.GetErrorReader().ReadToEnd()).Throws<IOException>();
            try
            {
                await testDockerClient.LoadAsync(imageTarball).ConfigureAwait(false);
                Assert.Fail("Write should have failed");
            }
            catch (IOException ex)
            {
                Assert.AreSame(expectedIOException, ex);
            }
        }

        [Test]
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Memory Streams do not need to be disposed")]
        public async Task TestLoad_stdoutFailAsync()
        {
            DockerClient testDockerClient = new DockerClient(_ => mockProcessBuilder);
            Mock.Get(mockProcess).Setup(m => m.WaitFor()).Returns(1);

            Mock.Get(mockProcess).Setup(m => m.GetOutputStream()).Returns(Stream.Null);

            Mock.Get(mockProcess).Setup(m => m.GetInputStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes("ignored")));

            Mock.Get(mockProcess).Setup(m => m.GetErrorStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes("error")));

            try
            {
                await testDockerClient.LoadAsync(imageTarball).ConfigureAwait(false);
                Assert.Fail("Process should have failed");
            }
            catch (IOException ex)
            {
                Assert.AreEqual("'docker load' command failed with output: error", ex.Message);
            }
        }

        [Test]
        public async Task TestTag()
        {
            DockerClient testDockerClient =
                new DockerClient(
                    subcommand =>
                    {
                        Assert.AreEqual(new[] { "tag", "original", "new" }, subcommand);
                        return mockProcessBuilder;
                    });
            Mock.Get(mockProcess).Setup(m => m.WaitFor()).Returns(0);

            ImageReference originalImageReference = ImageReference.Of(null, "original", null);
            ImageReference newImageReference = ImageReference.Parse("new");
            await testDockerClient.TagAsync(originalImageReference, newImageReference).ConfigureAwait(false);
        }

        [Test]
        public void TestDefaultProcessorBuilderFactory_customExecutable()
        {
            ProcessBuilder processBuilder =
                DockerClient.DefaultProcessBuilderFactory("docker-executable", ImmutableDictionary.Create<string, string>())
(new[] { "sub", "command" });

            Assert.AreEqual("docker-executable sub command", processBuilder.Command());
            CollectionAssert.AreEquivalent(
                Environment.GetEnvironmentVariables()
                    .Cast<DictionaryEntry>()
                    .ToDictionary(e => e.Key.ToString(), e => e.Value.ToString()),
                processBuilder.GetEnvironment());
        }

        [Test]
        public void TestDefaultProcessorBuilderFactory_customEnvironment()
        {
            ImmutableDictionary<string, string> environment = ImmutableDic.Of("Key1", "Value1");

            var expectedEnvironment = new Dictionary<string, string>(
                Environment.GetEnvironmentVariables()
                    .Cast<DictionaryEntry>()
                    .Select(e => new KeyValuePair<string, string>(e.Key?.ToString(), e.Value?.ToString())))
            {
                ["Key1"] = "Value1"
            };

            ProcessBuilder processBuilder =
                DockerClient.DefaultProcessBuilderFactory("docker", environment)(new List<string>());

            CollectionAssert.AreEquivalent(expectedEnvironment, processBuilder.GetEnvironment());
        }

        [Test]
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Memory Streams do not need to be disposed")]
        public async Task TestTag_fail()
        {
            DockerClient testDockerClient =
                new DockerClient(
                    subcommand =>
                    {
                        Assert.AreEqual(new[] { "tag", "original", "new" }, subcommand);
                        return mockProcessBuilder;
                    });
            Mock.Get(mockProcess).Setup(m => m.WaitFor()).Returns(1);

            Mock.Get(mockProcess).Setup(m => m.GetErrorStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes("error")));

            try
            {
                ImageReference originalImageReference = ImageReference.Of(null, "original", null);
                ImageReference newImageReference = ImageReference.Parse("new");
                await testDockerClient.TagAsync(originalImageReference, newImageReference).ConfigureAwait(false);
                Assert.Fail("docker tag should have failed");
            }
            catch (IOException ex)
            {
                Assert.AreEqual("'docker tag' command failed with error: error", ex.Message);
            }
        }
    }
}
