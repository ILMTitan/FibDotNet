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

namespace Fib.Net.Core.Registry
{
    /**
     * Enumerated errors that can be received from the Registry API.
     *
     * <p>Descriptions are from:
     *
     * @see <a
     *     href="https://docs.docker.com/registry/spec/api/#errors-2">https://docs.docker.com/registry/spec/api/#errors-2</a>
     */
    public enum ErrorCode
    {

        Unknown = 0,

        /**
         * This error may be returned when a blob is unknown to the registry in a specified repository.
         * This can be returned with a standard get or if a manifest references an unknown layer during
         * upload.
         */
        BlobUnknown,

        /** The blob upload encountered an error and can no longer proceed. */
        BlobUploadInvalid,

        /** If a blob upload has been cancelled or was never started, this error code may be returned. */
        BlobUploadUnknown,

        /**
         * When a blob is uploaded, the registry will check that the content matches the digest provided
         * by the client. The error may include a detail structure with the key "digest", including the
         * invalid digest string. This error may also be returned when a manifest includes an invalid
         * layer digest.
         */
        DigestInvalid,

        /** This error may be returned when a manifest blob is unknown to the registry. */
        ManifestBlobUnknown,

        /**
         * During upload, manifests undergo several checks ensuring validity. If those checks fail, this
         * error may be returned, unless a more specific error is included. The detail will contain
         * information the failed validation.
         */
        ManifestInvalid,

        /**
         * This error is returned when the manifest, identified by name and tag is unknown to the
         * repository.
         */
        ManifestUnknown,

        /**
         * During manifest upload, if the manifest fails signature verification, this error will be
         * returned.
         */
        ManifestUnverified,

        /** Invalid repository name encountered either during manifest validation or any API operation. */
        NameInvalid,

        /** This is returned if the name used during an operation is unknown to the registry. */
        NameUnknown,

        /**
         * When a layer is uploaded, the provided size will be checked against the uploaded content. If
         * they do not match, this error will be returned.
         */
        SizeInvalid,

        /**
         * During a manifest upload, if the tag in the manifest does not match the uri tag, this error
         * will be returned.
         */
        TagInvalid,

        /**
         * The access controller was unable to authenticate the client. Often this will be accompanied by
         * a Www-Authenticate HTTP response header indicating how to authenticate.
         */
        Unauthorized,

        /** The access controller denied access for the operation on a resource. */
        Denied,

        /** The operation was unsupported due to a missing implementation or invalid set of parameters. */
        Unsupported
    }
}
