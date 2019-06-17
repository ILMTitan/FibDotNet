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
using Authorization = com.google.cloud.tools.jib.http.Authorization;

namespace com.google.cloud.tools.jib.registry
{
































    /** Tests for {@link RegistryEndpointCaller}. */
    [RunWith(typeof(MockitoJUnitRunner))]
    public class RegistryEndpointCallerTest
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

            public string handleResponse(HttpResponseMessage response)
            {
                if (response.IsSuccessStatusCode)
                {
                    return CharStreams.toString(
                        new StreamReader(response.getBody(), StandardCharsets.UTF_8));
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

        [Test]
        public void testCall_secureCallerOnUnverifiableServer()
        {
            Mock.Get(mockConnection).Setup(m => m.send(It.IsAny<HttpRequestMessage>()))
                .Throws(new HttpRequestException("", new AuthenticationException())); // unverifiable HTTPS server

            try
            {
                secureEndpointCaller.call();
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
        public void testCall_insecureCallerOnUnverifiableServer()
        {
            Mock.Get(mockConnection).Setup(m => m.send(It.IsAny<HttpRequestMessage>()))
                .Throws(new HttpRequestException("", new AuthenticationException())); // unverifiable HTTPS server

            Mock.Get(mockInsecureConnection).Setup(m => m.send(It.IsAny<HttpRequestMessage>())).Returns(mockResponse); // OK with non-verifying connection

            RegistryEndpointCaller<string> insecureCaller = createRegistryEndpointCaller(true, -1);
            Assert.AreEqual("body", insecureCaller.call());


            Mock.Get(mockConnectionFactory).Verify(m => m(new Uri("https://apiroutebase/api")));
            Mock.Get(mockInsecureConnectionFactory).Verify(m => m(new Uri("https://apiroutebase/api")));

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Cannot verify server at https://apiroutebase/api. Attempting again with no TLS verification.")));
        }

        [Test]
        public void testCall_insecureCallerOnHttpServer()
        {
            Mock.Get(mockConnection)
                .Setup(c =>
                    c.send(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://apiroutebase/api")))))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS
            Mock.Get(mockConnection)
                .Setup(c =>
                    c.send(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("http://apiroutebase:80/api")))))
                .Returns(mockResponse);
            Mock.Get(mockInsecureConnection).Setup(c => c.send(It.IsAny<HttpRequestMessage>()))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS

            RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
            Assert.AreEqual("body", insecureEndpointCaller.call());

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Cannot verify server at https://apiroutebase/api. Attempting again with no TLS verification.")));

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Failed to connect to https://apiroutebase/api over HTTPS. Attempting again with HTTP: http://apiroutebase/api")));
        }

        [Test]
        public void testCall_insecureCallerOnHttpServerAndNoPortSpecified()
        {
            Mock.Get(mockConnection)
                .Setup(c => c.send(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://apiroutebase/api")))))
                .Throws(new ConnectException()); // server is not listening on 443

            Mock.Get(mockConnection)
                .Setup(c => c.send(It.Is<HttpRequestMessage>(m=>m.RequestUri.Equals(new Uri("http://apiroutebase/api")))))
                .Returns(mockResponse); // respond when connected through 80

            RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
            Assert.AreEqual("body", insecureEndpointCaller.call());

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Failed to connect to https://apiroutebase/api over HTTPS. Attempting again with HTTP: http://apiroutebase/api")));
        }

        [Test]
        public void testCall_secureCallerOnNonListeningServerAndNoPortSpecified()
        {
            ConnectException expectedException = new ConnectException();
            Mock.Get(mockConnection).Setup(m => m.send(It.IsAny<HttpRequestMessage>()))
                .Throws(expectedException); // server is not listening on 443

            try
            {
                secureEndpointCaller.call();
                Assert.Fail("Should not fall back to HTTP if not allowInsecureRegistries");
            }
            catch (ConnectException ex)
            {
                Assert.AreEqual(expectedException, ex);
            }

            Mock.Get(mockConnectionFactory).Verify(m => m(new Uri("https://apiroutebase/api")));
        }

        [Test]
        public void testCall_insecureCallerOnNonListeningServerAndPortSpecified()
        {
            ConnectException expectedException = new ConnectException();
            Mock.Get(mockConnection).Setup(m => m.send(It.IsAny<HttpRequestMessage>()))
                .Throws(expectedException); // server is not listening on 5000

            RegistryEndpointCaller<string> insecureEndpointCaller =
                createRegistryEndpointCaller(true, 5000);
            try
            {
                insecureEndpointCaller.call();
                Assert.Fail("Should not fall back to HTTP if port was explicitly given and cannot connect");
            }
            catch (ConnectException ex)
            {
                Assert.AreEqual(expectedException, ex);
            }
        }

        [Test]
        public void testCall_noHttpResponse()
        {
            NoHttpResponseException mockNoHttpResponseException =
                new NoHttpResponseException();
            Mock.Get(mockConnection).Setup(m => m.send(It.IsAny<HttpRequestMessage>())).Throws(mockNoHttpResponseException);

            try
            {
                secureEndpointCaller.call();
                Assert.Fail("Call should have failed");
            }
            catch (RegistryNoResponseException ex)
            {
                Assert.AreSame(mockNoHttpResponseException, ex.getCause());
            }
        }

        [Test]
        public void testCall_unauthorized()
        {
            verifyThrowsRegistryUnauthorizedException(HttpStatusCode.Unauthorized);
        }

        [Test]
        public void testCall_credentialsNotSentOverHttp()
        {
            HttpResponseMessage redirectResponse = mockRedirectHttpResponse("http://newlocation");
            HttpResponseMessage unauthroizedResponse =
                mockHttpResponse(HttpStatusCode.Unauthorized, null);

            Mock.Get(mockConnection)
                .Setup(c =>
                    c.send(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://apiroutebase/api")))))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS

            Mock.Get(mockConnection)
                .Setup(c =>
                    c.send(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("http://apiroutebase:80/api")))))
                .Returns(redirectResponse); // redirect to HTTP

            Mock.Get(mockConnection)
                .Setup(c => c.send(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("http://newlocation")))))
                .Returns(unauthroizedResponse); // final response
            Mock.Get(mockInsecureConnection).Setup(c => c.send(It.IsAny<HttpRequestMessage>()))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS

            RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
            try
            {
                insecureEndpointCaller.call();
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
        public void testCall_credentialsForcedOverHttp()
        {
            HttpResponseMessage redirectResponse = mockRedirectHttpResponse("http://newlocation");

            Mock.Get(mockConnection)
                .Setup(c => 
                    c.send(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://apiroutebase/api")))))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS

            Mock.Get(mockConnection)
                .Setup(c =>
                    c.send(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("http://apiroutebase/api")))))
                .Throws(new HttpResponseException(redirectResponse)); // redirect to HTTP

            Mock.Get(mockConnection)
                .Setup(c => c.send(It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("http://newlocation")))))
                .Returns(mockResponse); // final response
            Mock.Get(mockInsecureConnection).Setup(c => c.send(It.IsAny<HttpRequestMessage>()))
                .Throws(new HttpRequestException("", new AuthenticationException())); // server is not HTTPS

            Environment.SetEnvironmentVariable(JibSystemProperties.SEND_CREDENTIALS_OVER_HTTP, "true");
            RegistryEndpointCaller<string> insecureEndpointCaller = createRegistryEndpointCaller(true, -1);
            Assert.AreEqual("body", insecureEndpointCaller.call());

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Cannot verify server at https://apiroutebase/api. Attempting again with no TLS verification.")));

            Mock.Get(mockEventHandlers).Verify(m => m.dispatch(
                    LogEvent.info(
                        "Failed to connect to https://apiroutebase/api over HTTPS. Attempting again with HTTP: http://apiroutebase/api")));
        }

        [Test]
        public void testCall_forbidden()
        {
            verifyThrowsRegistryUnauthorizedException(HttpStatusCode.Forbidden);
        }

        [Test]
        public void testCall_badRequest()
        {
            verifyThrowsRegistryErrorException(HttpStatusCode.BadRequest);
        }

        [Test]
        public void testCall_notFound()
        {
            verifyThrowsRegistryErrorException(HttpStatusCode.NotFound);
        }

        [Test]
        public void testCall_methodNotAllowed()
        {
            verifyThrowsRegistryErrorException(HttpStatusCode.MethodNotAllowed);
        }

        [Test]
        public void testCall_unknown()
        {
            HttpResponseMessage httpResponse =
        mockHttpResponse(HttpStatusCode.InternalServerError, null);
            HttpResponseException httpResponseException = new HttpResponseException(httpResponse);

            Mock.Get(mockConnection).Setup(m => m.send(It.IsAny<HttpRequestMessage>())).Throws(httpResponseException);

            try
            {
                secureEndpointCaller.call();
                Assert.Fail("Call should have failed");
            }
            catch (HttpResponseException ex)
            {
                Assert.AreSame(httpResponseException, ex);
            }
        }

        [Test]
        public void testCall_temporaryRedirect()
        {
            verifyRetriesWithNewLocation(HttpStatusCode.TemporaryRedirect);
        }

        [Test]
        public void testCall_movedPermanently()
        {
            verifyRetriesWithNewLocation(HttpStatusCode.MovedPermanently);
        }

        [Test]
        public void testCall_permanentRedirect()
        {
            verifyRetriesWithNewLocation(RegistryEndpointCaller.STATUS_CODE_PERMANENT_REDIRECT);
        }

        [Test]
        public void testCall_disallowInsecure()
        {
            // Mocks a response for temporary redirect to a new location.
            HttpResponseMessage redirectResponse = mockRedirectHttpResponse("http://newlocation");

            HttpResponseException redirectException = new HttpResponseException(redirectResponse);
            Mock.Get(mockConnection).Setup(m => m.send(It.IsAny<HttpRequestMessage>())).Throws(redirectException);

            try
            {
                secureEndpointCaller.call();
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
        public void testNewRegistryErrorException_jsonErrorOutput()
        {
            HttpResponseException httpException = new HttpResponseException(new HttpResponseMessage
            {
                Content = new StringContent(
                    "{\"errors\": [{\"code\": \"MANIFEST_UNKNOWN\", \"message\": \"manifest unknown\"}]}")
            });

            RegistryErrorException registryException =
                secureEndpointCaller.newRegistryErrorException(httpException);
            Assert.AreSame(httpException.Cause, registryException.Cause);
            Assert.AreEqual(
                "Tried to actionDescription but failed because: manifest unknown | If this is a bug, "
                    + "please file an issue at https://github.com/GoogleContainerTools/jib/issues/new",
                registryException.getMessage());
        }

        [Test]
        public void testNewRegistryErrorException_nonJsonErrorOutput()
        {
            HttpResponseException httpException = new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(">>>>> (404) page not found <<<<<")
            });

            RegistryErrorException registryException =
                secureEndpointCaller.newRegistryErrorException(httpException);
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
        private void verifyThrowsRegistryUnauthorizedException(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage httpResponse = mockHttpResponse(httpStatusCode, null);

            Mock.Get(mockConnection).Setup(m => m.send(It.IsAny<HttpRequestMessage>())).Returns(httpResponse);

            try
            {
                secureEndpointCaller.call();
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
        private void verifyThrowsRegistryErrorException(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage errorResponse = mockHttpResponse(httpStatusCode, null);
            errorResponse.Content = new StringContent("{\"errors\":[{\"code\":\"code\",\"message\":\"message\"}]}");

            HttpResponseException httpResponseException = new HttpResponseException(errorResponse);

            Mock.Get(mockConnection).Setup(m => m.send(It.IsAny<HttpRequestMessage>())).Throws(httpResponseException);

            try
            {
                secureEndpointCaller.call();
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
        private void verifyRetriesWithNewLocation(HttpStatusCode httpStatusCode)
        {
            // Mocks a response for temporary redirect to a new location.
            HttpResponseMessage redirectResponse =
                mockHttpResponse(httpStatusCode, h => h.setLocation("https://newlocation"));

            // Has mockConnection.send throw first, then succeed.
            HttpResponseException redirectException = new HttpResponseException(redirectResponse);
            Mock.Get(mockConnection)
                .Setup(c => c.send(
                    It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://apiroutebase/api")))))
                .Throws(redirectException);
            Mock.Get(mockConnection)
                .Setup(c => c.send(
                    It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(new Uri("https://newlocation")))))
                .Returns(mockResponse);

            Assert.AreEqual("body", secureEndpointCaller.call());

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
