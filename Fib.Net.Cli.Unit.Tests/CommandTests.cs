﻿// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Fib.Net.Core.Api;
using Fib.Net.Core.BuildSteps;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Events;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Cli.Unit.Tests
{
    public class CommandTests : IDisposable
    {
        private TestableCommand _objectUnderTest;
        private readonly StringWriter _error = new StringWriter();
        private readonly StringWriter _output = new StringWriter();

        public void Dispose()
        {
            _output.Dispose();
            _error.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            _output.GetStringBuilder().Clear();
            _error.GetStringBuilder().Clear();
            _objectUnderTest = new TestableCommand();
        }

        [Test]
        public void TestExecuteAsync_ThrowsOutputArgumentNull()
        {
            var e = Assert.ThrowsAsync<ArgumentNullException>(
                () => _objectUnderTest.ExecuteAsync(null, _error));
            Assert.That(e.Message, Contains.Substring("output"));
        }

        [Test]
        public void TestExecuteAsync_ThrowsErrorArgumentNull()
        {
            var e = Assert.ThrowsAsync<ArgumentNullException>(
                () => _objectUnderTest.ExecuteAsync(_output, null));
            Assert.That(e.Message, Contains.Substring("error"));
        }

        [Test]
        public async Task TestExecuteAsync_CallsCreateContainerizer()
        {
            _objectUnderTest.ConfigFile =
                Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "trivial-config.json");
            await _objectUnderTest.ExecuteAsync(_output, _error).ConfigureAwait(false);

            Assert.AreEqual("library/trivial-image", _objectUnderTest.ImageReference.GetRepository());
        }

        [Test]
        public async Task TestExecuteAsync_ConfiguresContainerizer()
        {
            _objectUnderTest.ConfigFile =
                Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "offline-config.json");
            await _objectUnderTest.ExecuteAsync(_output, _error).ConfigureAwait(false);

            Mock.Get(_objectUnderTest.Containerizer).Verify(c => c.SetOfflineMode(true), Times.Once);
        }

        [Test]
        public async Task TestExecuteAsync_OutputsResult()
        {
            _objectUnderTest.ConfigFile =
                Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "trivial-config.json");
            Mock.Get(_objectUnderTest.Containerizer)
                .Setup(c => c.CreateStepsRunner(It.IsAny<BuildConfiguration>()).RunAsync())
                .Returns(
                    Task.FromResult(
                        Mock.Of<IBuildResult>(
                            r => r.GetImageDigest()
                            == DescriptorDigest.FromHash(new string('a', DescriptorDigest.HashLength))
                            && r.GetImageId()
                            == DescriptorDigest.FromHash(new string('b', DescriptorDigest.HashLength)))));
            await _objectUnderTest.ExecuteAsync(_output, _error).ConfigureAwait(false);

            Assert.That(
                _output.ToString(),
                Contains.Substring($"ImageDigest:sha256:{new string('a', DescriptorDigest.HashLength)}"));
            Assert.That(
                _output.ToString(),
                Contains.Substring($"ImageId:sha256:{new string('b', DescriptorDigest.HashLength)}"));
        }

        [Test]
        public async Task TestExecuteAsync_SetsToolName()
        {
            _objectUnderTest.ConfigFile =
                Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "trivial-config.json");
            _objectUnderTest.ToolName = "ToolName";
            await _objectUnderTest.ExecuteAsync(_output, _error).ConfigureAwait(false);

            Mock.Get(_objectUnderTest.Containerizer).Verify(c => c.SetToolName("ToolName"), Times.Once);
        }

        [Test]
        public async Task TestExecuteAsync_OutputsErrorEventsToErrorWriter()
        {
            _objectUnderTest.ConfigFile =
                Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "trivial-config.json");
            Mock.Get(_objectUnderTest.Containerizer)
                .Setup(c => c.AddEventHandler(It.IsAny<Action<IFibEvent>>()))
                .Callback<Action<IFibEvent>>(a => a(LogEvent.Error("ErrorMessage")));
            await _objectUnderTest.ExecuteAsync(_output, _error).ConfigureAwait(false);

            Assert.That(_error.ToString(), Contains.Substring("LogEvent:Error:ErrorMessage"));
        }

        [Test]
        public async Task TestExecuteAsync_OutputsOtherEventsToOutputWriter()
        {
            _objectUnderTest.ConfigFile =
                Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "trivial-config.json");
            Mock.Get(_objectUnderTest.Containerizer)
                .Setup(c => c.AddEventHandler(It.IsAny<Action<IFibEvent>>()))
                .Callback<Action<IFibEvent>>(a => a(Mock.Of<IFibEvent>(e => e.ToString() == "MockFibEvent")));
            await _objectUnderTest.ExecuteAsync(_output, _error).ConfigureAwait(false);

            Assert.That(_output.ToString(), Contains.Substring("MockFibEvent"));
        }

        private class TestableCommand : Command
        {
            public IContainerizer Containerizer { get; }
            public ImageReference ImageReference { get; private set; }

            public TestableCommand()
            {
                Containerizer = Mock.Of<IContainerizer>();
                Mock.Get(Containerizer).DefaultValueProvider = DefaultValueProvider.Mock;
                Mock.Get(Containerizer).Setup(c => c.WithAdditionalTags(It.IsAny<IEnumerable<string>>())).Returns(Containerizer);
                Mock.Get(Containerizer).Setup(c => c.SetAllowInsecureRegistries(It.IsAny<bool>())).Returns(Containerizer);
                Mock.Get(Containerizer).Setup(c => c.SetOfflineMode(It.IsAny<bool>())).Returns(Containerizer);
                Mock.Get(Containerizer).Setup(c => c.SetApplicationLayersCache(It.IsAny<string>())).Returns(Containerizer);
                Mock.Get(Containerizer).Setup(c => c.SetBaseImageLayersCache(It.IsAny<string>())).Returns(Containerizer);
                Mock.Get(Containerizer).Setup(c => c.GetApplicationLayersCacheDirectory()).Returns(TestContext.CurrentContext.WorkDirectory);
                Mock.Get(Containerizer).Setup(c => c.GetImageConfiguration()).Returns(() => ImageConfiguration.CreateBuilder(ImageReference).Build());
                Mock.Get(Containerizer).Setup(c => c.GetBaseImageLayersCacheDirectory()).Returns(TestContext.CurrentContext.WorkDirectory);
                Mock.Get(Containerizer).Setup(c => c.BuildEventHandlers()).Returns(new EventHandlers(ImmutableArray.Create<Action<IFibEvent>>()));
            }

            protected override IContainerizer CreateContainerizer(ImageReference imageReference)
            {
                ImageReference = imageReference;
                return Containerizer;
            }
        }
    }
}
