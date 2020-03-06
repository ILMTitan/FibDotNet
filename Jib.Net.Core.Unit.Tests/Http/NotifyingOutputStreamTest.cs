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

using NUnit.Framework;
using Jib.Net.Core.Global;
using System.Collections.Generic;
using System.IO;
using NodaTime;
using Jib.Net.Core.Events.Progress;
using Jib.Net.Core.Http;

namespace Jib.Net.Core.Unit.Tests.Http
{
    /** Tests for {@link NotifyingOutputStream}. */
    public class NotifyingOutputStreamTest
    {
        [Test]
        public void TestCallback_correctSequence()
        {
            MemoryStream byteArrayOutputStream = new MemoryStream();

            List<long> byteCounts = new List<long>();

            using (NotifyingOutputStream notifyingOutputStream =
                new NotifyingOutputStream(byteArrayOutputStream, byteCounts.Add))
            {
                byte[] firstBytes = new byte[] { 0 };
                notifyingOutputStream.Write(firstBytes);
                byte[] secondBites = new byte[] { 1, 2, 3 };
                notifyingOutputStream.Write(secondBites, 0, secondBites.Length);
                notifyingOutputStream.Write(new byte[] { 1, 2, 3, 4, 5 }, 3, 2);
            }

            Assert.AreEqual(new[] { 1L, 3L, 2L }, byteCounts);
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 3, 4, 5 }, byteArrayOutputStream.ToArray());
        }

        [Test]
        public void TestDelay()
        {
            MemoryStream byteArrayOutputStream = new MemoryStream();

            IList<long> byteCounts = new List<long>();

            Queue<Instant> instantQueue = new Queue<Instant>();
            instantQueue.Enqueue(Instant.FromUnixTimeSeconds(0));

            using (ThrottledAccumulatingConsumer byteCounter =
                    new ThrottledAccumulatingConsumer(
                        byteCounts.Add, Duration.FromSeconds(3), instantQueue.Dequeue))
            using (NotifyingOutputStream notifyingOutputStream =
                    new NotifyingOutputStream(byteArrayOutputStream, byteCounter.Accept))

            {
                instantQueue.Enqueue(Instant.FromUnixTimeSeconds(0));
                notifyingOutputStream.WriteByte(100);
                instantQueue.Enqueue(Instant.FromUnixTimeSeconds(0));
                notifyingOutputStream.Write(new byte[] { 101, 102, 103 });
                instantQueue.Enqueue(Instant.FromUnixTimeSeconds(0) + Duration.FromSeconds(4));
                notifyingOutputStream.Write(new byte[] { 104, 105, 106 });

                instantQueue.Enqueue(Instant.FromUnixTimeSeconds(0) + Duration.FromSeconds(10));
                notifyingOutputStream.Write(new byte[] { 107, 108 });

                instantQueue.Enqueue(Instant.FromUnixTimeSeconds(0) + Duration.FromSeconds(10));
                notifyingOutputStream.Write(new byte[] { 109 });
                instantQueue.Enqueue(Instant.FromUnixTimeSeconds(0) + Duration.FromSeconds(13));
                notifyingOutputStream.Write(new byte[] { 0, 110 }, 1, 1);
            }

            Assert.AreEqual(new[] { 7L, 2L, 2L }, byteCounts);
            CollectionAssert.AreEqual(
                new byte[] { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110 },
                byteArrayOutputStream.ToArray());
        }
    }
}
