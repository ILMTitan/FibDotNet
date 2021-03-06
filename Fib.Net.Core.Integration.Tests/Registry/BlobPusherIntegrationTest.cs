// Copyright 2017 Google LLC.
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
using Fib.Net.Core.Blob;
using Fib.Net.Core.Configuration;
using Fib.Net.Core.Registry;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Fib.Net.Core.Integration.Tests.Registry
{
    /** Integration tests for {@link BlobPusher}. */
    public class BlobPusherIntegrationTest : HttpRegistryTest
    {
        [Test]
        public async Task TestPushAsync()
        {
            localRegistry.PullAndPushToLocal("busybox", "busybox");
            IBlob testBlob = Blobs.From("crepecake");
            // Known digest for 'crepecake'
            DescriptorDigest testBlobDigest =
                DescriptorDigest.FromHash(
                    "52a9e4d4ba4333ce593707f98564fee1e6d898db0d3602408c0b2a6a424d357c");

            RegistryClient registryClient =
                RegistryClient.CreateFactory(EventHandlers.NONE, "localhost:5000", "testimage")
                    .SetAllowInsecureRegistries(true)
                    .NewRegistryClient();
            Assert.IsFalse(await registryClient.PushBlobAsync(testBlobDigest, testBlob, null, _ => { }).ConfigureAwait(false));
        }
    }
}
