// Copyright 2019 Google LLC.
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
using System.IO;

namespace Fib.Net.Core.FileSystem
{
    /** Creates and deletes lock files. */
    public sealed class LockFile : IDisposable
    {
        private readonly Stream outputStream;

        private LockFile(Stream outputStream)
        {
            this.outputStream = outputStream;
        }

        /**
         * Creates a lock file.
         *
         * @param lockFile the path of the lock file
         * @return a new {@link LockFile} that can be released later
         * @throws IOException if creating the lock file fails
         */
        public static LockFile Create(SystemPath lockFile)
        {
            lockFile = lockFile ?? throw new ArgumentNullException(nameof(lockFile));
            Files.CreateDirectories(lockFile.GetParent());
            return new LockFile(lockFile.ToFile().Create());
        }

        /** Releases the lock file. */

        public void Dispose()
        {
            outputStream.Dispose();
        }
    }
}
