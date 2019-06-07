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

namespace com.google.cloud.tools.jib.filesystem {









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
public class UserCacheHome {

  private static readonly Logger logger = Logger.getLogger(typeof(UserCacheHome).getName());

  public static Path getCacheHome() {
    return getCacheHome(System.getProperties(), System.getenv());
  }

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

  static Path getCacheHome(Properties properties, Map<string, string> environment) {
    // Use environment variable $XDG_CACHE_HOME if set and not empty.
    string xdgCacheHome = environment.get("XDG_CACHE_HOME");
    if (xdgCacheHome != null && !xdgCacheHome.trim().isEmpty()) {
      return Paths.get(xdgCacheHome);
    }

    string userHome = properties.getProperty("user.home");
    Path xdgPath = Paths.get(userHome, ".cache");

    string rawOsName = properties.getProperty("os.name");
    string osName = rawOsName.toLowerCase(Locale.ENGLISH);

    if (osName.contains("linux")) {
      return xdgPath;

    } else if (osName.contains("windows")) {
      // Use %LOCALAPPDATA% for Windows.
      string localAppDataEnv = environment.get("LOCALAPPDATA");
      if (localAppDataEnv == null || localAppDataEnv.trim().isEmpty()) {
        logger.warning("LOCALAPPDATA environment is invalid or missing");
        return xdgPath;
      }
      Path localAppData = Paths.get(localAppDataEnv);
      if (!Files.exists(localAppData)) {
        logger.warning(localAppData + " does not exist");
        return xdgPath;
      }
      return localAppData;

    } else if (osName.contains("mac") || osName.contains("darwin")) {
      // Use '~/Library/Application Support/' for macOS.
      Path applicationSupport = Paths.get(userHome, "Library", "Application Support");
      if (!Files.exists(applicationSupport)) {
        logger.warning(applicationSupport + " does not exist");
        return xdgPath;
      }
      return applicationSupport;
    }

    throw new IllegalStateException("Unknown OS: " + rawOsName);
  }

  private UserCacheHome() {}
}
}
