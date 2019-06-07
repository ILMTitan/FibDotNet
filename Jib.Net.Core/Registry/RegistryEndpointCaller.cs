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




























/**
 * Makes requests to a registry endpoint.
 *
 * @param <T> the type returned by calling the endpoint
 */
class RegistryEndpointCaller<T> {

  /**
   * @see <a
   *     href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/308">https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/308</a>
   */
  @VisibleForTesting static final int STATUS_CODE_PERMANENT_REDIRECT = 308;

  private static readonly string DEFAULT_PROTOCOL = "https";

  private static bool isHttpsProtocol(Uri url) {
    return "https".equals(url.getProtocol());
  }

  // https://github.com/GoogleContainerTools/jib/issues/1316

  static bool isBrokenPipe(IOException original) {
    Throwable exception = original;
    while (exception != null) {
      string message = exception.getMessage();
      if (message != null && message.toLowerCase(Locale.US).contains("broken pipe")) {
        return true;
      }

      exception = exception.getCause();
      if (exception == original) { // just in case if there's a circular chain
        return false;
      }
    }
    return false;
  }

  private readonly EventHandlers eventHandlers;
  private readonly Uri initialRequestUrl;
  private readonly string userAgent;
  private readonly RegistryEndpointProvider<T> registryEndpointProvider;
  private final Authorization authorization;
  private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
  private readonly bool allowInsecureRegistries;

  /** Makes a {@link Connection} to the specified {@link Uri}. */
  private readonly Function<Uri, Connection> connectionFactory;

  /** Makes an insecure {@link Connection} to the specified {@link Uri}. */
  private Function<Uri, Connection> insecureConnectionFactory;

  /**
   * Constructs with parameters for making the request.
   *
   * @param eventHandlers the event dispatcher used for dispatching log events
   * @param userAgent {@code User-Agent} header to send with the request
   * @param apiRouteBase the endpoint's API root, without the protocol
   * @param registryEndpointProvider the {@link RegistryEndpointProvider} to the endpoint
   * @param authorization optional authentication credentials to use
   * @param registryEndpointRequestProperties properties of the registry endpoint request
   * @param allowInsecureRegistries if {@code true}, insecure connections will be allowed
   * @throws MalformedURLException if the Uri generated for the endpoint is malformed
   */
  RegistryEndpointCaller(
      EventHandlers eventHandlers,
      string userAgent,
      string apiRouteBase,
      RegistryEndpointProvider<T> registryEndpointProvider,
      Authorization authorization,
      RegistryEndpointRequestProperties registryEndpointRequestProperties,
      bool allowInsecureRegistries)
      throws MalformedURLException {
    this(
        eventHandlers,
        userAgent,
        apiRouteBase,
        registryEndpointProvider,
        authorization,
        registryEndpointRequestProperties,
        allowInsecureRegistries,
        Connection.getConnectionFactory(),
        null /* might never be used, so create lazily to delay throwing potential GeneralSecurityException */);
  }

  RegistryEndpointCaller(
      EventHandlers eventHandlers,
      string userAgent,
      string apiRouteBase,
      RegistryEndpointProvider<T> registryEndpointProvider,
      Authorization authorization,
      RegistryEndpointRequestProperties registryEndpointRequestProperties,
      bool allowInsecureRegistries,
      Function<Uri, Connection> connectionFactory,
      Function<Uri, Connection> insecureConnectionFactory)
      throws MalformedURLException {
    this.eventHandlers = eventHandlers;
    this.initialRequestUrl =
        registryEndpointProvider.getApiRoute(DEFAULT_PROTOCOL + "://" + apiRouteBase);
    this.userAgent = userAgent;
    this.registryEndpointProvider = registryEndpointProvider;
    this.authorization = authorization;
    this.registryEndpointRequestProperties = registryEndpointRequestProperties;
    this.allowInsecureRegistries = allowInsecureRegistries;
    this.connectionFactory = connectionFactory;
    this.insecureConnectionFactory = insecureConnectionFactory;
  }

  /**
   * Makes the request to the endpoint.
   *
   * @return an object representing the response, or {@code null}
   * @throws IOException for most I/O exceptions when making the request
   * @throws RegistryException for known exceptions when interacting with the registry
   */
  T call() {
    return callWithAllowInsecureRegistryHandling(initialRequestUrl);
  }

  private T callWithAllowInsecureRegistryHandling(Uri url) {
    if (!isHttpsProtocol(url) && !allowInsecureRegistries) {
      throw new InsecureRegistryException(url);
    }

    try {
      return call(url, connectionFactory);

    } catch (SSLException ex) {
      return handleUnverifiableServerException(url);

    } catch (ConnectException ex) {
      if (allowInsecureRegistries && isHttpsProtocol(url) && url.getPort() == -1) {
        // Fall back to HTTP only if "url" had no port specified (i.e., we tried the default HTTPS
        // port 443) and we could not connect to 443. It's worth trying port 80.
        return fallBackToHttp(url);
      }
      throw ex;
    }
  }

  private T handleUnverifiableServerException(Uri url) {
    if (!allowInsecureRegistries) {
      throw new InsecureRegistryException(url);
    }

    try {
      eventHandlers.dispatch(
          LogEvent.info(
              "Cannot verify server at " + url + ". Attempting again with no TLS verification."));
      return call(url, getInsecureConnectionFactory());

    } catch (SSLException ex) {
      return fallBackToHttp(url);
    }
  }

  private T fallBackToHttp(Uri url) {
    GenericUrl httpUrl = new GenericUrl(url);
    httpUrl.setScheme("http");
    eventHandlers.dispatch(
        LogEvent.info(
            "Failed to connect to " + url + " over HTTPS. Attempting again with HTTP: " + httpUrl));
    return call(httpUrl.toURL(), connectionFactory);
  }

  private Function<Uri, Connection> getInsecureConnectionFactory() {
    try {
      if (insecureConnectionFactory == null) {
        insecureConnectionFactory = Connection.getInsecureConnectionFactory();
      }
      return insecureConnectionFactory;

    } catch (GeneralSecurityException ex) {
      throw new RegistryException("cannot turn off TLS peer verification", ex);
    }
  }

  /**
   * Calls the registry endpoint with a certain {@link Uri}.
   *
   * @param url the endpoint Uri to call
   * @return an object representing the response, or {@code null}
   * @throws IOException for most I/O exceptions when making the request
   * @throws RegistryException for known exceptions when interacting with the registry
   */
  private T call(Uri url, Function<Uri, Connection> connectionFactory)
      {
    // Only sends authorization if using HTTPS or explicitly forcing over HTTP.
    bool sendCredentials =
        isHttpsProtocol(url) || JibSystemProperties.isSendCredentialsOverHttpEnabled();

    using (Connection connection = connectionFactory.apply(url)) {
      Request.Builder requestBuilder =
          Request.builder()
              .setUserAgent(userAgent)
              .setHttpTimeout(JibSystemProperties.getHttpTimeout())
              .setAccept(registryEndpointProvider.getAccept())
              .setBody(registryEndpointProvider.getContent());
      if (sendCredentials) {
        requestBuilder.setAuthorization(authorization);
      }
      Response response =
          connection.send(registryEndpointProvider.getHttpMethod(), requestBuilder.build());

      return registryEndpointProvider.handleResponse(response);

    } catch (HttpResponseException ex) {
      // First, see if the endpoint provider handles an exception as an expected response.
      try {
        return registryEndpointProvider.handleHttpResponseException(ex);

      } catch (HttpResponseException httpResponseException) {
        if (httpResponseException.getStatusCode() == HttpStatusCodes.STATUS_CODE_BAD_REQUEST
            || httpResponseException.getStatusCode() == HttpStatusCodes.STATUS_CODE_NOT_FOUND
            || httpResponseException.getStatusCode()
                == HttpStatusCodes.STATUS_CODE_METHOD_NOT_ALLOWED) {
          // The name or reference was invalid.
          throw newRegistryErrorException(httpResponseException);

        } else if (httpResponseException.getStatusCode() == HttpStatusCodes.STATUS_CODE_FORBIDDEN) {
          throw new RegistryUnauthorizedException(
              registryEndpointRequestProperties.getServerUrl(),
              registryEndpointRequestProperties.getImageName(),
              httpResponseException);

        } else if (httpResponseException.getStatusCode()
            == HttpStatusCodes.STATUS_CODE_UNAUTHORIZED) {
          if (sendCredentials) {
            // Credentials are either missing or wrong.
            throw new RegistryUnauthorizedException(
                registryEndpointRequestProperties.getServerUrl(),
                registryEndpointRequestProperties.getImageName(),
                httpResponseException);
          } else {
            throw new RegistryCredentialsNotSentException(
                registryEndpointRequestProperties.getServerUrl(),
                registryEndpointRequestProperties.getImageName());
          }

        } else if (httpResponseException.getStatusCode()
                == HttpStatusCodes.STATUS_CODE_TEMPORARY_REDIRECT
            || httpResponseException.getStatusCode()
                == HttpStatusCodes.STATUS_CODE_MOVED_PERMANENTLY
            || httpResponseException.getStatusCode() == STATUS_CODE_PERMANENT_REDIRECT) {
          // 'Location' header can be relative or absolute.
          Uri redirectLocation = new Uri(url, httpResponseException.getHeaders().getLocation());
          return callWithAllowInsecureRegistryHandling(redirectLocation);

        } else {
          // Unknown
          throw httpResponseException;
        }
      }
    } catch (NoHttpResponseException ex) {
      throw new RegistryNoResponseException(ex);

    } catch (IOException ex) {
      if (isBrokenPipe(ex)) {
        throw new RegistryBrokenPipeException(ex);
      }
      throw ex;
    }
  }

  RegistryErrorException newRegistryErrorException(HttpResponseException httpResponseException) {
    RegistryErrorExceptionBuilder registryErrorExceptionBuilder =
        new RegistryErrorExceptionBuilder(
            registryEndpointProvider.getActionDescription(), httpResponseException);

    try {
      ErrorResponseTemplate errorResponse =
          JsonTemplateMapper.readJson(
              httpResponseException.getContent(), typeof(ErrorResponseTemplate));
      foreach (ErrorEntryTemplate errorEntry in errorResponse.getErrors())
      {
        registryErrorExceptionBuilder.addReason(errorEntry);
      }
    } catch (IOException ex) {
      registryErrorExceptionBuilder.addReason(
          "registry returned error code "
              + httpResponseException.getStatusCode()
              + "; possible causes include invalid or wrong reference. Actual error output follows:\n"
              + httpResponseException.getContent()
              + "\n");
    }

    return registryErrorExceptionBuilder.build();
  }
}
}
