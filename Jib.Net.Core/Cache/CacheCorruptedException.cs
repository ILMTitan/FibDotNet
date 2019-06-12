/*
 * Copyright 2018 Google LLC. All rights reserved.
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

namespace com.google.cloud.tools.jib.cache
{
    /** Thrown if the the cache was found to be corrupted. */
    public class CacheCorruptedException : Exception
    {
        public CacheCorruptedException(SystemPath cacheDirectory, string message, Exception cause)
                  : base(
                        $"{message}. " +
                        $"You may need to clear the cache by deleting the '{cacheDirectory}' directory " +
                        $"(if this is a bug, please file an issue at {ProjectInfo.GITHUB_NEW_ISSUE_URL})",
                        cause)
        {
        }

        public CacheCorruptedException(SystemPath cacheDirectory, string message) : base(
              message
                  + ". You may need to clear the cache by deleting the '"
                  + cacheDirectory
                  + "' directory (if this is a bug, please file an issue at "
                  + ProjectInfo.GITHUB_NEW_ISSUE_URL
                  + ")")
        {
        }

        public CacheCorruptedException() : base()
        {
        }

        public CacheCorruptedException(string message) : base(message)
        {
        }

        public CacheCorruptedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}