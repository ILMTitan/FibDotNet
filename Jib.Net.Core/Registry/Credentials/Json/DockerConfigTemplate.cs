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

namespace com.google.cloud.tools.jib.registry.credentials.json {







/**
 * Template for a Docker config file.
 *
 * <p>Example:
 *
 * <pre>{@code
 * {
 *   "auths": {
 *     "registry": {
 *       "auth": "username:password in base64"
 *     },
 *     "anotherregistry": {},
 *     ...
 *   },
 *   "credsStore": "credential helper name",
 *   "credHelpers": {
 *     "registry": "credential helper name",
 *     "anotherregistry": "another credential helper name",
 *     ...
 *   }
 * }
 * }</pre>
 *
 * If an {@code auth} is defined for a registry, that is a valid {@code Basic} authorization to use
 * for that registry.
 *
 * <p>If {@code credsStore} is defined, is a credential helper that stores authorizations for all
 * registries listed under {@code auths}.
 *
 * <p>Each entry in {@code credHelpers} is a mapping from a registry to a credential helper that
 * stores the authorization for that registry.
 *
 * @see <a
 *     href="https://www.projectatomic.io/blog/2016/03/docker-credentials-store/">https://www.projectatomic.io/blog/2016/03/docker-credentials-store/</a>
 */
[JsonIgnoreProperties(ignoreUnknown = true)]
public class DockerConfigTemplate : JsonTemplate {

  /** Template for an {@code auth} defined for a registry under {@code auths}. */
  [JsonIgnoreProperties(ignoreUnknown = true)]
  public static class AuthTemplate implements JsonTemplate {

    private string auth;

    public string getAuth() {
      return auth;
    }
  }

  /** Maps from registry to its {@link AuthTemplate}. */
  private readonly Map<string, AuthTemplate> auths = new HashMap<>();

  private string credsStore;

  /** Maps from registry to credential helper name. */
  private readonly Map<string, string> credHelpers = new HashMap<>();

  public Map<string, AuthTemplate> getAuths() {
    return auths;
  }

  public string getCredsStore() {
    return credsStore;
  }

  public Map<string, string> getCredHelpers() {
    return credHelpers;
  }

  DockerConfigTemplate addAuth(string registry, string auth) {
    AuthTemplate authTemplate = new AuthTemplate();
    authTemplate.auth = auth;
    auths.put(registry, authTemplate);
    return this;
  }

  DockerConfigTemplate setCredsStore(string credsStore) {
    this.credsStore = credsStore;
    return this;
  }

  DockerConfigTemplate addCredHelper(string registry, string credHelper) {
    credHelpers.put(registry, credHelper);
    return this;
  }
}
}
