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

using Fib.Net.Core.Registry;
using System.Net.Http;
using System;
using System.Runtime.Serialization;

namespace Fib.Net.Core.Api
{
    /** Thrown when a registry request was unauthorized and therefore authentication is needed. */
    [Serializable]
    public class RegistryUnauthorizedException : RegistryException
    {
        private readonly string registry;
        private readonly string repository;

        /**
         * Identifies the image registry and repository that denied access.
         *
         * @param registry the image registry
         * @param repository the image repository
         * @param cause the cause
         */
        public RegistryUnauthorizedException(string registry, string repository, HttpResponseMessage cause) : base("Unauthorized for " + registry + "/" + repository, cause)
        {
            this.registry = registry;
            this.repository = repository;
        }

        /**
         * Identifies the image registry and repository that denied access.
         *
         * @param registry the image registry
         * @param repository the image repository
         * @param cause the cause
         */
        public RegistryUnauthorizedException(string registry, string repository, HttpResponseException cause)
            : base("Unauthorized for " + registry + "/" + repository, cause?.Cause)
        {
            this.registry = registry;
            this.repository = repository;
        }

        protected RegistryUnauthorizedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public string GetRegistry()
        {
            return registry;
        }

        public string GetRepository()
        {
            return repository;
        }

        public string GetImageReference()
        {
            return registry + "/" + repository;
        }

        public HttpResponseMessage GetHttpResponse()
        {
            return Cause;
        }
    }
}
