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

using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace com.google.cloud.tools.jib.filesystem
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

        public static SystemPath getCacheHome()
        {
            // Use environment variable $XDG_CACHE_HOME if set and not empty.
            string xdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (!string.IsNullOrWhiteSpace(xdgCacheHome))
            {
                return Paths.get(xdgCacheHome);
            }

            // Next, try using localAppData.
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                return Paths.get(localAppData);
            }
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Use '~/Library/Application Support/' for macOS.
                SystemPath applicationSupport = Paths.get(userHome, "Library", "Application Support");
                if (Files.exists(applicationSupport))
                {
                    return applicationSupport;
                }
            }
            if (!string.IsNullOrWhiteSpace(userHome))
            {
                SystemPath xdgPath = Paths.get(userHome, ".cache");
                return xdgPath;
            }

            throw new InvalidOperationException("Unknown OS: " + RuntimeInformation.OSDescription);
        }
    }
}