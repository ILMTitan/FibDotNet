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
using System.IO;

namespace Fib.Net.Test.Common
{
    public sealed class TemporaryFolder : IDisposable
    {
        private readonly DirectoryInfo directory;

        public TemporaryFolder()
        {
            directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        }

        public DirectoryInfo NewFolder()
        {
            return directory.CreateSubdirectory(Path.GetRandomFileName());
        }

        public FileInfo NewFile()
        {
            var fileInfo = new FileInfo(Path.Combine(directory.FullName, Path.GetRandomFileName()));
            fileInfo.Create().Dispose();
            return fileInfo;
        }

        public DirectoryInfo GetRoot()
        {
            return directory;
        }

        public DirectoryInfo NewFolder(string folderName)
        {
            return directory.CreateSubdirectory(folderName);
        }

        public void Dispose()
        {
            directory.Delete(true);
        }
    }
}