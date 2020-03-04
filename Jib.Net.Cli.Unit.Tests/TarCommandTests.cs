using Jib.Net.Core.Api;
using NUnit.Framework;

namespace Jib.Net.Cli.Unit.Tests
{
    public class TarCommandTests
    {
        private class TestableTarCommand : TarCommand
        {
            public new IContainerizer CreateContainerizer(ImageReference imageReference)
            {
                return base.CreateContainerizer(imageReference);
            }
        }

        [Test]
        public void TestCreateContainerizer_CreatesForDockerClient()
        {
            var result = new TestableTarCommand { OutputFile = "OutputFile" }
                .CreateContainerizer(ImageReference.Parse("image-reference"));
            Assert.AreEqual(Containerizer.DescriptionForImageTarFile, result.GetDescription());
        }
    }
}
