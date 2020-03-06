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

using Fib.Net.Core.Api;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Fib.Net.Core.Configuration
{
    /** Immutable configuration options for an image reference with credentials. */
    public sealed class ImageConfiguration
    {
        /** Builder for instantiating an {@link ImageConfiguration}. */
        public class Builder
        {
            private readonly IImageReference imageReference;
            private ImmutableArray<CredentialRetriever> credentialRetrievers = ImmutableArray.Create<CredentialRetriever>();

            /**
             * Sets the providers for registry credentials. The order determines the priority in which the
             * retrieval methods are attempted.
             *
             * @param credentialRetrievers the list of {@link CredentialRetriever}s
             * @return this
             */
            public Builder SetCredentialRetrievers(IList<CredentialRetriever> credentialRetrievers)
            {
                credentialRetrievers = credentialRetrievers ?? throw new ArgumentNullException(nameof(credentialRetrievers));
                Preconditions.CheckArgument(
                    !credentialRetrievers.Contains(null), "credential retriever list contains null elements");
                this.credentialRetrievers = ImmutableArray.CreateRange(credentialRetrievers);
                return this;
            }

            /**
             * Builds the {@link ImageConfiguration}.
             *
             * @return the corresponding {@link ImageConfiguration}
             */
            public ImageConfiguration Build()
            {
                return new ImageConfiguration(imageReference, credentialRetrievers);
            }

            public Builder(IImageReference imageReference)
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
        public static Builder CreateBuilder(IImageReference imageReference)
        {
            return new Builder(imageReference);
        }

        private readonly IImageReference image;
        private readonly ImmutableArray<CredentialRetriever> credentialRetrievers;

        private ImageConfiguration(
            IImageReference image, ImmutableArray<CredentialRetriever> credentialRetrievers)
        {
            this.image = image;
            this.credentialRetrievers = credentialRetrievers;
        }

        public IImageReference GetImage()
        {
            return image;
        }

        public string GetImageRegistry()
        {
            return image.GetRegistry();
        }

        public string GetImageRepository()
        {
            return image.GetRepository();
        }

        public string GetImageTag()
        {
            return image.GetTag();
        }

        public ImmutableArray<CredentialRetriever> GetCredentialRetrievers()
        {
            return credentialRetrievers;
        }
    }
}
