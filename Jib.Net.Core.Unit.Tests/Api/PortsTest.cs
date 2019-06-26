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
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link Ports}. */

    public class PortsTest
    {
        [Test]
        public void TestParse()
        {
            IList<string> goodInputs =
                Arrays.AsList("1000", "2000-2003", "3000-3000", "4000/tcp", "5000/udp", "6000-6002/udp");
            ImmutableHashSet<Port> expected =
                ImmutableHashSet.CreateBuilder<Port>()
                    .Add(
                        Port.Tcp(1000),
                        Port.Tcp(2000),
                        Port.Tcp(2001),
                        Port.Tcp(2002),
                        Port.Tcp(2003),
                        Port.Tcp(3000),
                        Port.Tcp(4000),
                        Port.Udp(5000),
                        Port.Udp(6000),
                        Port.Udp(6001),
                        Port.Udp(6002))
                    .Build();
            ImmutableHashSet<Port> result = Ports.Parse(goodInputs);
            Assert.AreEqual(expected, result);

            IList<string> badInputs = Arrays.AsList("abc", "/udp", "1000/abc", "a100/tcp", "20/udpabc");
            foreach (string input in badInputs)
            {
                try
                {
                    Ports.Parse(new List<string> { input });
                    Assert.Fail(input);
                }
                catch (FormatException ex)
                {
                    Assert.AreEqual(
                        "Invalid port configuration: '"
                            + input
                            + "'. Make sure the port is a single number or a range of two numbers separated "
                            + "with a '-', with or without protocol specified (e.g. '<portNum>/tcp' or "
                            + "'<portNum>/udp').",
                        ex.GetMessage());
                }
            }

            try
            {
                Ports.Parse(new List<string> { "4002-4000" });
                Assert.Fail();
            }
            catch (FormatException ex)
            {
                Assert.AreEqual(
                    "Invalid port range '4002-4000'; smaller number must come first.", ex.GetMessage());
            }

            badInputs = Arrays.AsList("0", "70000", "0-400", "1-70000");
            foreach (string input in badInputs)
            {
                try
                {
                    Ports.Parse(new List<string> { input });
                    Assert.Fail();
                }
                catch (FormatException ex)
                {
                    Assert.AreEqual(
                        "Port number '" + input + "' is out of usual range (1-65535).", ex.GetMessage());
                }
            }
        }
    }
}