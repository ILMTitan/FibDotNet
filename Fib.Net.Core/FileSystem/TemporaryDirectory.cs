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

using Fib.Net.Core.Api;
using System;
using System.Diagnostics;
using System.IO;

namespace Fib.Net.Core.FileSystem
{
    /**
     * A temporary directory that tries to delete itself upon close. Note that deletion is <b>NOT</b>
     * guaranteed.
     */
    public sealed class TemporaryDirectory : IDisposable
    {
        private readonly SystemPath path;

        /**
         * Creates a new temporary directory under an existing {@code parentDirectory}.
         *
         * @param parentDirectory the directory to create the temporary directory within
         * @throws IOException if an I/O exception occurs
         */
        public TemporaryDirectory(SystemPath parentDirectory)
        {
            parentDirectory = parentDirectory ?? throw new ArgumentNullException(nameof(parentDirectory));
            path = Files.CreateTempDirectory(parentDirectory, null);
        }

        /**
         * Creates a new temporary directory under an existing {@code parentDirectory}.
         *
         * @param parentDirectory the directory to create the temporary directory within
         * @throws IOException if an I/O exception occurs
         */
        public TemporaryDirectory(string parentDirectory)
        {
            path = Files.CreateTempDirectory(parentDirectory, null);
        }

        /**
         * Gets the temporary directory.
         *
         * @return the temporary directory.
         */
        public SystemPath GetDirectory()
        {
            return path;
        }

        public void Dispose()
        {
            if (Files.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"Error deleting temporary directory {path}: {e}");
                    // TODO log error; deletion is best-effort
                }
            }
        }

        internal void MoveIfDoesNotExist(SystemPath destination)
        {
            if (Directory.Exists(destination) || File.Exists(destination))
            {
                return;
            }
            try
            {
                Directory.Move(path, destination);
            }
            catch (IOException)
            {
                if (!Directory.Exists(destination))
                {
                    throw;
                }
            }
        }
    }
}
