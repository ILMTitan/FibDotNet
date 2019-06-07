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

namespace com.google.cloud.tools.jib.registry {












/** Retrieves the {@code WWW-Authenticate} header from the registry API. */
class AuthenticationMethodRetriever : $2 {
  private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
  private readonly string userAgent;

  AuthenticationMethodRetriever(
      RegistryEndpointRequestProperties registryEndpointRequestProperties, string userAgent) {
    this.registryEndpointRequestProperties = registryEndpointRequestProperties;
    this.userAgent = userAgent;
  }

  public BlobHttpContent getContent() {
    return null;
  }

  public List<string> getAccept() {
    return Collections.emptyList();
  }

  /**
   * The request did not error, meaning that the registry does not require authentication.
   *
   * @param response ignored
   * @return {@code null}
   */

  public RegistryAuthenticator handleResponse(Response response) {
    return null;
  }

  public Uri getApiRoute(string apiRouteBase) {
    return new Uri(apiRouteBase);
  }

  public string getHttpMethod() {
    return HttpMethods.GET;
  }

  public string getActionDescription() {
    return "retrieve authentication method for " + registryEndpointRequestProperties.getServerUrl();
  }

  public RegistryAuthenticator handleHttpResponseException(
      HttpResponseException httpResponseException)
      {
    // Only valid for status code of '401 Unauthorized'.
    if (httpResponseException.getStatusCode() != HttpStatusCodes.STATUS_CODE_UNAUTHORIZED) {
      throw httpResponseException;
    }

    // Checks if the 'WWW-Authenticate' header is present.
    string authenticationMethod = httpResponseException.getHeaders().getAuthenticate();
    if (authenticationMethod == null) {
      throw new RegistryErrorExceptionBuilder(getActionDescription(), httpResponseException)
          .addReason("'WWW-Authenticate' header not found")
          .build();
    }

    // Parses the header to retrieve the components.
    try {
      return RegistryAuthenticator.fromAuthenticationMethod(
          authenticationMethod, registryEndpointRequestProperties, userAgent);

    } catch (RegistryAuthenticationFailedException ex) {
      throw new RegistryErrorExceptionBuilder(getActionDescription(), ex)
          .addReason("Failed get authentication method from 'WWW-Authenticate' header")
          .build();
    }
  }
}
}
