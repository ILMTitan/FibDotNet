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

using com.google.cloud.tools.jib.global;
using Jib.Net.Core.Global;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.http
{









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
    public class Connection : IDisposable
    {
        /**
         * Returns a factory for {@link Connection}.
         *
         * @return {@link Connection} factory, a function that generates a {@link Connection} to a Uri
         */
        public static Func<Uri, Connection> getConnectionFactory()
        {
            /*
             * Do not use {@link NetHttpTransport}. It does not process response errors properly. A new
             * {@link ApacheHttpTransport} needs to be created for each connection because otherwise HTTP
             * connection persistence causes the connection to throw {@link NoHttpResponseException}.
             *
             * @see <a
             *     href="https://github.com/google/google-http-java-client/issues/39">https://github.com/google/google-http-java-client/issues/39</a>
             */
            return url => new Connection(url);
        }

        /**
         * Returns a factory for {@link Connection} that does not verify TLS peer verification.
         *
         * @throws GeneralSecurityException if unable to turn off TLS peer verification
         * @return {@link Connection} factory, a function that generates a {@link Connection} to a Uri
         */
        public static Func<Uri, Connection> getInsecureConnectionFactory()
        {
            // Do not use {@link NetHttpTransport}. See {@link getConnectionFactory} for details.

            return url => new Connection(url);
        }

        /** The Uri to send the request to. */
        private readonly HttpClient client;

        /**
         * Make sure to wrap with a try-with-resource to ensure that the connection is closed after usage.
         *
         * @param url the url to send the request to
         */

        public Connection(Uri url)
        {
            client = new HttpClient() { BaseAddress = url, Timeout = TimeSpan.FromMilliseconds(JibSystemProperties.getHttpTimeout()) };
        }

        public void Dispose()
        {
        }

        /**
         * Sends the request.
         *
         * @param httpMethod the HTTP request method
         * @param request the request to send
         * @return the response to the sent request
         * @throws IOException if building the HTTP request fails.
         */
        public HttpResponseMessage send(HttpRequestMessage request)
        {
            return client.SendAsync(request).Result;
        }

        internal int? getRequestedHttpTimeout()
        {
            throw new NotImplementedException();
        }
    }
}