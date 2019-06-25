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

using Jib.Net.Core.Api;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link DockerDaemonImage}. */

    public class DockerDaemonImageTest
    {
        [Test]
        public void testGetters_default()
        {
            DockerDaemonImage dockerDaemonImage = DockerDaemonImage.named("docker/daemon/image");

            Assert.AreEqual("docker/daemon/image", dockerDaemonImage.getImageReference().toString());
            Assert.AreEqual(Option.empty<SystemPath>(), dockerDaemonImage.getDockerExecutable());
            Assert.AreEqual(0, dockerDaemonImage.getDockerEnvironment().size());
        }

        [Test]
        public void testGetters()
        {
            DockerDaemonImage dockerDaemonImage =
                DockerDaemonImage.named("docker/daemon/image")
                    .setDockerExecutable(Paths.get("docker/binary"))
                    .setDockerEnvironment(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["key"] = "value" }));

            Assert.AreEqual(Paths.get("docker/binary"), dockerDaemonImage.getDockerExecutable().get());
            Assert.AreEqual(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["key"] = "value" }), dockerDaemonImage.getDockerEnvironment());
        }
    }
}