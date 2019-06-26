using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using static com.google.cloud.tools.jib.registry.RegistryAuthenticator;

namespace Jib.Net.Core.Unit.Tests.Registry
{
    public class AuthenticationResponseTemplateTest
    {
        private const string json = "{\"token\":\"token string\",\"access_token\":\"access token string\",\"expires_in\":300,\"issued_at\":\"2019-06-17T22:21:13.872586297Z\"}";

        [Test]
        public void TestDeserialize()
        {
            var template = JsonConvert.DeserializeObject<AuthenticationResponseTemplate>(json);
            Assert.AreEqual(template.Token, "token string");
            Assert.AreEqual(template.AccessToken, "access token string");
        }
    }
}
