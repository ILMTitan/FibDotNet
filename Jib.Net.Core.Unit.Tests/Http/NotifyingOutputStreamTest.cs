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
using com.google.cloud.tools.jib.@event.progress;

namespace com.google.cloud.tools.jib.http
{



    /** Tests for {@link NotifyingOutputStream}. */
    public class NotifyingOutputStreamTest
    {
        [Test]
        public void testCallback_correctSequence()
        {
            MemoryStream byteArrayOutputStream = new MemoryStream();

            List<long> byteCounts = new List<long>();

            using (NotifyingOutputStream notifyingOutputStream =
                new NotifyingOutputStream(byteArrayOutputStream, byteCounts.add))
            {
                notifyingOutputStream.write(new byte[] { 0 });
                notifyingOutputStream.write(new byte[] { 1, 2, 3 });
                notifyingOutputStream.Write(new byte[] { 1, 2, 3, 4, 5 }, 3, 2);
            }

            Assert.AreEqual(Arrays.asList(1L, 3L, 2L), byteCounts);
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 3, 4, 5 }, byteArrayOutputStream.toByteArray());
        }

        [Test]
        public void testDelay()
        {
            MemoryStream byteArrayOutputStream = new MemoryStream();

            IList<long> byteCounts = new List<long>();

            Queue<Instant> instantQueue = new Queue<Instant>();
            instantQueue.add(Instant.FromUnixTimeSeconds(0));

            using (ThrottledAccumulatingConsumer byteCounter =
                    new ThrottledAccumulatingConsumer(
                        byteCounts.add, Duration.FromSeconds(3), instantQueue.remove))
            using (NotifyingOutputStream notifyingOutputStream =
                    new NotifyingOutputStream(byteArrayOutputStream, byteCounter))

            {
                instantQueue.add(Instant.FromUnixTimeSeconds(0));
                notifyingOutputStream.write(100);
                instantQueue.add(Instant.FromUnixTimeSeconds(0));
                notifyingOutputStream.write(new byte[] { 101, 102, 103 });
                instantQueue.add(Instant.FromUnixTimeSeconds(0).plusSeconds(4));
                notifyingOutputStream.write(new byte[] { 104, 105, 106 });

                instantQueue.add(Instant.FromUnixTimeSeconds(0).plusSeconds(10));
                notifyingOutputStream.write(new byte[] { 107, 108 });

                instantQueue.add(Instant.FromUnixTimeSeconds(0).plusSeconds(10));
                notifyingOutputStream.write(new byte[] { 109 });
                instantQueue.add(Instant.FromUnixTimeSeconds(0).plusSeconds(13));
                notifyingOutputStream.write(new byte[] { 0, 110 }, 1, 1);
            }

            Assert.AreEqual(Arrays.asList(7L, 2L, 2L), byteCounts);
            CollectionAssert.AreEqual(
                new byte[] { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110 },
                byteArrayOutputStream.toByteArray());
        }
    }
}
