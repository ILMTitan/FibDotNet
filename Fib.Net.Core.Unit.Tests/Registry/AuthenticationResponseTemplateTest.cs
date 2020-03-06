// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using NUnit.Framework;
using static Fib.Net.Core.Registry.RegistryAuthenticator;

namespace Fib.Net.Core.Unit.Tests.Registry
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
