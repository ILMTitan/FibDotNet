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

namespace com.google.cloud.tools.jib.registry.credentials {
















/**
 * Retrieves Docker credentials with a Docker credential helper.
 *
 * @see <a
 *     href="https://github.com/docker/docker-credential-helpers">https://github.com/docker/docker-credential-helpers</a>
 */
public class DockerCredentialHelper {

  public static readonly string CREDENTIAL_HELPER_PREFIX = "docker-credential-";

  private readonly string serverUrl;
  private readonly Path credentialHelper;

  /** Template for a Docker credential helper output. */
  [JsonIgnoreProperties(ignoreUnknown = true)]
  private class DockerCredentialsTemplate : JsonTemplate  {
    private string Username;
    private string Secret;
  }

  /**
   * Constructs a new {@link DockerCredentialHelper}.
   *
   * @param serverUrl the server Uri to pass into the credential helper
   * @param credentialHelper the path to the credential helper executable
   */
  public DockerCredentialHelper(string serverUrl, Path credentialHelper) {
    this.serverUrl = serverUrl;
    this.credentialHelper = credentialHelper;
  }

  DockerCredentialHelper(string registry, string credentialHelperSuffix) {
    this(registry, Paths.get(CREDENTIAL_HELPER_PREFIX + credentialHelperSuffix));
  }

  /**
   * Calls the credential helper CLI in the form:
   *
   * <pre>{@code
   * echo -n <server Uri> | docker-credential-<credential helper suffix> get
   * }</pre>
   *
   * @return the Docker credentials by calling the corresponding CLI
   * @throws IOException if writing/reading process input/output fails
   * @throws CredentialHelperUnhandledServerUrlException if no credentials could be found for the
   *     corresponding server
   * @throws CredentialHelperNotFoundException if the credential helper CLI doesn't exist
   */
  public Credential retrieve()
      {
    try {
      string[] credentialHelperCommand = {credentialHelper.toString(), "get"};

      Process process = new ProcessBuilder(credentialHelperCommand).start();
      using (OutputStream processStdin = process.getOutputStream()) {
        processStdin.write(serverUrl.getBytes(StandardCharsets.UTF_8));
      }

      using (InputStreamReader processStdoutReader =
          new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8)) {
        string output = CharStreams.toString(processStdoutReader);

        // Throws an exception if the credential store does not have credentials for serverUrl.
        if (output.contains("credentials not found in native keychain")) {
          throw new CredentialHelperUnhandledServerUrlException(
              credentialHelper, serverUrl, output);
        }
        if (output.isEmpty()) {
          using (InputStreamReader processStderrReader =
              new InputStreamReader(process.getErrorStream(), StandardCharsets.UTF_8)) {
            string errorOutput = CharStreams.toString(processStderrReader);
            throw new CredentialHelperUnhandledServerUrlException(
                credentialHelper, serverUrl, errorOutput);
          }
        }

        try {
          DockerCredentialsTemplate dockerCredentials =
              JsonTemplateMapper.readJson(output, typeof(DockerCredentialsTemplate));
          if (Strings.isNullOrEmpty(dockerCredentials.Username)
              || Strings.isNullOrEmpty(dockerCredentials.Secret)) {
            throw new CredentialHelperUnhandledServerUrlException(
                credentialHelper, serverUrl, output);
          }

          return Credential.from(dockerCredentials.Username, dockerCredentials.Secret);

        } catch (JsonProcessingException ex) {
          throw new CredentialHelperUnhandledServerUrlException(
              credentialHelper, serverUrl, output);
        }
      }

    } catch (IOException ex) {
      if (ex.getMessage() == null) {
        throw ex;
      }

      // Checks if the failure is due to a nonexistent credential helper CLI.
      if (ex.getMessage().contains("No such file or directory")
          || ex.getMessage().contains("cannot find the file")) {
        throw new CredentialHelperNotFoundException(credentialHelper, ex);
      }

      throw ex;
    }
  }

  Path getCredentialHelper() {
    return credentialHelper;
  }
}
}
