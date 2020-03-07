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

using Fib.Net.Test.Common;
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
                ["PublishProvider"] = "FibDotNet",
                ["FibPublishType"] = "Tar",
                ["FibOutputTarFile"] = TarFileName
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
                    ["PublishProvider"] = "FibDotNet",
                    ["FibPublishType"] = "Push",
                    ["FibTargetRegistry"] = "localhost:5000",
                    ["FibAllowInsecureRegistries"] = "True",
                    ["FibPort"] = "80"
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

                string imageReference = "localhost:5000/" + TestProjectName.ToLowerInvariant() + ":" + TestProjectVersion;
                Command.Run("docker", "pull", imageReference);
                string dockerPortsEnv = Command.Run("docker", "inspect", imageReference, "-f", "{{.Config.ExposedPorts}}");
                Assert.That(dockerPortsEnv, Does.Contain("80/tcp"));
                string dockerConfigEnv =
                    Command.Run("docker", "inspect", "-f", "{{.Config.Env}}", imageReference);
                Assert.That(dockerConfigEnv, Does.Contain("ASPNETCORE_URLS=http://+:80"));

                string history = Command.Run("docker", "history", imageReference);
                Assert.That(history, Does.Contain("Fib.Net.MSBuild"));

                Command.Run("docker", "image", "rm", imageReference);
            }
        }

        [Test]
        public async Task TestDeamonBuildAsync()
        {
            var properties = new Dictionary<string, string>
            {
                ["PublishProvider"] = "FibDotNet",
                ["FibPublishType"] = "Daemon",
                ["FibPort"] = "80"
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
            var tcs = new TaskCompletionSource<int>();
            p.Exited += (sender, args) => tcs.TrySetResult(p.ExitCode);
            if (!p.HasExited)
            {
                await tcs.Task;
            }

            TestContext.WriteLine(await stdOutTask);
            TestContext.WriteLine(await stdErrTask);

            Assert.AreEqual(0, p.ExitCode);
            string imageReference = TestProjectName.ToLowerInvariant() + ":" + TestProjectVersion;
            string dockerPortsEnv = Command.Run("docker", "inspect", imageReference, "-f", "{{.Config.ExposedPorts}}");
            Assert.That(dockerPortsEnv, Does.Contain("80/tcp"));
            string dockerConfigEnv =
                Command.Run("docker", "inspect", "-f", "{{.Config.Env}}", imageReference);
            Assert.That(dockerConfigEnv, Does.Contain("ASPNETCORE_URLS=http://+:80"));

            string history = Command.Run("docker", "history", imageReference);
            Assert.That(history, Does.Contain("Fib.Net.MSBuild"));

            Command.Run("docker", "image", "rm", imageReference);
        }
    }
}