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

using com.google.cloud.tools.jib.api;
using com.google.cloud.tools.jib.builder;
using com.google.cloud.tools.jib.cache;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.global;
using com.google.cloud.tools.jib.http;
using Jib.Net.Core.Global;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using Authorization = com.google.cloud.tools.jib.http.Authorization;

namespace com.google.cloud.tools.jib.registry
{
    /** Tests for {@link RegistryEndpointCaller}. */
    public class RegistryEndpointCallerTest : IDisposable
    {
        /** Implementation of {@link RegistryEndpointProvider} for testing. */
        private class TestRegistryEndpointProvider : RegistryEndpointProvider<string>
        {
            public HttpMethod getHttpMethod()
            {
                return new HttpMethod("httpMethod");
            }

            public Uri getApiRoute(string apiRouteBase)
            {
                return new Uri(apiRouteBase + "/api");
            }

            public BlobHttpContent getContent()
            {
                return null;
            }

            public IList<string> getAccept()
            {
                return Collections.emptyList<string>();
            }

            public async Task<string> handleResponseAsync(HttpResponseMessage response)
            {
                if (response.IsSuccessStatusCode)
                {
                    return CharStreams.toString(
                        new StreamReader(await response.getBodyAsync().ConfigureAwait(false), StandardCharsets.UTF_8));
                } else
                {
                    throw new HttpResponseException(response);
                }
            }

            public string getActionDescription()
            {
                return "actionDescription";
            }
        }

        private static HttpResponseMessage mockHttpResponse(HttpStatusCode statusCode, Action<HttpResponseHeaders> setHeadersAction)
        {
            HttpResponseMessage mock = new HttpResponseMessage(statusCode);
            setHeadersAction?.Invoke(mock.Headers);

            return mock;
        }

        private static HttpResponseMessage mockRedirectHttpResponse(string redirectLocation)
        {
            const HttpStatusCode code307 = HttpStatusCode.TemporaryRedirect;
            return mockHttpResponse(code307, h => h.setLocation(redirectLocation));
        }

        private IEventHandlers mockEventHandlers;
        private IConnection mockConnection;
        private IConnection mockInsecureConnection;
        private HttpResponseMessage mockResponse;
        private Func<Uri, IConnection> mockConnectionFactory;
        private Func<Uri, IConnection> mockInsecureConnectionFactory;

        private RegistryEndpointCaller<string> secureEndpointCaller;

        [SetUp]
        public void setUp()
        {
            mockEventHandlers = Mock.Of<IEventHandlers>();
            mockConnection = Mock.Of<IConnection>();
            mockInsecureConnection = Mock.Of<IConnection>();
            mockConnectionFactory = Mock.Of<Func<Uri, IConnection>>();
            mockInsecureConnectionFactory = Mock.Of<Func<Uri, IConnection>>();
            secureEndpointCaller = createRegistryEndpointCaller(false, -1);

            Mock.Get(mockConnectionFactory).Setup(m => m(It.IsAny<Uri>())).Returns(mockConnection);

            Mock.Get(mockInsecureConnectionFactory).Setup(m => m(It.IsAny<Uri>())).Returns(mockInsecureConnection);

            mockResponse = new HttpResponseMessage
            {
                Content = new StringContent("body")
            };
        }

        [TearDown]
        public void tearDown()
        {
            Environment.SetEnvironmentVariable(JibSystemProperties.HTTP_TIMEOUT, null);
            Environment.SetEnvironmentVariable(JibSystemProperties.SEND_CREDENTIALS_OVER_HTTP, null);
        }

        public void Dispose()
        {
            mockResponse?.Dispose();
            mockConnection?.Dispose();
            mockInsecureConnection?.Dispose();
        }

        [Test]
        public async Task testCall_secureCallerOnUnverifiableServerAsync()
        {
            Mock.Get(mockConnection).Setup(m => m.sendAsync(It.IsAny<HttpRequestMessage>()))
                .Throws(new HttpRequestException("", new AuthenticationException())); // unverifiable HTTPS server

            try
            {
                await secureEndpointCaller.callAsync().ConfigureAwait(false);
                Assert.Fail("Secure caller should fail if cannot verify server");
            }
            catch (InsecureRegistryException ex)
            {
                Assert.AreEqual(
                    "Failed to verify the server at https://apiroutebase/api because only secure connections are allowed.",
                    ex.getMessage());
            }
        }

        [Test]
        public async Task testCall_insecureCallerOnUnverifiableServerAsync()
        {
            Mock.Get(mockConnection).Setup(m => m.sendAsync(It.IsAny<HttpRequestMessage>()))
                .Throws(new HttpRequestException("", new AuthenticationException())); // unverifiable HTTPS server

            Mock.Get(mockInsecureConnection)
                .Setup(m => m.sendAsync(It.IsAny<HttpRequestMessage>()))
                .Returns(Task.FromResult(mockResponse)); // OK with non-verifying connection

            RegistryEndpointCaller<string> insecureCaller = createRegistryEndpointCaller(true, -1);
            Assert.AreEqual("body", await insecureCaller.callAsync().ConfigureAwait(false));

            Mock.Get(mockConnectionFactory).Verify(m => m(new Uri("https://apiroutebase/api")));
            Mock.Get(mockInsecureConnectionFactory).Verify(m => m(new Uri("https://apiroutebase/api")));

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Cannot verify server at https://apiroutebase/api. Attempting again with no TLS verification.")));
        }

        [Test]
        public async Task testCall_insecureCallerOnHttpServerAsync()
        {
            Mock.Get(mockConnection)
                .Setup(c =>
                    c.sendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://apiroutebase/api")))))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS
            Mock.Get(mockConnection)
                .Setup(c =>
                    c.sendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("http://apiroutebase:80/api")))))
                .Returns(Task.FromResult(mockResponse));
            Mock.Get(mockInsecureConnection).Setup(c => c.sendAsync(It.IsAny<HttpRequestMessage>()))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS

            RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
            Assert.AreEqual("body", await insecureEndpointCaller.callAsync().ConfigureAwait(false));

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Cannot verify server at https://apiroutebase/api. Attempting again with no TLS verification.")));

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Failed to connect to https://apiroutebase/api over HTTPS. Attempting again with HTTP: http://apiroutebase/api")));
        }

        [Test]
        public async Task testCall_insecureCallerOnHttpServerAndNoPortSpecifiedAsync()
        {
            Mock.Get(mockConnection)
                .Setup(c => c.sendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://apiroutebase/api")))))
                .Throws(new ConnectException()); // server is not listening on 443

            Mock.Get(mockConnection)
                .Setup(c => c.sendAsync(It.Is<HttpRequestMessage>(m=>m.RequestUri.Equals(new Uri("http://apiroutebase/api")))))
                .Returns(Task.FromResult(mockResponse)); // respond when connected through 80

            RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
            Assert.AreEqual("body", await insecureEndpointCaller.callAsync().ConfigureAwait(false));

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Failed to connect to https://apiroutebase/api over HTTPS. Attempting again with HTTP: http://apiroutebase/api")));
        }

        [Test]
        public async Task testCall_secureCallerOnNonListeningServerAndNoPortSpecifiedAsync()
        {
            ConnectException expectedException = new ConnectException();
            Mock.Get(mockConnection).Setup(m => m.sendAsync(It.IsAny<HttpRequestMessage>()))
                .Throws(expectedException); // server is not listening on 443

            try
            {
                await secureEndpointCaller.callAsync().ConfigureAwait(false);
                Assert.Fail("Should not fall back to HTTP if not allowInsecureRegistries");
            }
            catch (ConnectException ex)
            {
                Assert.AreEqual(expectedException, ex);
            }

            Mock.Get(mockConnectionFactory).Verify(m => m(new Uri("https://apiroutebase/api")));
        }

        [Test]
        public async Task testCall_insecureCallerOnNonListeningServerAndPortSpecifiedAsync()
        {
            ConnectException expectedException = new ConnectException();
            Mock.Get(mockConnection).Setup(m => m.sendAsync(It.IsAny<HttpRequestMessage>()))
                .Throws(expectedException); // server is not listening on 5000

            RegistryEndpointCaller<string> insecureEndpointCaller =
                createRegistryEndpointCaller(true, 5000);
            try
            {
                await insecureEndpointCaller.callAsync().ConfigureAwait(false);
                Assert.Fail("Should not fall back to HTTP if port was explicitly given and cannot connect");
            }
            catch (ConnectException ex)
            {
                Assert.AreEqual(expectedException, ex);
            }
        }

        [Test]
        public async Task testCall_noHttpResponseAsync()
        {
            var mockNoHttpResponseException = new TimeoutException("no response");
            Mock.Get(mockConnection).Setup(m => m.sendAsync(It.IsAny<HttpRequestMessage>())).Throws(mockNoHttpResponseException);

            try
            {
                await secureEndpointCaller.callAsync().ConfigureAwait(false);
                Assert.Fail("Call should have failed");
            }
            catch (RegistryNoResponseException ex)
            {
                Assert.AreSame(mockNoHttpResponseException, ex.getCause());
            }
        }

        [Test]
        public async Task testCall_unauthorizedAsync()
        {
            await verifyThrowsRegistryUnauthorizedExceptionAsync(HttpStatusCode.Unauthorized).ConfigureAwait(false);
        }

        [Test]
        public async Task testCall_credentialsNotSentOverHttpAsync()
        {
            HttpResponseMessage redirectResponse = mockRedirectHttpResponse("http://newlocation");
            HttpResponseMessage unauthroizedResponse =
                mockHttpResponse(HttpStatusCode.Unauthorized, null);

            Mock.Get(mockConnection)
                .Setup(c =>
                    c.sendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://apiroutebase/api")))))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS

            Mock.Get(mockConnection)
                .Setup(c =>
                    c.sendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("http://apiroutebase:80/api")))))
                .Returns(Task.FromResult(redirectResponse)); // redirect to HTTP

            Mock.Get(mockConnection)
                .Setup(c => c.sendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("http://newlocation")))))
                .Returns(Task.FromResult(unauthroizedResponse)); // final response
            Mock.Get(mockInsecureConnection).Setup(c => c.sendAsync(It.IsAny<HttpRequestMessage>()))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS

            RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
            try
            {
                await insecureEndpointCaller.callAsync().ConfigureAwait(false);
                Assert.Fail("Call should have failed");
            }
            catch (RegistryCredentialsNotSentException ex)
            {
                Assert.AreEqual(
                    "Required credentials for serverUrl/imageName were not sent because the connection was over HTTP",
                    ex.getMessage());
            }
        }

        [Test]
        public async Task testCall_credentialsForcedOverHttpAsync()
        {
            HttpResponseMessage redirectResponse = mockRedirectHttpResponse("http://newlocation");

            Mock.Get(mockConnection)
                .Setup(c =>
                    c.sendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://apiroutebase/api")))))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS

            Mock.Get(mockConnection)
                .Setup(c =>
                    c.sendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("http://apiroutebase/api")))))
                .Throws(new HttpResponseException(redirectResponse)); // redirect to HTTP

            Mock.Get(mockConnection)
                .Setup(c => c.sendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("http://newlocation")))))
                .Returns(Task.FromResult(mockResponse)); // final response
            Mock.Get(mockInsecureConnection).Setup(c => c.sendAsync(It.IsAny<HttpRequestMessage>()))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS

            Environment.SetEnvironmentVariable(JibSystemProperties.SEND_CREDENTIALS_OVER_HTTP, "true");
            RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
            Assert.AreEqual("body", await insecureEndpointCaller.callAsync().ConfigureAwait(false));

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Cannot verify server at https://apiroutebase/api. Attempting again with no TLS verification.")));

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Failed to connect to https://apiroutebase/api over HTTPS. Attempting again with HTTP: http://apiroutebase/api")));
        }

        [Test]
        public async Task testCall_forbiddenAsync()
        {
            await verifyThrowsRegistryUnauthorizedExceptionAsync(HttpStatusCode.Forbidden).ConfigureAwait(false);
        }

        [Test]
        public async Task testCall_badRequestAsync()
        {
            await verifyThrowsRegistryErrorExceptionAsync(HttpStatusCode.BadRequest).ConfigureAwait(false);
        }

        [Test]
        public async Task testCall_notFoundAsync()
        {
            await verifyThrowsRegistryErrorExceptionAsync(HttpStatusCode.NotFound).ConfigureAwait(false);
        }

        [Test]
        public async Task testCall_methodNotAllowedAsync()
        {
            await verifyThrowsRegistryErrorExceptionAsync(HttpStatusCode.MethodNotAllowed).ConfigureAwait(false);
        }

        [Test]
        public async Task testCall_unknownAsync()
        {
            HttpResponseMessage httpResponse =
        mockHttpResponse(HttpStatusCode.InternalServerError, null);
            HttpResponseException httpResponseException = new HttpResponseException(httpResponse);

            Mock.Get(mockConnection).Setup(m => m.sendAsync(It.IsAny<HttpRequestMessage>())).Throws(httpResponseException);

            try
            {
                await secureEndpointCaller.callAsync().ConfigureAwait(false);
                Assert.Fail("Call should have failed");
            }
            catch (HttpResponseException ex)
            {
                Assert.AreSame(httpResponseException, ex);
            }
        }

        [Test]
        public async Task testCall_temporaryRedirectAsync()
        {
            await verifyRetriesWithNewLocationAsync(HttpStatusCode.TemporaryRedirect).ConfigureAwait(false);
        }

        [Test]
        public async Task testCall_movedPermanentlyAsync()
        {
            await verifyRetriesWithNewLocationAsync(HttpStatusCode.MovedPermanently).ConfigureAwait(false);
        }

        [Test]
        public async Task testCall_permanentRedirectAsync()
        {
            await verifyRetriesWithNewLocationAsync(RegistryEndpointCaller.STATUS_CODE_PERMANENT_REDIRECT).ConfigureAwait(false);
        }

        [Test]
        public async Task testCall_disallowInsecureAsync()
        {
            // Mocks a response for temporary redirect to a new location.
            HttpResponseMessage redirectResponse = mockRedirectHttpResponse("http://newlocation");

            HttpResponseException redirectException = new HttpResponseException(redirectResponse);
            Mock.Get(mockConnection).Setup(m => m.sendAsync(It.IsAny<HttpRequestMessage>())).Throws(redirectException);

            try
            {
                await secureEndpointCaller.callAsync().ConfigureAwait(false);
                Assert.Fail("Call should have failed");
            }
            catch (InsecureRegistryException)
            {
                // pass
            }
        }

        [Test]
        public void testIsBrokenPipe_notBrokenPipe()
        {
            Assert.IsFalse(RegistryEndpointCaller.isBrokenPipe(new IOException()));
            Assert.IsFalse(RegistryEndpointCaller.isBrokenPipe(new SocketException()));
            Assert.IsFalse(RegistryEndpointCaller.isBrokenPipe(new AuthenticationException("mock")));
            Assert.IsFalse(RegistryEndpointCaller.isBrokenPipe(new HttpRequestException("mock")));
        }

        [Test]
        public void testIsBrokenPipe_brokenPipe()
        {
            Assert.IsTrue(RegistryEndpointCaller.isBrokenPipe(new Win32Exception(RegistryEndpointCaller.ERROR_BROKEN_PIPE)));
            Assert.IsTrue(RegistryEndpointCaller.isBrokenPipe(new SocketException(RegistryEndpointCaller.ERROR_BROKEN_PIPE)));
            Assert.IsTrue(RegistryEndpointCaller.isBrokenPipe(
                new HttpRequestException("mock", new SocketException(RegistryEndpointCaller.ERROR_BROKEN_PIPE))));
        }

        [Test]
        public void testIsBrokenPipe_nestedBrokenPipe()
        {
            IOException exception = new IOException(
                "",
                new AuthenticationException("", new SocketException(RegistryEndpointCaller.ERROR_BROKEN_PIPE)));
            Assert.IsTrue(RegistryEndpointCaller.isBrokenPipe(exception));
        }

        [Test]
        public async Task testNewRegistryErrorException_jsonErrorOutputAsync()
        {
            HttpResponseException httpException = new HttpResponseException(new HttpResponseMessage
            {
                Content = new StringContent(
                    "{\"errors\": [{\"code\": \"MANIFEST_UNKNOWN\", \"message\": \"manifest unknown\"}]}")
            });

            RegistryErrorException registryException =
                await secureEndpointCaller.newRegistryErrorExceptionAsync(httpException).ConfigureAwait(false);
            Assert.AreSame(httpException.Cause, registryException.Cause);
            Assert.AreEqual(
                "Tried to actionDescription but failed because: manifest unknown | If this is a bug, "
                    + "please file an issue at https://github.com/GoogleContainerTools/jib/issues/new",
                registryException.getMessage());
        }

        [Test]
        public async Task testNewRegistryErrorException_nonJsonErrorOutputAsync()
        {
            HttpResponseException httpException = new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(">>>>> (404) page not found <<<<<")
            });

            RegistryErrorException registryException =
                await secureEndpointCaller.newRegistryErrorExceptionAsync(httpException).ConfigureAwait(false);
            Assert.AreSame(httpException.Cause, registryException.Cause);
            Assert.AreEqual(
                "Tried to actionDescription but failed because: registry returned error code 404; "
                    + "possible causes include invalid or wrong reference. Actual error output follows:\n"
                    + ">>>>> (404) page not found <<<<<\n"
                    + " | If this is a bug, please file an issue at "
                    + "https://github.com/GoogleContainerTools/jib/issues/new",
                registryException.getMessage());
        }

        /**
         * Verifies that a response with {@code httpStatusCode} throws {@link
         * RegistryUnauthorizedException}.
         */
        private async Task verifyThrowsRegistryUnauthorizedExceptionAsync(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage httpResponse = mockHttpResponse(httpStatusCode, null);

            Mock.Get(mockConnection).Setup(m => m.sendAsync(It.IsAny<HttpRequestMessage>())).Returns(Task.FromResult(httpResponse));

            try
            {
                await secureEndpointCaller.callAsync().ConfigureAwait(false);
                Assert.Fail("Call should have failed");
            }
            catch (RegistryUnauthorizedException ex)
            {
                Assert.AreEqual("serverUrl", ex.getRegistry());
                Assert.AreEqual("imageName", ex.getRepository());
                Assert.AreSame(httpResponse, ex.Cause);
            }
        }

        /**
         * Verifies that a response with {@code httpStatusCode} throws {@link
         * RegistryUnauthorizedException}.
         */
        private async Task verifyThrowsRegistryErrorExceptionAsync(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage errorResponse = mockHttpResponse(httpStatusCode, null);
            errorResponse.Content = new StringContent("{\"errors\":[{\"code\":\"code\",\"message\":\"message\"}]}");

            HttpResponseException httpResponseException = new HttpResponseException(errorResponse);

            Mock.Get(mockConnection).Setup(m => m.sendAsync(It.IsAny<HttpRequestMessage>())).Throws(httpResponseException);

            try
            {
                await secureEndpointCaller.callAsync().ConfigureAwait(false);
                Assert.Fail("Call should have failed");
            }
            catch (RegistryErrorException ex)
            {
                Assert.That(
                    ex.getMessage(), Does.Contain(
                        "Tried to actionDescription but failed because: unknown: message"));
            }
        }

        /**
         * Verifies that a response with {@code httpStatusCode} retries the request with the {@code
         * Location} header.
         */
        private async Task verifyRetriesWithNewLocationAsync(HttpStatusCode httpStatusCode)
        {
            // Mocks a response for temporary redirect to a new location.
            HttpResponseMessage redirectResponse =
                mockHttpResponse(httpStatusCode, h => h.setLocation("https://newlocation"));

            // Has mockConnection.send throw first, then succeed.
            HttpResponseException redirectException = new HttpResponseException(redirectResponse);
            Mock.Get(mockConnection)
                .Setup(c => c.sendAsync(
                    It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://apiroutebase/api")))))
                .Throws(redirectException);
            Mock.Get(mockConnection)
                .Setup(c => c.sendAsync(
                    It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://newlocation")))))
                .Returns(Task.FromResult(mockResponse));

            Assert.AreEqual("body", await secureEndpointCaller.callAsync().ConfigureAwait(false));

            // Checks that the Uri was changed to the new location.
            Mock.Get(mockConnectionFactory).Verify(m => m(new Uri("https://apiroutebase/api")));
            Mock.Get(mockConnectionFactory).Verify(m => m(new Uri("https://newlocation")));
        }

        private RegistryEndpointCaller<string> createRegistryEndpointCaller(
            bool allowInsecure, int port)
        {
            return new RegistryEndpointCaller<string>(
                mockEventHandlers,
                new[] { new ProductInfoHeaderValue(new ProductHeaderValue("userAgent")) },
                (port == -1) ? "apiroutebase" : ("apiroutebase:" + port),
                new TestRegistryEndpointProvider(),
                Authorization.fromBasicToken("token"),
                new RegistryEndpointRequestProperties("serverUrl", "imageName"),
                allowInsecure,
                mockConnectionFactory,
                mockInsecureConnectionFactory);
        }
    }
}
