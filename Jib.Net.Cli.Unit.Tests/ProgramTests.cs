using CommandLine;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Jib.Net.Cli.Unit.Tests
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