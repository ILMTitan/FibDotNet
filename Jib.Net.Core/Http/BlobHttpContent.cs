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

namespace com.google.cloud.tools.jib.http {






/** {@link Blob}-backed {@link HttpContent}. */
public class BlobHttpContent : HttpContent {

  private readonly Blob blob;
  private readonly string contentType;
  private readonly Consumer<Long> writtenByteCountListener;

  public BlobHttpContent(Blob blob, string contentType) {
    this(blob, contentType, ignored => {});
  }

  public BlobHttpContent(Blob blob, string contentType, Consumer<Long> writtenByteCountListener) {
    this.blob = blob;
    this.contentType = contentType;
    this.writtenByteCountListener = writtenByteCountListener;
  }

  public long getLength() {
    // Returns negative value for unknown length.
    return -1;
  }

  public string getType() {
    return contentType;
  }

  public bool retrySupported() {
    return false;
  }

  public void writeTo(OutputStream outputStream) {
    blob.writeTo(new NotifyingOutputStream(outputStream, writtenByteCountListener));
    outputStream.flush();
  }
}
}
