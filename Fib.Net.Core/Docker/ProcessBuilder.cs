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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fib.Net.Core.Docker
{
    public class ProcessBuilder : IProcessBuilder
    {
        private readonly string _args;
        private readonly string _cmd;
        private readonly IDictionary<string, string> env;

        public ProcessBuilder(string command, string args) : this(command, args, new Dictionary<string, string>()) { }

        public ProcessBuilder(string command) : this(command, null) { }

        public ProcessBuilder(string command, string args, IDictionary<string, string> additonalEnv)
        {
            _cmd = command;
            _args = args;
            env = new Dictionary<string, string>();
            
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                env.Add((string)entry.Key, (string)entry.Value);
            }

            foreach ((string key, string value) in additonalEnv ?? Enumerable.Empty<KeyValuePair<string, string>>())
            {
                env[key] = value;
            }
        }

        internal IDictionary<string, string> GetEnvironment()
        {
            return env;
        }

        public IProcess Start()
        {
            var startInfo = new ProcessStartInfo(_cmd, _args)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };
            foreach (var kvp in env)
            {
                startInfo.Environment.Add(kvp.Key, kvp.Value);
            }
            return new Process(System.Diagnostics.Process.Start(startInfo));
        }

        public string Command()
        {
            if (string.IsNullOrWhiteSpace(_args))
            {
                return _cmd;
            }
            else
            {
                return $"{_cmd} {_args}";
            }
        }
    }
}