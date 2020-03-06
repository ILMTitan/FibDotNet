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

namespace Fib.Net.Core.FileSystem
{
    /**
     * Obtains an OS-specific user cache directory based on the XDG Base Directory Specification.
     *
     * <p>Specifically, from the specification:
     *
     * <ul>
     *   <li>This directory is defined by the environment variable {@code $XDG_CACHE_HOME}.
     *   <li>If {@code $XDG_CACHE_HOME} is either not set or empty, a default equal to {@code
     *       $HOME/.cache} should be used.
     * </ul>
     *
     * @see <a
     *     href="https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html">https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html</a>
     */
    public static class UserCacheHome
    {
        /**
         * Returns {@code $XDG_CACHE_HOME}, if available, or resolves the OS-specific user cache home
         * based.
         *
         * <p>For Linus, this is {@code $HOME/.cache/}.
         *
         * <p>For Windows, this is {@code %LOCALAPPDATA%}.
         *
         * <p>For macOS, this is {@code $HOME/Library/Application Support/}.
         */

        public static SystemPath GetCacheHome(IEnvironment environment)
        {
            environment = environment ?? throw new ArgumentNullException(nameof(environment));
            // Use environment variable $XDG_CACHE_HOME if set and not empty.
            string xdgCacheHome = environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (!string.IsNullOrWhiteSpace(xdgCacheHome))
            {
                return Paths.Get(xdgCacheHome);
            }

            // Next, try using localAppData.
            string localAppData = environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                return Paths.Get(localAppData);
            }

            string userHome = environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (environment.IsOsx())
            {
                // Use '~/Library/Application Support/' for macOS.
                SystemPath applicationSupport = Paths.Get(userHome, "Library", "Application Support");
                if (Files.Exists(applicationSupport))
                {
                    return applicationSupport;
                }
            }

            if (!string.IsNullOrWhiteSpace(userHome))
            {
                return Paths.Get(userHome, ".cache");
            }

            throw new InvalidOperationException(Resources.UserCacheHomeMissingUserProfileExceptionMessage);
        }
    }
}
