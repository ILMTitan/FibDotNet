﻿using Jib.Net.Core.Api;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jib.Net.Cli.Unit.Tests
{
    public class PushCommandTests
    {
        private class TestablePushCommand : PushCommand
        {
            public new IContainerizer CreateContainerizer(ImageReference imageReference)
            {
                return base.CreateContainerizer(imageReference);
            }
        }

        [Test]
        public void TestCreateContainerizer_CreatesForDockerClient()
        {
            var result = new TestablePushCommand().CreateContainerizer(ImageReference.Parse("image-reference"));
            Assert.AreEqual(Containerizer.DescriptionForDockerRegistry, result.GetDescription());
        }
    }
}
