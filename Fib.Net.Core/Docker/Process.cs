// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Core.Docker
{
    public class Process : IProcess
    {
        private readonly System.Diagnostics.Process process;

        public Process(System.Diagnostics.Process process)
        {
            this.process = process;
        }

        public Stream GetOutputStream()
        {
            return process.StandardInput.BaseStream;
        }

        public Stream GetErrorStream()
        {
            return process.StandardError.BaseStream;
        }

        public TextReader GetErrorReader()
        {
            return process.StandardError;
        }

        public Stream GetInputStream()
        {
            return process.StandardOutput.BaseStream;
        }

        public int WaitFor()
        {
            process.WaitForExit();
            return process.ExitCode;
        }

        public Task<int> WhenFinishedAsync()
        {
            process.EnableRaisingEvents = true;
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            process.Exited += (sender, args) => tcs.TrySetResult(process.ExitCode);
            if (process.HasExited)
            {
                return Task.FromResult(process.ExitCode);
            }
            else
            {
                return tcs.Task;
            }
        }
    }
}