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

using Jib.Net.Core.Global;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link Port}. */

    public class PortTest
    {
        [Test]
        public void TestTcp()
        {
            Port port = Port.Tcp(5555);
            Assert.AreEqual(5555, port.GetPort());
            Assert.AreEqual("5555/tcp", JavaExtensions.ToString(port));
        }

        [Test]
        public void TestUdp()
        {
            Port port = Port.Udp(6666);
            Assert.AreEqual(6666, port.GetPort());
            Assert.AreEqual("6666/udp", JavaExtensions.ToString(port));
        }

        [Test]
        public void TestParseProtocol()
        {
            Assert.AreEqual(Port.Tcp(1111), Port.ParseProtocol(1111, "tcp"));
            Assert.AreEqual(Port.Udp(2222), Port.ParseProtocol(2222, "udp"));
            Assert.AreEqual(Port.Tcp(3333), Port.ParseProtocol(3333, ""));
        }
    }
}