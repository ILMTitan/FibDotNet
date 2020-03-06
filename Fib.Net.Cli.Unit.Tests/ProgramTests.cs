// Copyright 2020 James Przybylinski
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

using CommandLine;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Cli.Unit.Tests
{
    public class ProgramTests : IDisposable
    {
        private readonly StringWriter _outputWriter = new StringWriter();
        private readonly StringWriter _errorWriter = new StringWriter();
        private readonly StringWriter _helpWriter = new StringWriter();
        private readonly Parser _parser ;
        private Program _objectUnderTest;

        public ProgramTests()
        {
            _parser = new Parser(s => s.HelpWriter = _helpWriter);
        }

        public void Dispose()
        {
            _outputWriter.Dispose();
            _errorWriter.Dispose();
            _helpWriter.Dispose();
            _parser.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            _outputWriter.GetStringBuilder().Clear();
            _errorWriter.GetStringBuilder().Clear();
            _helpWriter.GetStringBuilder().Clear();
            _objectUnderTest = new Program(_outputWriter, _errorWriter, _parser);
        }

        [Test]
        public async Task TestMain_NoArgs()
        {
            int result = await _objectUnderTest.ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(result, 1);
            Assert.That(_helpWriter.ToString(), Contains.Substring("No verb selected."));
        }

        [Test]
        public async Task TestMain_HelpNoVerb()
        {
            int result = await _objectUnderTest.ExecuteAsync("help").ConfigureAwait(false);

            Assert.AreEqual(0, result);
            Assert.That(_helpWriter.ToString(), Contains.Substring("tar"));
            Assert.That(_helpWriter.ToString(), Contains.Substring("daemon"));
            Assert.That(_helpWriter.ToString(), Contains.Substring("push"));
        }

        [Test]
        public async Task TestMain_HelpTar()
        {
            int result = await _objectUnderTest.ExecuteAsync("help", "tar").ConfigureAwait(false);

            Assert.AreEqual(0, result);
            Assert.That(_helpWriter.ToString(), Contains.Substring("outputfile"));
            Assert.That(_helpWriter.ToString(), Contains.Substring("toolname"));
            Assert.That(_helpWriter.ToString(), Contains.Substring("configfile"));

            _helpWriter.GetStringBuilder().Clear();
            result = await _objectUnderTest.ExecuteAsync("tar", "--help").ConfigureAwait(false);

            Assert.AreEqual(0, result);
            Assert.That(_helpWriter.ToString(), Contains.Substring("outputfile"));
            Assert.That(_helpWriter.ToString(), Contains.Substring("toolname"));
            Assert.That(_helpWriter.ToString(), Contains.Substring("configfile"));
        }

        [Test]
        public async Task TestMain_HelpDaemon()
        {
            int result = await _objectUnderTest.ExecuteAsync("help", "daemon").ConfigureAwait(false);

            Assert.AreEqual(0, result);
            Assert.That(_helpWriter.ToString(), Contains.Substring("toolname"));
            Assert.That(_helpWriter.ToString(), Contains.Substring("configfile"));

            _helpWriter.GetStringBuilder().Clear();
            result = await _objectUnderTest.ExecuteAsync("daemon", "--help").ConfigureAwait(false);

            Assert.AreEqual(0, result);
            Assert.That(_helpWriter.ToString(), Contains.Substring("toolname"));
            Assert.That(_helpWriter.ToString(), Contains.Substring("configfile"));
        }

        [Test]
        public async Task TestMain_HelpPush()
        {
            int result = await _objectUnderTest.ExecuteAsync("help", "push").ConfigureAwait(false);

            Assert.AreEqual(0, result);
            Assert.That(_helpWriter.ToString(), Contains.Substring("toolname"));
            Assert.That(_helpWriter.ToString(), Contains.Substring("configfile"));

            _helpWriter.GetStringBuilder().Clear();
            result = await _objectUnderTest.ExecuteAsync("push", "--help").ConfigureAwait(false);

            Assert.AreEqual(0, result);
            Assert.That(_helpWriter.ToString(), Contains.Substring("toolname"));
            Assert.That(_helpWriter.ToString(), Contains.Substring("configfile"));
        }
    }
}