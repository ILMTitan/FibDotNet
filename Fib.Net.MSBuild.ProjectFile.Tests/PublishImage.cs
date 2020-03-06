// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Fib.Net.MSBuild.ProjectFile.Tests
{
    public class PublishImage : Task
    {
        [Required]
        public string PublishType { get; set; }

        [Required]
        public string BaseImage { get; set; }

        [Required]
        public string TargetImage { get; set; }

        public ITaskItem[] TargetTags { get; set; }
        public string OutputTarFile { get; set; }

        public ITaskItem[] ImageFiles { get; set; }

        public string Entrypoint { get; set; }
        public string Cmd { get; set; }
        public string ImageWorkingDirectory { get; set; }
        public string ImageUser { get; set; }
        public ITaskItem[] Environment { get; set; }
        public ITaskItem[] Ports { get; set; }
        public ITaskItem[] Volumes { get; set; }
        public ITaskItem[] Labels { get; set; }

        public bool AllowInsecureRegistries { get; set; }
        public bool OfflineMode { get; set; }
        public string ApplicationLayersCacheDirectory { get; set; }
        public string BaseLayersCacheDirectory { get; set; }
        public bool ReproducableBuild { get; set; }
        public string ImageFormat { get; set; } = "Docker";

        [Required]
        public string CliExecutablePath { get; set; }

        [Output]
        public string ImageId { get; set; }

        [Output]
        public string ImageDigest { get; set; }

        public static event Action<PublishImage> OnExecute = _ => { };

        public override bool Execute()
        {
            OnExecute(this);
            return true;
        }
    }

    /** Indicates the format of the image. */
    public enum ImageFormat
    {

        /** @see <a href="https://docs.docker.com/registry/spec/manifest-v2-2/">Docker V2.2</a> */
        Docker,

        /** @see <a href="https://github.com/opencontainers/image-spec/blob/master/manifest.md">OCI</a> */
        OCI
    }
}
