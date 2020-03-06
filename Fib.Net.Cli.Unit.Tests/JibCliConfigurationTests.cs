using Fib.Net.Core.Api;
using Fib.Net.Core.Configuration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Fib.Net.Cli.Unit.Tests
{
    public class FibCliConfigurationTests
    {
        [Test]
        public void TestLoadAsync_ThrowsNull()
        {
            var e = Assert.ThrowsAsync<ArgumentNullException>(() => FibCliConfiguration.LoadAsync(null));
            Assert.AreEqual("configFile", e.ParamName);
        }

        [Test]
        public void TestGetTargetImageReference() {
            var config = new FibCliConfiguration
            {
                TargetImage = "registry.io/target-image",
                TargetTags = new[] { "tag1", "tag2" }
            };

            var result = config.GetTargetImageReference();

            Assert.AreEqual("registry.io", result.GetRegistry());
            Assert.AreEqual("target-image", result.GetRepository());
            Assert.AreEqual("tag1", result.GetTag());
        }

        [Test]
        public void TestGetTargetImageReference_NoTags()
        {
            var config = new FibCliConfiguration
            {
                TargetImage = "registry.io/target-image",
                TargetTags = Array.Empty<string>()
            };

            var result = config.GetTargetImageReference();

            Assert.AreEqual("registry.io", result.GetRegistry());
            Assert.AreEqual("target-image", result.GetRepository());
            Assert.AreEqual("latest", result.GetTag());
        }

        [Test]
        public void TestConfigureContainerizer_ThrowsForNull()
        {
            var config = new FibCliConfiguration();
            var e = Assert.Throws<ArgumentNullException>(() => config.ConfigureContainerizer(null));

            Assert.AreEqual("containerizer", e.ParamName);
        }

        [Test]
        public void TestConfigureContainerizer_WithAdditonalTags()
        {
            var config = new FibCliConfiguration
            {
                TargetTags = new string[] {"tag1", "tag2"}
            };

            var containerizer = Containerizer.To(DockerDaemonImage.Named("target-image:tag3"));
            config.ConfigureContainerizer(containerizer);

            Assert.AreEqual(new string[] { "tag1", "tag2" }, containerizer.GetAdditionalTags());
        }

        [Test]
        public void TestConfigureContainerizer_WithAdditonalTags_HandlesNull()
        {
            var config = new FibCliConfiguration
            {
                TargetTags = null
            };

            var containerizer = Containerizer.To(DockerDaemonImage.Named("target-image:tag3"));
            config.ConfigureContainerizer(containerizer);

            Assert.AreEqual(Array.Empty<string>(), containerizer.GetAdditionalTags());
        }

        [Test]
        public void TestConfigureContainerizer_SetsAllowInsecureRegistries()
        {
            var config = new FibCliConfiguration
            {
                AllowInsecureRegistries = true
            };

            var containerizer = Containerizer.To(DockerDaemonImage.Named("target-image:tag3"));
            config.ConfigureContainerizer(containerizer);

            Assert.AreEqual(true, containerizer.GetAllowInsecureRegistries());
        }

        [Test]
        public void TestConfigureContainerizer_SetsOfflineMode()
        {
            var config = new FibCliConfiguration
            {
                OfflineMode = true
            };

            var containerizer = Containerizer.To(DockerDaemonImage.Named("target-image:tag3"));
            config.ConfigureContainerizer(containerizer);

            Assert.AreEqual(true, containerizer.IsOfflineMode());
        }

        [Test]
        public void TestConfigureContainerizer_SetsApplicationLayersCache()
        {
            var config = new FibCliConfiguration
            {
                ApplicationLayersCacheDirectory = "AppCacheDirectory"
            };

            var containerizer = Containerizer.To(DockerDaemonImage.Named("target-image:tag3"));
            config.ConfigureContainerizer(containerizer);

            Assert.AreEqual("AppCacheDirectory", containerizer.GetApplicationLayersCacheDirectory());
        }

        [Test]
        public void TestConfigureContainerizer_SetsBaseImageLayersCache()
        {
            var config = new FibCliConfiguration
            {
                BaseLayersCacheDirectory = "BaseCacheDirectory"
            };

            var containerizer = Containerizer.To(RegistryImage.Named("target-image:tag3"));
            config.ConfigureContainerizer(containerizer);

            Assert.AreEqual("BaseCacheDirectory", containerizer.GetBaseImageLayersCacheDirectory());
        }

        [Test]
        public void TestGetContainerBuilder_BaseImage()
        {
            LayerConfiguration[] layerConfigurations = new[] { new LayerConfiguration("appLayer", ImmutableArray<LayerEntry>.Empty) };
            var cliConfiguration = new FibCliConfiguration
            {
                ImageFormat = ImageFormat.OCI,
                ImageLayers = layerConfigurations,
                BaseImage = "reg.io/base-image:tag",
                Entrypoint = new[] { "Entrypoint" },
                Cmd = new[] { "Program", "Arguments" },
                Environment = new Dictionary<string, string> { ["Var"] = "Value" },
                ImageWorkingDirectory = AbsoluteUnixPath.Get("/working/dir"),
                ImageUser = "user",
                Ports = new[] { Port.Tcp(5000) },
                Volumes = new[] { AbsoluteUnixPath.Get("/volume") },
                Labels = new Dictionary<string, string> { ["Label"] = "data"},
                ReproducableBuild = true
            };

            var builder = cliConfiguration.GetContainerBuilder();

            var configuration = builder.ToBuildConfiguration(Containerizer.To(DockerDaemonImage.Named("target-image")));

            Assert.AreEqual(ManifestFormat.OCI, configuration.GetTargetFormat());
            Assert.AreEqual(layerConfigurations, configuration.GetLayerConfigurations());

            ImageConfiguration imageConfiguration = configuration.GetBaseImageConfiguration();
            Assert.AreEqual("reg.io", imageConfiguration.GetImageRegistry());
            Assert.AreEqual("base-image", imageConfiguration.GetImageRepository());
            Assert.AreEqual("tag", imageConfiguration.GetImageTag());

            IContainerConfiguration containerConfiguration = configuration.GetContainerConfiguration();
            Assert.AreEqual(new[] { "Entrypoint" }, containerConfiguration.GetEntrypoint());
            Assert.AreEqual(new[] { "Program", "Arguments" }, containerConfiguration.GetProgramArguments());
            Assert.AreEqual(new Dictionary<string, string> { ["Var"] = "Value" }, containerConfiguration.GetEnvironmentMap());
            Assert.AreEqual(AbsoluteUnixPath.Get("/working/dir"), containerConfiguration.GetWorkingDirectory());
            Assert.AreEqual("user", containerConfiguration.GetUser());
            Assert.AreEqual(new[] { Port.Tcp(5000) }, containerConfiguration.GetExposedPorts());
            Assert.AreEqual(new[] { AbsoluteUnixPath.Get("/volume") }, containerConfiguration.GetVolumes());
            Assert.AreEqual(new Dictionary<string, string> { ["Label"] = "data" }, containerConfiguration.GetLabels());
            Assert.AreEqual(ContainerConfiguration.DefaultCreationTime, containerConfiguration.GetCreationTime());
        }
    }
}
