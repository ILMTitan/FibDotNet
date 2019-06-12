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

using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Global;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace com.google.cloud.tools.jib.configuration
{
    /** Immutable configuration options for an image reference with credentials. */
    public sealed class ImageConfiguration
    {
        /** Builder for instantiating an {@link ImageConfiguration}. */
        public class Builder
        {
            private ImageReference imageReference;
            private ImmutableArray<CredentialRetriever> credentialRetrievers = ImmutableArray.Create<CredentialRetriever>();

            /**
             * Sets the providers for registry credentials. The order determines the priority in which the
             * retrieval methods are attempted.
             *
             * @param credentialRetrievers the list of {@link CredentialRetriever}s
             * @return this
             */
            public Builder setCredentialRetrievers(IList<CredentialRetriever> credentialRetrievers)
            {
                Preconditions.checkArgument(
                    !credentialRetrievers.contains(null), "credential retriever list contains null elements");
                this.credentialRetrievers = ImmutableArray.CreateRange(credentialRetrievers);
                return this;
            }

            /**
             * Builds the {@link ImageConfiguration}.
             *
             * @return the corresponding {@link ImageConfiguration}
             */
            public ImageConfiguration build()
            {
                return new ImageConfiguration(imageReference, credentialRetrievers);
            }

            public Builder(ImageReference imageReference)
            {
                this.imageReference = imageReference;
            }
        }

        /**
         * Constructs a builder for an {@link ImageConfiguration}.
         *
         * @param imageReference the image reference, which is a required field
         * @return the builder
         */
        public static Builder builder(ImageReference imageReference)
        {
            return new Builder(imageReference);
        }

        private readonly ImageReference image;
        private readonly ImmutableArray<CredentialRetriever> credentialRetrievers;

        private ImageConfiguration(
            ImageReference image, ImmutableArray<CredentialRetriever> credentialRetrievers)
        {
            this.image = image;
            this.credentialRetrievers = credentialRetrievers;
        }

        public ImageReference getImage()
        {
            return image;
        }

        public string getImageRegistry()
        {
            return image.getRegistry();
        }

        public string getImageRepository()
        {
            return image.getRepository();
        }

        public string getImageTag()
        {
            return image.getTag();
        }

        public ImmutableArray<CredentialRetriever> getCredentialRetrievers()
        {
            return credentialRetrievers;
        }
    }
}
