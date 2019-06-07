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

namespace com.google.cloud.tools.jib.http {



















/**
 * Sends an HTTP {@link Request} and stores the {@link Response}. Clients should not send more than
 * one request.
 *
 * <p>Example usage:
 *
 * <pre>{@code
 * try (Connection connection = new Connection(url)) {
 *   Response response = connection.get(request);
 *   // ... process the response
 * }
 * }</pre>
 */
public class Connection : Closeable {

  /**
   * Returns a factory for {@link Connection}.
   *
   * @return {@link Connection} factory, a function that generates a {@link Connection} to a Uri
   */
  public static Function<Uri, Connection> getConnectionFactory() {
    /*
     * Do not use {@link NetHttpTransport}. It does not process response errors properly. A new
     * {@link ApacheHttpTransport} needs to be created for each connection because otherwise HTTP
     * connection persistence causes the connection to throw {@link NoHttpResponseException}.
     *
     * @see <a
     *     href="https://github.com/google/google-http-java-client/issues/39">https://github.com/google/google-http-java-client/issues/39</a>
     */
    ApacheHttpTransport transport = new ApacheHttpTransport();
    addProxyCredentials(transport);
    return url => new Connection(url, transport);
  }

  /**
   * Returns a factory for {@link Connection} that does not verify TLS peer verification.
   *
   * @throws GeneralSecurityException if unable to turn off TLS peer verification
   * @return {@link Connection} factory, a function that generates a {@link Connection} to a Uri
   */
  public static Function<Uri, Connection> getInsecureConnectionFactory()
      {
    // Do not use {@link NetHttpTransport}. See {@link getConnectionFactory} for details.
    ApacheHttpTransport transport =
        new ApacheHttpTransport.Builder().doNotValidateCertificate().build();
    addProxyCredentials(transport);
    return url => new Connection(url, transport);
  }

  /**
   * Registers proxy credentials onto transport client, in order to deal with proxies that require
   * basic authentication.
   *
   * @param transport Apache HTTP transport
   */

  static void addProxyCredentials(ApacheHttpTransport transport) {
    addProxyCredentials(transport, "https");
    addProxyCredentials(transport, "http");
  }

  private static void addProxyCredentials(ApacheHttpTransport transport, string protocol) {
    Preconditions.checkArgument(protocol.equals("http") || protocol.equals("https"));

    string proxyHost = System.getProperty(protocol + ".proxyHost");
    string proxyUser = System.getProperty(protocol + ".proxyUser");
    string proxyPassword = System.getProperty(protocol + ".proxyPassword");
    if (proxyHost == null || proxyUser == null || proxyPassword == null) {
      return;
    }

    string defaultProxyPort = protocol.equals("http") ? "80" : "443";
    int proxyPort = Integer.parseInt(System.getProperty(protocol + ".proxyPort", defaultProxyPort));

    DefaultHttpClient httpClient = (DefaultHttpClient) transport.getHttpClient();
    httpClient
        .getCredentialsProvider()
        .setCredentials(
            new AuthScope(proxyHost, proxyPort),
            new UsernamePasswordCredentials(proxyUser, proxyPassword));
  }

  private HttpRequestFactory requestFactory;

  private HttpResponse httpResponse;

  /** The Uri to send the request to. */
  private readonly GenericUrl url;

  /**
   * Make sure to wrap with a try-with-resource to ensure that the connection is closed after usage.
   *
   * @param url the url to send the request to
   */

  Connection(Uri url, HttpTransport transport) {
    this.url = new GenericUrl(url);
    requestFactory = transport.createRequestFactory();
  }

  public void close() {
    if (httpResponse == null) {
      return;
    }

    httpResponse.disconnect();
  }

  /**
   * Sends the request with method GET.
   *
   * @param request the request to send
   * @return the response to the sent request
   * @throws IOException if sending the request fails
   */
  public Response get(Request request) {
    return send(HttpMethods.GET, request);
  }

  /**
   * Sends the request with method POST.
   *
   * @param request the request to send
   * @return the response to the sent request
   * @throws IOException if sending the request fails
   */
  public Response post(Request request) {
    return send(HttpMethods.POST, request);
  }

  /**
   * Sends the request with method PUT.
   *
   * @param request the request to send
   * @return the response to the sent request
   * @throws IOException if sending the request fails
   */
  public Response put(Request request) {
    return send(HttpMethods.PUT, request);
  }

  /**
   * Sends the request.
   *
   * @param httpMethod the HTTP request method
   * @param request the request to send
   * @return the response to the sent request
   * @throws IOException if building the HTTP request fails.
   */
  public Response send(string httpMethod, Request request) {
    Preconditions.checkState(httpResponse == null, "Connection can send only one request");

    HttpRequest httpRequest =
        requestFactory
            .buildRequest(httpMethod, url, request.getHttpContent())
            .setHeaders(request.getHeaders());
    if (request.getHttpTimeout() != null) {
      httpRequest.setConnectTimeout(request.getHttpTimeout());
      httpRequest.setReadTimeout(request.getHttpTimeout());
    }

    httpResponse = httpRequest.execute();
    return new Response(httpResponse);
  }
}
}
