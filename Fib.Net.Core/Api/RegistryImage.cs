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

using System.Collections.Generic;

namespace Fib.Net.Core.Api
{
    /**
     * Defines an image on a container registry that can be used as either a source or target image.
     *
     * <p>The registry portion of the image reference determines which registry to the image lives (or
     * should live) on. The repository portion is the namespace within the registry. The tag is a label
     * to easily identify an image among all the images in the repository. See {@link ImageReference}
     * for more details.
     *
     * <p>When configuring credentials (via {@link #addCredential} for example), make sure the
     * credentials are valid push (for using this as a target image) or pull (for using this as a source
     * image) credentials for the repository specified via the image reference.
     */
    public sealed class RegistryImage
    {
        /**
         * Instantiate with the image reference to use.
         *
         * @param imageReference the image reference
         * @return a new {@link RegistryImage}
         */
        public static RegistryImage Named(ImageReference imageReference)
        {
            return new RegistryImage(imageReference);
        }

        /**
         * Instantiate with the image reference to use.
         *
         * @param imageReference the image reference
         * @return a new {@link RegistryImage}
         * @throws InvalidImageReferenceException if {@code imageReference} is not a valid image reference
         */
        public static RegistryImage Named(string imageReference)
        {
            return Named(ImageReference.Parse(imageReference));
        }

        private readonly ImageReference imageReference;
        private readonly IList<CredentialRetriever> credentialRetrievers = new List<CredentialRetriever>();

        /** Instantiate with {@link #named}. */
        private RegistryImage(ImageReference imageReference)
        {
            this.imageReference = imageReference;
        }

        /**
         * Adds a username-password credential to use to push/pull the image. This is a shorthand for
         * {@code addCredentialRetriever(() => Optional.of(Credential.basic(username, password)))}.
         *
         * @param username the username
         * @param password the password
         * @return this
         */
        public RegistryImage AddCredential(string username, string password)
        {
            AddCredentialRetriever(() => Maybe.Of(Credential.From(username, password)));
            return this;
        }

        /**
         * Adds {@link CredentialRetriever} to fetch push/pull credentials for the image. Credential
         * retrievers are attempted in the order in which they are specified until credentials are
         * successfully retrieved.
         *
         * <p>Example usage:
         *
         * <pre>{@code
         * .addCredentialRetriever(() => {
         *   if (!Files.exists("secret.txt") {
         *     return Optional.empty();
         *   }
         *   try {
         *     string password = fetchPasswordFromFile("secret.txt");
         *     return Credential.basic("myaccount", password);
         *
         *   } catch (IOException ex) {
         *     throw new CredentialRetrievalException("Failed to load password", ex);
         *   }
         * })
         * }</pre>
         *
         * @param credentialRetriever the {@link CredentialRetriever} to add
         * @return this
         */
        public RegistryImage AddCredentialRetriever(CredentialRetriever credentialRetriever)
        {
            credentialRetrievers.Add(credentialRetriever);
            return this;
        }

        public ImageReference GetImageReference()
        {
            return imageReference;
        }

        public IList<CredentialRetriever> GetCredentialRetrievers()
        {
            return credentialRetrievers;
        }
    }
}
