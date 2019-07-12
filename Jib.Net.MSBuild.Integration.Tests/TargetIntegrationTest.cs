
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Tests
{
    public class TargetIntegrationTest
    {


        [Test]
        public async Task TestTarFileBuildAsync()
        {
            var properties = new Dictionary<string, string>
            {
                ["JibPublishType"] = "Tar",
                ["PublishProvider"] = "JibDotNet",
                ["JibOutputTarFile"] = "TestTar.tar.gz"
            };
            string arguments = "publish RazorTestProject -v:d "
                + string.Join(" ", properties.Select(kvp => $"-p:{kvp.Key}={kvp.Value}"));
            var p = Process.Start(
                new ProcessStartInfo(
                    "dotnet",
                    arguments)
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

            Console.WriteLine(await stderrTask);
            Console.WriteLine(await stdOutTask);
            Assert.AreEqual(0, p.ExitCode);

            Assert.IsTrue(File.Exists("TestTar.tar.gz"));
        }
    }
}