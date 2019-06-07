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





/** Holds an HTTP request. */
public class Request {

  /** The HTTP request headers. */
  private readonly HttpHeaders headers;

  /** The HTTP request body. */
  private final HttpContent body;

  /** HTTP connection and read timeout. */
  private final Integer httpTimeout;

  public static class Builder {

    private readonly HttpHeaders headers = new HttpHeaders().setAccept("*/*");
    private HttpContent body;
    private Integer httpTimeout;

    public Request build() {
      return new Request(this);
    }

    /**
     * Sets the {@code Authorization} header.
     *
     * @param authorization the authorization
     * @return this
     */
    public Builder setAuthorization(Authorization authorization) {
      if (authorization != null) {
        headers.setAuthorization(authorization.toString());
      }
      return this;
    }

    /**
     * Sets the {@code Accept} header.
     *
     * @param mimeTypes the items to pass into the accept header
     * @return this
     */
    public Builder setAccept(List<string> mimeTypes) {
      headers.setAccept(string.join(",", mimeTypes));
      return this;
    }

    /**
     * Sets the {@code User-Agent} header.
     *
     * @param userAgent the user agent
     * @return this
     */
    public Builder setUserAgent(string userAgent) {
      headers.setUserAgent(userAgent);
      return this;
    }

    /**
     * Sets the HTTP connection and read timeout in milliseconds. {@code null} uses the default
     * timeout and {@code 0} an infinite timeout.
     *
     * @param httpTimeout timeout in milliseconds
     * @return this
     */
    public Builder setHttpTimeout(Integer httpTimeout) {
      this.httpTimeout = httpTimeout;
      return this;
    }

    /**
     * Sets the body and its corresponding {@code Content-Type} header.
     *
     * @param httpContent the body content
     * @return this
     */
    public Builder setBody(HttpContent httpContent) {
      this.body = httpContent;
      return this;
    }
  }

  public static Builder builder() {
    return new Builder();
  }

  private Request(Builder builder) {
    this.headers = builder.headers;
    this.body = builder.body;
    this.httpTimeout = builder.httpTimeout;
  }

  HttpHeaders getHeaders() {
    return headers;
  }

  HttpContent getHttpContent() {
    return body;
  }

  Integer getHttpTimeout() {
    return httpTimeout;
  }
}
}
