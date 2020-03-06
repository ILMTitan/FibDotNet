
using Jib.Net.Test.Common;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Tests
{
    public class TargetIntegrationTest
    {
        private const string TarFileName = "TestTar.tar.gz";
        private const string TestProjectName = "RazorTestProject";
        private const string TestProjectVersion = "1.0.0-alpha1";

        [TearDown]
        public void DeleteTarFile()
        {
            File.Delete(Path.Combine(TestProjectName, TarFileName));
        }

        [Test]
        public async Task TestTarFileBuildAsync()
        {
            var properties = new Dictionary<string, string>
            {
                ["PublishProvider"] = "JibDotNet",
                ["JibPublishType"] = "Tar",
                ["JibOutputTarFile"] = TarFileName
            };
            string arguments = "publish " + TestProjectName + " -v:d "
                + string.Join(" ", properties.Select(kvp => $"-p:{kvp.Key}={kvp.Value}"));
            var p = Process.Start(
                new ProcessStartInfo("dotnet", arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                });
            p.EnableRaisingEvents = true;
            Task<string> stderrTask = p.StandardError.ReadToEndAsync();
            Task<string> stdOutTask = p.StandardOutput.ReadToEndAsync();
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            p.Exited += (sender, args) => tcs.TrySetResult(p.ExitCode);
            if (!p.HasExited)
            {
                await tcs.Task;
            }

            Assert.AreEqual(0, p.ExitCode);

            Assert.IsTrue(File.Exists(Path.Combine(TestProjectName, TarFileName)));
        }

        [Test]
        public async Task TestRegistryBuildAsync()
        {
            using (var localRegistry = new LocalRegistry(5000))
            {
                await localRegistry.StartAsync();
                var properties = new Dictionary<string, string>
                {
                    ["PublishProvider"] = "JibDotNet",
                    ["JibPublishType"] = "Push",
                    ["JibTargetRegistry"] = "localhost:5000",
                    ["JibAllowInsecureRegistries"] = "True"
                };
                string propertiesString = string.Join(" ", properties.Select(kvp => $"-p:{kvp.Key}={kvp.Value}"));
                string arguments = $"publish {TestProjectName} {propertiesString}";
                var p = Process.Start(
                    new ProcessStartInfo("dotnet", arguments)
                    {
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    });
                p.EnableRaisingEvents = true;
                var stdOutTask = p.StandardOutput.ReadToEndAsync();
                var stdErrTask = p.StandardError.ReadToEndAsync();
                var tcs = new TaskCompletionSource<int>();
                p.Exited += (sender, args) => tcs.TrySetResult(p.ExitCode);
                if (!p.HasExited)
                {
                    await tcs.Task;
                }

                TestContext.WriteLine(await stdOutTask);
                TestContext.WriteLine(await stdErrTask);

                Assert.AreEqual(0, p.ExitCode);
                new Command("docker", "pull", "localhost:5000/"+ TestProjectName.ToLowerInvariant() + ":" + TestProjectVersion).Run();
            }
        }

        [Test]
        public async Task TestDeamonBuildAsync()
        {
            var properties = new Dictionary<string, string>
            {
                ["PublishProvider"] = "JibDotNet",
                ["JibPublishType"] = "Daemon",
                ["JibTargetRegistry"] = "localhost:5000",
                ["JibAllowInsecureRegistries"] = "True",
                ["JibPort"] = "80"
            };
            string propertiesString = string.Join(" ", properties.Select(kvp => $"-p:{kvp.Key}={kvp.Value}"));
            string arguments = $"publish {TestProjectName} -v:d {propertiesString}";
            var p = Process.Start(
                new ProcessStartInfo("dotnet", arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                });
            p.EnableRaisingEvents = true;
            var stdOutTask = p.StandardOutput.ReadToEndAsync();
            var stdErrTask = p.StandardError.ReadToEndAsync();
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            p.Exited += (sender, args) => tcs.TrySetResult(p.ExitCode);
            if (!p.HasExited)
            {
                await tcs.Task;
            }

            TestContext.WriteLine(await stdOutTask);
            TestContext.WriteLine(await stdErrTask);

            Assert.AreEqual(0, p.ExitCode);
            string imageReference = "localhost:5000/" + TestProjectName.ToLowerInvariant() + ":" + TestProjectVersion;
            string dockerPortsEnv = new Command("docker", "inspect", imageReference, "-f", "{{.Config.ExposedPorts}}").Run();
            Assert.That(dockerPortsEnv, Does.Contain("80/tcp"));
            string dockerConfigEnv =
                new Command("docker", "inspect", "-f", "{{.Config.Env}}", imageReference).Run();
            Assert.That(dockerConfigEnv, Does.Contain("ASPNETCORE_URLS=http://+:80"));

            string history = new Command("docker", "history", imageReference).Run();
            Assert.That(history, Does.Contain("Jib.Net.MSBuild"));
        }
    }
}