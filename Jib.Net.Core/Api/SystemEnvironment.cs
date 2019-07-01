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
using System.Runtime.InteropServices;
using com.google.cloud.tools.jib.filesystem;

namespace Jib.Net.Core.Api
{
    internal sealed class SystemEnvironment : IEnvironment
    {
        public static SystemEnvironment Instance { get; } = new SystemEnvironment();

        private SystemEnvironment() { }

        public string GetEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName);
        }

        public string GetFolderPath(Environment.SpecialFolder folder)
        {
            return Environment.GetFolderPath(folder);
        }
#if NETSTANDARD2_0
        public bool IsOSPlatform(OSPlatform osPlatform)
        {
            return RuntimeInformation.IsOSPlatform(osPlatform);
        }
#endif
    }
}