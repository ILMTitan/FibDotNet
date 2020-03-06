// Copyright 2018 Google LLC. All rights reserved.
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
using System.Diagnostics;
using System.IO;

namespace Fib.Net.Core.FileSystem
{
    public sealed class TemporaryFile : IDisposable
    {
        public SystemPath Path { get; }

        public TemporaryFile(SystemPath path)
        {
            Path = path;
        }

        public TemporaryFile()
        {
            Path = new SystemPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName()));
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
            catch (IOException)
            {
                if (File.Exists(Path))
                {
                    Debug.WriteLine($"failed to delete temporary file {Path}");
                }
            }
        }

        internal void MoveIfDoesNotExist(SystemPath destination)
        {
            // If the file already exists, we skip renaming and use the existing file. This happens if a
            // new layer happens to have the same content as a previously-cached layer.
            if (File.Exists(destination))
            {
                return;
            }

            try
            {
                File.Move(Path, destination);
            }
            catch (IOException)
            {
                if (!File.Exists(destination))
                {
                    // TODO to log that the destination exists
                    throw;
                }
            }
        }
    }
}