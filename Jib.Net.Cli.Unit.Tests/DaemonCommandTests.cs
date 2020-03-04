using Jib.Net.Core.Api;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jib.Net.Cli.Unit.Tests
{
    public class DaemonCommandTests
    {
        private class TestableDaemonCommand : DaemonCommand
        {
            public new IContainerizer CreateContainerizer(ImageReference imageReference)
            {
                return base.CreateContainerizer(imageReference);
            }
        }

        [Test]
        public void TestCreateContainerizer_CreatesForDockerClient()
        {
            var result = new TestableDaemonCommand().CreateContainerizer(ImageReference.Parse("image-reference"));
            Assert.AreEqual(Containerizer.DescriptionForDockerDaemon, result.GetDescription());
        }
    }
}
