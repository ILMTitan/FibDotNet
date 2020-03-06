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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fib.Net.Core.Hash
{
    internal static class ByteStreams
    {
        internal static async Task CopyAsync(Stream inStream, Stream contentsOut)
        {
            await inStream.CopyToAsync(contentsOut).ConfigureAwait(false);
        }

        internal static byte[] ToByteArray(Stream stream)
        {
            return ReadBytes(stream).ToArray();

            IEnumerable<byte> ReadBytes(Stream s)
            {
                int b;
                while ((b = s.ReadByte()) >= 0)
                {
                    yield return (byte)b;
                }
            }
        }
    }
}