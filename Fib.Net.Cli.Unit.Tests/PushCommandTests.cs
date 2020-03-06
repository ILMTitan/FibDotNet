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

using Fib.Net.Core.Api;
using NUnit.Framework;

namespace Fib.Net.Cli.Unit.Tests
{
    public class PushCommandTests
    {
        private class TestablePushCommand : PushCommand
        {
            public new IContainerizer CreateContainerizer(ImageReference imageReference)
            {
                return base.CreateContainerizer(imageReference);
            }
        }

        [Test]
        public void TestCreateContainerizer_CreatesForDockerClient()
        {
            var result = new TestablePushCommand().CreateContainerizer(ImageReference.Parse("image-reference"));
            Assert.AreEqual(Containerizer.DescriptionForDockerRegistry, result.GetDescription());
        }
    }
}
