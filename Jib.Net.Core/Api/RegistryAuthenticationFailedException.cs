/*
 * Copyright 2017 Google LLC.
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

namespace com.google.cloud.tools.jib.api {


/** Thrown because registry authentication failed. */
public class RegistryAuthenticationFailedException : RegistryException {

  private static readonly string REASON = "Failed to authenticate with registry {0}/{1} because: {2}";
  private readonly string serverUrl;
  private readonly string imageName;

  public RegistryAuthenticationFailedException(
      string serverUrl, string imageName, Throwable cause) : base(MessageFormat.format(REASON, serverUrl, imageName, cause.getMessage()), cause) {
    
    this.serverUrl = serverUrl;
    this.imageName = imageName;
  }

  public RegistryAuthenticationFailedException(string serverUrl, string imageName, string reason) : base(MessageFormat.format(REASON, serverUrl, imageName, reason)) {
    
    this.serverUrl = serverUrl;
    this.imageName = imageName;
  }

  /** @return the server being authenticated */
  public string getServerUrl() {
    return serverUrl;
  }

  /** @return the image being authenticated */
  public string getImageName() {
    return imageName;
  }
}
}
