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

using com.google.cloud.tools.jib.registry;
using System.Net.Http;

namespace com.google.cloud.tools.jib.api {


/** Thrown when a registry request was unauthorized and therefore authentication is needed. */
public class RegistryUnauthorizedException : RegistryException {

  private readonly string registry;
  private readonly string repository;

        public HttpResponseMessage Cause { get; }

        /**
         * Identifies the image registry and repository that denied access.
         *
         * @param registry the image registry
         * @param repository the image repository
         * @param cause the cause
         */
        public RegistryUnauthorizedException(
      string registry, string repository, HttpResponseMessage cause) : base("Unauthorized for " + registry + "/" + repository) {
    
    this.registry = registry;
    this.repository = repository;
            Cause = cause;
        }

        /**
         * Identifies the image registry and repository that denied access.
         *
         * @param registry the image registry
         * @param repository the image repository
         * @param cause the cause
         */
        public RegistryUnauthorizedException(
      string registry, string repository, HttpResponseException cause) : base("Unauthorized for " + registry + "/" + repository)
        {

            this.registry = registry;
            this.repository = repository;
            Cause = cause.Cause;
        }

        public RegistryUnauthorizedException(string message, System.Exception cause) : base(message, cause)
        {
        }

        public RegistryUnauthorizedException(string message) : base(message)
        {
        }

        public RegistryUnauthorizedException(string message, HttpResponseMessage cause) : base(message, cause)
        {
        }

        public RegistryUnauthorizedException() : base()
        {
        }

        public string getRegistry() {
    return registry;
  }

  public string getRepository() {
    return repository;
  }

  public string getImageReference() {
    return registry + "/" + repository;
  }

  public HttpResponseMessage getHttpResponse() {
            return Cause;
  }
}
}
