// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using Fib.Net.Core.Api;
using Fib.Net.Core.FileSystem;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Fib.Net.Core.Unit.Tests.Api
{
    /** Tests for {@link DockerDaemonImage}. */

    public class DockerDaemonImageTest
    {
        [Test]
        public void TestGetters_default()
        {
            DockerDaemonImage dockerDaemonImage = DockerDaemonImage.Named("docker/daemon/image");

            Assert.AreEqual("docker/daemon/image", dockerDaemonImage.GetImageReference().ToString());
            Assert.AreEqual(Maybe.Empty<SystemPath>(), dockerDaemonImage.GetDockerExecutable());
            Assert.AreEqual(0, dockerDaemonImage.GetDockerEnvironment().Count);
        }

        [Test]
        public void TestGetters()
        {
            DockerDaemonImage dockerDaemonImage =
                DockerDaemonImage.Named("docker/daemon/image")
                    .SetDockerExecutable(Paths.Get("docker/binary"))
                    .SetDockerEnvironment(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["key"] = "value" }));

            Assert.AreEqual(Paths.Get("docker/binary"), dockerDaemonImage.GetDockerExecutable().Get());
            Assert.AreEqual(ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["key"] = "value" }), dockerDaemonImage.GetDockerEnvironment());
        }
    }
}