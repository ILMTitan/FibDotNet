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

using Fib.Net.Core.FileSystem;
using System;
using System.Runtime.Serialization;

namespace Fib.Net.Core.Caching
{
    /** Thrown if the the cache was found to be corrupted. */
    [Serializable]
    public class CacheCorruptedException : Exception
    {
        public CacheCorruptedException(SystemPath cacheDirectory, string message, Exception cause)
                  : base(
                        $"{message}. " +
                        $"You may need to clear the cache by deleting the '{cacheDirectory}' directory " +
                        $"(if this is a bug, please file an issue at {ProjectInfo.GitHubNewIssueUrl})",
                        cause)
        {
        }

        public CacheCorruptedException(SystemPath cacheDirectory, string message) : base(
              message
                  + ". You may need to clear the cache by deleting the '"
                  + cacheDirectory
                  + "' directory (if this is a bug, please file an issue at "
                  + ProjectInfo.GitHubNewIssueUrl
                  + ")")
        {
        }

        protected CacheCorruptedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
