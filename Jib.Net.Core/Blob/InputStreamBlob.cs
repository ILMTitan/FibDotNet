/*
 * Copyright 2017 Google LLC.
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

using com.google.cloud.tools.jib.hash;
using Jib.Net.Core.Blob;
using System;
using System.IO;

namespace com.google.cloud.tools.jib.blob
{
    /** A {@link Blob} that holds an {@link InputStream}. */
    internal class InputStreamBlob : Blob
    {
        private readonly Stream inputStream;

        /** Indicates if the {@link Blob} has already been written or not. */
        private bool isWritten = false;

        public InputStreamBlob(Stream inputStream)
        {
            this.inputStream = inputStream;
        }

        public BlobDescriptor writeTo(Stream outputStream)
        {
            // Cannot rewrite.
            if (isWritten)
            {
                throw new InvalidOperationException("Cannot rewrite Blob backed by an InputStream");
            }
            try
            {
                using (Stream inputStream = this.inputStream)
                {
                    return Digests.computeDigest(inputStream, outputStream);
                }
            }
            finally
            {
                isWritten = true;
            }
        }
    }
}
