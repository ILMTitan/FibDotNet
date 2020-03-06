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

using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Core.Blob
{
    /** Holds a BLOB source for writing to an {@link OutputStream}. */
    public interface IBlob
    {
        /**
         * Writes the BLOB to an {@link OutputStream}. Does not close the {@code innerStream}.
         *
         * @param innerStream the {@link OutputStream} to write to
         * @return the {@link BlobDescriptor} of the written BLOB
         * @throws IOException if writing the BLOB fails
         */
        Task<BlobDescriptor> WriteToAsync(Stream outputStream);

        long Size { get; }
    }
}
