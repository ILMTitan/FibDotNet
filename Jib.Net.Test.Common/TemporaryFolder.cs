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

using Jib.Net.Core.FileSystem;
using System;
using System.IO;

namespace com.google.cloud.tools.jib.builder.steps
{
    public class TemporaryFolder:IDisposable
    {
        private readonly DirectoryInfo directory;

        public TemporaryFolder()
        {
            directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        }

        public DirectoryInfo newFolder()
        {
            return directory.CreateSubdirectory(Path.GetRandomFileName());
        }

        public FileInfo newFile()
        { var fileInfo = new FileInfo(Path.Combine(directory.FullName, Path.GetRandomFileName()));
            fileInfo.Create().Dispose();
            return fileInfo;
        }

        public DirectoryInfo getRoot()
        {
            return directory;
        }

        public DirectoryInfo newFolder(string folderName)
        {
            return directory.CreateSubdirectory(folderName);
        }

        public void Dispose()
        {
            directory.Delete(true);
        }
    }
}