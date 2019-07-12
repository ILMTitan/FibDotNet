using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Jib.Net.MSBuild.ProjectFile.Tests
{
    public class TargetsTest
    {
        private const string existingFilePath = "build/path/ExistingFile.txt";
        private ProjectInstance _projectInstance;

        private static IEnumerable<ILogger> Loggers { get; } = new[] { new ConsoleLogger(LoggerVerbosity.Normal, s => TestContext.Out.WriteLine(s), _ => { }, () => { }) };

        [SetUp]
        public void Setup()
        {
            string tasksFile = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "build",
                "Jib.Net.MSBuild.targets");
            BuildManager.DefaultBuildManager.ResetCaches();
            Project project =
                ProjectCollection.GlobalProjectCollection.LoadProject(
                    tasksFile,
                    new Dictionary<string, string>
                    {
                        ["JibDotNetPublishTaskAssembly"] =
                            Path.Combine(
                                TestContext.CurrentContext.TestDirectory,
                                "Jib.Net.MsBuild.ProjectFile.Tests.dll")
                    },
                    null);
            _projectInstance = BuildManager.DefaultBuildManager.GetProjectInstanceForBuild(project);
            _projectInstance.SetProperty("JibDotNetPublishTaskAssembly", typeof(TargetsTest).Assembly.Location);
        }

        [Test]
        [TestCase("my-registry.io", "my-repository", "", "my-sha", "my-registry.io/my-repository@my-sha")]
        [TestCase("my-registry.io", "my-repository", "my-tag", "my-sha", "my-registry.io/my-repository@my-sha")]
        [TestCase("my-registry.io", "my-repository", "my-tag", "", "my-registry.io/my-repository:my-tag")]
        [TestCase("my-registry.io", "my-repository", "", "", "my-registry.io/my-repository")]
        [TestCase("my-registry.io", "", "my-tag", "my-sha", "my-registry.io@my-sha")]
        [TestCase("my-registry.io", "", "", "my-sha", "my-registry.io@my-sha")]
        [TestCase("my-registry.io", "", "my-tag", "", "my-registry.io:my-tag")]
        [TestCase("my-registry.io", "", "", "", "my-registry.io")]
        [TestCase("", "my-repository", "", "my-sha", "my-repository@my-sha")]
        [TestCase("", "my-repository", "my-tag", "my-sha", "my-repository@my-sha")]
        [TestCase("", "my-repository", "my-tag", "", "my-repository:my-tag")]
        [TestCase("", "my-repository", "", "", "my-repository")]
        [TestCase("", "", "", "my-sha", "")]
        [TestCase("", "", "my-tag", "my-sha", "")]
        [TestCase("", "", "my-tag", "", "")]
        [TestCase("", "", "", "", "")]
        public void TestSetJibBaseImage(string registry, string repository, string tag, string digest, string expectedJibBaseImage)
        {
            _projectInstance.SetProperty("JibBaseRegistry", registry);
            _projectInstance.SetProperty("JibBaseRepository", repository);
            _projectInstance.SetProperty("JibBaseTag", tag);
            _projectInstance.SetProperty("JibBaseDigest", digest);

            Assert.IsTrue(_projectInstance.Build("SetJibBaseImage", Loggers));

            Assert.AreEqual(expectedJibBaseImage, _projectInstance.GetPropertyValue("JibBaseImage"));
        }

        [Test]
        [TestCase("my-registry.io", "my-repository", "", "my-sha")]
        [TestCase("my-registry.io", "my-repository", "my-tag", "my-sha")]
        [TestCase("my-registry.io", "my-repository", "my-tag", "")]
        [TestCase("my-registry.io", "my-repository", "", "")]
        [TestCase("my-registry.io", "", "my-tag", "my-sha")]
        [TestCase("my-registry.io", "", "", "my-sha")]
        [TestCase("my-registry.io", "", "my-tag", "")]
        [TestCase("my-registry.io", "", "", "")]
        [TestCase("", "my-repository", "", "my-sha")]
        [TestCase("", "my-repository", "my-tag", "my-sha")]
        [TestCase("", "my-repository", "my-tag", "")]
        [TestCase("", "my-repository", "", "")]
        [TestCase("", "", "", "my-sha")]
        [TestCase("", "", "my-tag", "my-sha")]
        [TestCase("", "", "my-tag", "")]
        [TestCase("", "", "", "")]
        public void TestSetJibBaseImage_DoesNotOverrideExisting(string registry, string repository, string tag, string digest)
        {
            _projectInstance.SetProperty("JibBaseRegistry", registry);
            _projectInstance.SetProperty("JibBaseRepository", repository);
            _projectInstance.SetProperty("JibBaseTag", tag);
            _projectInstance.SetProperty("JibBaseDigest", digest);
            _projectInstance.SetProperty("JibBaseImage", "expectedImageName");

            Assert.IsTrue(_projectInstance.Build("SetJibBaseImage", Loggers));

            Assert.AreEqual("expectedImageName", _projectInstance.GetPropertyValue("JibBaseImage"));
        }

        [Test]
        [TestCase("my-registry.io", "my-repository", "my-registry.io/my-repository")]
        [TestCase("my-registry.io", "", "my-registry.io")]
        [TestCase("", "my-repository", "my-repository")]
        [TestCase("", "", "")]
        public void TestSetJibTargetImage(string registry, string repository, string expectedJibTargetImage)
        {
            _projectInstance.SetProperty("JibTargetRegistry", registry);
            _projectInstance.SetProperty("JibTargetRepository", repository);

            Assert.IsTrue(_projectInstance.Build("SetJibTargetImage", Loggers));

            Assert.AreEqual(expectedJibTargetImage, _projectInstance.GetPropertyValue("JibTargetImage"));
        }

        [Test]
        [TestCase("my-registry.io", "my-repository")]
        [TestCase("my-registry.io", "")]
        [TestCase("", "my-repository")]
        [TestCase("", "")]
        public void TestSetJibTargetImage_DoesNotOverrideExisting(string registry, string repository)
        {
            _projectInstance.SetProperty("JibTargetImage", "expectedTargetImage");
            _projectInstance.SetProperty("JibTargetRegistry", registry);
            _projectInstance.SetProperty("JibTargetRepository", repository);

            Assert.IsTrue(_projectInstance.Build("SetJibTargetImage", Loggers));

            Assert.AreEqual("expectedTargetImage", _projectInstance.GetPropertyValue("JibTargetImage"));
        }

        [Test]
        public void TestSetJibTargetTag()
        {
            _projectInstance.AddItem("JibTargetTag", "tag1");
            _projectInstance.AddItem("JibTargetTag", "tag2");
            _projectInstance.SetProperty("JibTargetTag", "tag3;tag4");

            Assert.IsTrue(_projectInstance.Build("SetJibTargetTag", Loggers));

            CollectionAssert.AreEquivalent(
                new[] { "tag1", "tag2", "tag3", "tag4" },
                _projectInstance.GetItems("JibTargetTag").Select(t => t.EvaluatedInclude));
        }

        [Test]
        public void TestGatherImageFiles_FromIntermediateAssembly()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.AddItem("IntermediateAssembly", "build/path/MyAssembly.dll");
            _projectInstance.AddItem("JibImageFile", existingFilePath,
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });
            _projectInstance.AddItem("IntermediateAssembly", existingFilePath);

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");
            Assert.AreEqual(2, imageFiles.Count);

            var existingFile = imageFiles.Single(i => i.EvaluatedInclude == existingFilePath);
            Assert.AreEqual("OriginalTargetPath", existingFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", existingFile.GetMetadataValue("Layer"));

            var imageFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/MyAssembly.dll");
            Assert.AreEqual("/base/path/MyAssembly.dll", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("Binary", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromProjectDeps()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.SetProperty("ProjectDepsFileName", "MyProject.deps.json");
            _projectInstance.SetProperty("ProjectDepsFilePath", "build/path/MyProject.deps.json");

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");

            var imageFile = imageFiles.Single();

            Assert.AreEqual("build/path/MyProject.deps.json", imageFile.EvaluatedInclude);
            Assert.AreEqual("/base/path/MyProject.deps.json", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("Binary", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromProjectDeps_DoesNotOverrideExisting()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.SetProperty("ProjectDepsFileName", "MyProject.deps.json");
            _projectInstance.SetProperty("ProjectDepsFilePath", "build/path/MyProject.deps.json");
            _projectInstance.AddItem("JibImageFile", "build/path/MyProject.deps.json",
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");

            var imageFile = imageFiles.Single();

            Assert.AreEqual("build/path/MyProject.deps.json", imageFile.EvaluatedInclude);
            Assert.AreEqual("OriginalTargetPath", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromRuntimeConfig()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.SetProperty("ProjectRuntimeConfigFileName", "MyProject.runtimeconfig.json");
            _projectInstance.SetProperty("ProjectRuntimeConfigFilePath", "build/path/MyProject.runtimeconfig.json");

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");

            var imageFile = imageFiles.Single();

            Assert.AreEqual("build/path/MyProject.runtimeconfig.json", imageFile.EvaluatedInclude);
            Assert.AreEqual("/base/path/MyProject.runtimeconfig.json", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("Binary", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromRuntimeConfig_DoesNotOverrideExisting()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.SetProperty("ProjectRuntimeConfigFileName", "MyProject.runtimeconfig.json");
            _projectInstance.SetProperty("ProjectRuntimeConfigFilePath", "build/path/MyProject.runtimeconfig.json");
            _projectInstance.AddItem("JibImageFile", "build/path/MyProject.runtimeconfig.json",
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");

            var imageFile = imageFiles.Single();

            Assert.AreEqual("build/path/MyProject.runtimeconfig.json", imageFile.EvaluatedInclude);
            Assert.AreEqual("OriginalTargetPath", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromAppConfig()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.AddItem("JibImageFile", existingFilePath,
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });
            _projectInstance.AddItem(
                "AppConfigWithTargetPath",
                "build/path/MyProject.appconfig.json",
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "target/path/MyProject.appconfig.json"
                });
            _projectInstance.AddItem(
                "AppConfigWithTargetPath",
                existingFilePath,
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "target/path/MyProject.appconfig.json"
                });

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");
            Assert.AreEqual(2, imageFiles.Count);

            var existingFile = imageFiles.Single(i => i.EvaluatedInclude == existingFilePath);
            Assert.AreEqual("OriginalTargetPath", existingFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", existingFile.GetMetadataValue("Layer"));

            var imageFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/MyProject.appconfig.json");
            Assert.AreEqual("/base/path/target/path/MyProject.appconfig.json", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("Binary", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromRazorAssembly()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.AddItem("JibImageFile", existingFilePath,
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });
            _projectInstance.AddItem("RazorIntermediateAssembly", "build/path/MyProject.Views.dll");
            _projectInstance.AddItem("RazorIntermediateAssembly", existingFilePath);

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");
            Assert.AreEqual(2, imageFiles.Count);

            var existingFile = imageFiles.Single(i => i.EvaluatedInclude == existingFilePath);
            Assert.AreEqual("OriginalTargetPath", existingFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", existingFile.GetMetadataValue("Layer"));

            var imageFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/MyProject.Views.dll");
            Assert.AreEqual("/base/path/MyProject.Views.dll", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("Binary", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromDebugSymbols()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.SetProperty("CopyOutputSymbolsToPublishDirectory", "True");
            _projectInstance.AddItem("JibImageFile", existingFilePath,
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });
            _projectInstance.AddItem("_DebugSymbolsIntermediatePath", "build/path/MyProject.pdb");
            _projectInstance.AddItem("_DebugSymbolsIntermediatePath", existingFilePath);

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");
            Assert.AreEqual(2, imageFiles.Count);

            var existingFile = imageFiles.Single(i => i.EvaluatedInclude == existingFilePath);
            Assert.AreEqual("OriginalTargetPath", existingFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", existingFile.GetMetadataValue("Layer"));

            var imageFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/MyProject.pdb");
            Assert.AreEqual("/base/path/MyProject.pdb", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("Debug", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromDocFile()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.SetProperty("PublishDocumentationFile", "True");
            _projectInstance.AddItem("JibImageFile", existingFilePath,
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });
            _projectInstance.AddItem("FinalDocFile", "build/path/MyProject.Doc.xml");
            _projectInstance.AddItem("FinalDocFile", existingFilePath);

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");
            Assert.AreEqual(2, imageFiles.Count);

            var existingFile = imageFiles.Single(i => i.EvaluatedInclude == existingFilePath);
            Assert.AreEqual("OriginalTargetPath", existingFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", existingFile.GetMetadataValue("Layer"));

            var imageFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/MyProject.Doc.xml");
            Assert.AreEqual("/base/path/MyProject.Doc.xml", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("Debug", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromRazorDebugSymbols()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.SetProperty("CopyOutputSymbolsToPublishDirectory", "True");
            _projectInstance.AddItem("JibImageFile", existingFilePath,
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });
            _projectInstance.AddItem("_RazorDebugSymbolsIntermediatePath", "build/path/MyProject.Views.pdb");
            _projectInstance.AddItem("_RazorDebugSymbolsIntermediatePath", existingFilePath);

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");
            Assert.AreEqual(2, imageFiles.Count);

            var existingFile = imageFiles.Single(i => i.EvaluatedInclude == existingFilePath);
            Assert.AreEqual("OriginalTargetPath", existingFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", existingFile.GetMetadataValue("Layer"));

            var imageFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/MyProject.Views.pdb");
            Assert.AreEqual("/base/path/MyProject.Views.pdb", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("Debug", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromSatelliteAssemblies()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.AddItem("JibImageFile", existingFilePath,
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });
            _projectInstance.AddItem(
                "IntermediateSatelliteAssembliesWithTargetPath",
                existingFilePath,
                new Dictionary<string, string>
                {
                    ["Culture"] = "en-US"
                });
            _projectInstance.AddItem(
                "IntermediateSatelliteAssembliesWithTargetPath",
                "build/path/MyProject.Resources.dll",
                new Dictionary<string, string>
                {
                    ["Culture"] = "en-US"
                });

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");

            Assert.AreEqual(2, imageFiles.Count);

            var existingFile = imageFiles.Single(i => i.EvaluatedInclude == existingFilePath);
            Assert.AreEqual("OriginalTargetPath", existingFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", existingFile.GetMetadataValue("Layer"));

            var imageFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/MyProject.Resources.dll");
            Assert.AreEqual("/base/path/en-US/MyProject.Resources.dll", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("Resources", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromLocalPublishAssets()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.AddItem("JibImageFile", existingFilePath,
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });
            _projectInstance.AddItem(
                "_ResolvedCopyLocalPublishAssets",
                "build/path/MyProject.CopyLocal.txt",
                new Dictionary<string, string>
                {
                    ["DestinationSubDirectory"] = "subdirectory/"
                });
            _projectInstance.AddItem(
                "_ResolvedCopyLocalPublishAssets",
                existingFilePath,
                new Dictionary<string, string>
                {
                    ["DestinationSubDirectory"] = "subdirectory/"
                });

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");

            Assert.AreEqual(2, imageFiles.Count);

            var existingFile = imageFiles.Single(i => i.EvaluatedInclude == existingFilePath);
            Assert.AreEqual("OriginalTargetPath", existingFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", existingFile.GetMetadataValue("Layer"));

            var imageFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/MyProject.CopyLocal.txt");
            Assert.AreEqual("/base/path/subdirectory/MyProject.CopyLocal.txt", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OtherFiles", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromDotNetPublishFiles()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.AddItem("JibImageFile", existingFilePath,
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });
            _projectInstance.AddItem(
                "DotNetPublishFiles",
                "build/path/MyProject.DotNetPublishFile.txt",
                new Dictionary<string, string>
                {
                    ["DestinationRelativePath"] = "subdirectory/MyProject.DotNetPublishFile.txt"
                });
            _projectInstance.AddItem(
                "DotNetPublishFiles",
                existingFilePath,
                new Dictionary<string, string>
                {
                    ["DestinationRelativePath"] = "subdirectory/ExistingFile.txt"
                });

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");
            Assert.AreEqual(2, imageFiles.Count);

            var existingFile = imageFiles.Single(i => i.EvaluatedInclude == existingFilePath);
            Assert.AreEqual("OriginalTargetPath", existingFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", existingFile.GetMetadataValue("Layer"));

            var imageFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/MyProject.DotNetPublishFile.txt");
            Assert.AreEqual("/base/path/subdirectory/MyProject.DotNetPublishFile.txt", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OtherFiles", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromEfSqlScripts()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.SetProperty("EFSQLScriptsFolderName", "scripts");
            _projectInstance.AddItem("JibImageFile", existingFilePath,
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });
            _projectInstance.AddItem(
                "_EFSQLScripts",
                "build/path/MyProject.Script.sql");
            _projectInstance.AddItem(
                "_EFSQLScripts",
                existingFilePath);

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");
            Assert.AreEqual(2, imageFiles.Count);

            var existingFile = imageFiles.Single(i => i.EvaluatedInclude == existingFilePath);
            Assert.AreEqual("OriginalTargetPath", existingFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", existingFile.GetMetadataValue("Layer"));

            var imageFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/MyProject.Script.sql");
            Assert.AreEqual("/base/path/scripts/MyProject.Script.sql", imageFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OtherFiles", imageFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestGatherImageFiles_FromResolvedFileToPublish()
        {
            _projectInstance.SetProperty("JibAppBasePath", "/base/path/");
            _projectInstance.AddItem("JibImageFile", "build/path/ExistingFile.dll",
                new Dictionary<string, string>
                {
                    ["TargetPath"] = "OriginalTargetPath",
                    ["Layer"] = "OriginalLayer"
                });
            _projectInstance.AddItem(
                "ResolvedFileToPublish",
                "build/path/RandomTextFile.txt",
                new Dictionary<string, string>
                {
                    ["RelativePath"] = "relative/path/RandomTextFile.txt"
                });
            _projectInstance.AddItem(
                "ResolvedFileToPublish",
                "build/path/RandomDllFile.dll",
                new Dictionary<string, string>
                {
                    ["RelativePath"] = "relative/path/RandomDllFile.dll"
                });
            _projectInstance.AddItem(
                "ResolvedFileToPublish",
                "build/path/ExistingFile.dll",
                new Dictionary<string, string>
                {
                    ["RelativePath"] = "relative/path/Existing.dll"
                });

            Assert.IsTrue(_projectInstance.Build("GatherImageFiles", Loggers));

            var imageFiles = _projectInstance.GetItems("JibImageFile");
            Assert.AreEqual(3, imageFiles.Count);
            var existingFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/ExistingFile.dll");
            Assert.AreEqual("OriginalTargetPath", existingFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OriginalLayer", existingFile.GetMetadataValue("Layer"));

            var randomTextFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/RandomTextFile.txt");
            Assert.AreEqual("/base/path/relative/path/RandomTextFile.txt", randomTextFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("OtherFiles", randomTextFile.GetMetadataValue("Layer"));

            var randomDllFile = imageFiles.Single(i => i.EvaluatedInclude == "build/path/RandomDllFile.dll");
            Assert.AreEqual("/base/path/relative/path/RandomDllFile.dll", randomDllFile.GetMetadataValue("TargetPath"));
            Assert.AreEqual("References", randomDllFile.GetMetadataValue("Layer"));
        }

        [Test]
        public void TestJibPublish_FailsMissingJibBaseImage()
        {
            _projectInstance.SetProperty("JibPublishType", "Docker");
            _projectInstance.SetProperty("JibTargetImage", "target/image:tag");

            Assert.IsFalse(_projectInstance.Build("JibPublish", Loggers));
        }

        [Test]
        public void TestJibPublish_FailsMissingJibTargetImage()
        {
            _projectInstance.SetProperty("JibPublishType", "Docker");
            _projectInstance.SetProperty("JibBaseImage", "base/image:tag");

            Assert.IsFalse(_projectInstance.Build("JibPublish", Loggers));
        }

        [Test]
        public void TestJibPublish_FailsMissingJibPublishType()
        {
            _projectInstance.SetProperty("JibBaseImage", "base/image:tag");
            _projectInstance.SetProperty("JibTargetImage", "target/image:tag");

            Assert.IsFalse(_projectInstance.Build("JibPublish", Loggers));
        }

        [Test]
        public void TestJibPublish_CallsTask()
        {
            bool called = false;
            Action<PublishImage> onExecute = _ => called = true;
            PublishImage.OnExecute += onExecute;
            
            _projectInstance.SetProperty("JibPublishType", "Docker");
            _projectInstance.SetProperty("JibBaseImage", "base/image:tag");
            _projectInstance.SetProperty("JibTargetImage", "target/image:tag");

            Assert.IsTrue(_projectInstance.Build("JibPublish", Loggers));

            Assert.IsTrue(called);

            PublishImage.OnExecute -= onExecute;
        }

        [Test]
        public void TestJibPublish_SetsInputs()
        {
            const string baseImage = "base/image:tag";
            const string targetImage = "target/image:tag";
            const string outputTarFile = "target/tar.file";
            const string entrypoint = "entrypoint";
            const string cmd = "cmd";
            const string workingDirectory = "working/directory";
            const string user = "user";
            const string appLayersCache = "app/layers/cache";
            const string baseLayersCache = "base/layers/cache";

            _projectInstance.SetProperty("JibPublishType", "Registry");
            _projectInstance.SetProperty("JibBaseImage", baseImage);
            _projectInstance.SetProperty("JibTargetImage", targetImage);
            _projectInstance.SetProperty("JibTargetTag", "tag1;tag2");
            _projectInstance.SetProperty("JibOutputTarFile", outputTarFile);
            _projectInstance.AddItem("JibImageFile", "File1");
            _projectInstance.AddItem("JibImageFile", "File2");
            _projectInstance.SetProperty("JibEntrypoint", entrypoint);
            _projectInstance.SetProperty("JibCmd", cmd);
            _projectInstance.AddItem("JibEnvironment", "Name1=Value1");
            _projectInstance.AddItem("JibEnvironment", "Name2=Value2");
            _projectInstance.SetProperty("JibImageWorkingDirectory", workingDirectory);
            _projectInstance.SetProperty("JibImageUser", user);
            _projectInstance.AddItem("JibPort", "5000");
            _projectInstance.AddItem("JibPort", "udp:5001");
            _projectInstance.AddItem("JibVolume", "folder/1/");
            _projectInstance.AddItem("JibVolume", "folder/2/");
            _projectInstance.AddItem("JibLabel", "Label1=Value1");
            _projectInstance.AddItem("JibLabel", "Label2=Value2");
            _projectInstance.SetProperty("JibAllowInsecureRegistries", "true");
            _projectInstance.SetProperty("JibOfflineMode", "true");
            _projectInstance.SetProperty("JibApplicationLayersCacheDirectory", appLayersCache);
            _projectInstance.SetProperty("JibBaseLayersCacheDirectory", baseLayersCache);
            _projectInstance.SetProperty("JibReproducableBuild", "false");
            _projectInstance.SetProperty("JibImageFormat", "OCI");

            PublishImage.OnExecute += verifyProperties;

            Assert.IsTrue(_projectInstance.Build("JibPublish", Loggers));

            PublishImage.OnExecute -= verifyProperties;

            void verifyProperties(PublishImage task)
            {
                Assert.AreEqual("Registry", task.PublishType);
                Assert.AreEqual(baseImage, task.BaseImage);
                Assert.AreEqual(targetImage, task.TargetImage);
                Assert.AreEqual(new[] { "tag1", "tag2" }, task.TargetTags.Select(i => i.ItemSpec));
                Assert.AreEqual(outputTarFile, task.OutputTarFile);
                Assert.AreEqual(new[] { "File1", "File2" }, task.ImageFiles.Select(i => i.ItemSpec));
                Assert.AreEqual(entrypoint, task.Entrypoint);
                Assert.AreEqual(cmd, task.Cmd);
                Assert.AreEqual(new[] { "Name1=Value1", "Name2=Value2" }, task.Environment.Select(i => i.ItemSpec));
                Assert.AreEqual(workingDirectory, task.ImageWorkingDirectory);
                Assert.AreEqual(user, task.ImageUser);
                Assert.AreEqual(new[] { "5000", "udp:5001" }, task.Ports.Select(i => i.ItemSpec));
                Assert.AreEqual(new[] { "folder/1/", "folder/2/" }, task.Volumes.Select(i => i.ItemSpec));
                Assert.AreEqual(new[] { "Label1=Value1", "Label2=Value2" }, task.Labels.Select(i => i.ItemSpec));
                Assert.IsTrue(task.AllowInsecureRegistries);
                Assert.IsTrue(task.OfflineMode);
                Assert.AreEqual(appLayersCache, task.ApplicationLayersCacheDirectory);
                Assert.AreEqual(baseLayersCache, task.BaseLayersCacheDirectory);
                Assert.IsFalse(task.ReproducableBuild);
                Assert.AreEqual("OCI", task.ImageFormat);
            }
        }
    }
}