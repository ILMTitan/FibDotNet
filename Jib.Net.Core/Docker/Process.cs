/*
 * Copyright 2018 Google LLC.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

using System;
using System.Diagnostics;
using System.IO;

namespace com.google.cloud.tools.jib.docker
{
    public class Process : IProcess
    {
        private System.Diagnostics.Process process;

        public Process(System.Diagnostics.Process process)
        {
            this.process = process;
        }

        public Stream getOutputStream()
        {
            return process.StandardInput.BaseStream;
        }

        public Stream getErrorStream()
        {
            return process.StandardError.BaseStream;
        }

        public TextReader GetErrorReader()
        {
            return process.StandardError;
        }

        public Stream getInputStream()
        {
            return process.StandardOutput.BaseStream;
        }

        public int waitFor()
        {
            process.WaitForExit();
            return process.ExitCode;
        }
    }
}