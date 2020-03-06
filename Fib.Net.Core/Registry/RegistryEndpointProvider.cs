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

using Fib.Net.Core.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fib.Net.Core.Registry
{
    /**
     * Provides implementations for a registry endpoint. Implementations should be immutable.
     *
     * @param <T> the type returned from handling the endpoint response
     */
    internal interface RegistryEndpointProvider<T>
    {
        /** @return the HTTP method to send the request with */
        HttpMethod GetHttpMethod();

        /**
         * @param apiRouteBase the registry's base Uri (for example, {@code https://gcr.io/v2/})
         * @return the registry endpoint Uri
         */
        Uri GetApiRoute(string apiRouteBase);

        /** @return the {@link BlobHttpContent} to send as the request body */
        BlobHttpContent GetContent();

        /** @return a list of MIME types to pass as an HTTP {@code Accept} header */
        IList<string> GetAccept();

        /** Handles the response specific to the registry action. */
        Task<T> HandleResponseAsync(HttpResponseMessage response);
        string GetActionDescription();
    }
}
