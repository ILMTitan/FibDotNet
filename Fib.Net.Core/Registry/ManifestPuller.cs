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

using Fib.Net.Core.Http;
using Fib.Net.Core.Images.Json;
using Fib.Net.Core.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fib.Net.Core.Registry
{
    internal class ManifestPuller : ManifestPuller<IManifestTemplate>
    {
        public ManifestPuller(RegistryEndpointRequestProperties registryEndpointRequestProperties, string imageTag) : base(registryEndpointRequestProperties, imageTag)
        {
        }

        protected override IManifestTemplate GetManifestTemplateFromJson(string jsonString)
        {
            var token = JToken.Parse(jsonString);
            if (!(token is JObject obj))
            {
                throw new UnknownManifestFormatException(Resources.ManifestPullerNotJsonExceptionMessage);
            }
            if (!obj.ContainsKey("schemaVersion"))
            {
                throw new UnknownManifestFormatException(Resources.ManifestPullerMissingSchemaVersionExceptionMessage);
            }
            if (!obj.TryGetValue("schemaVersion", out JToken schemaVersionToken) || schemaVersionToken.Type != JTokenType.Integer)
            {
                throw new UnknownManifestFormatException(Resources.ManifestPullerSchemaVersionNotIntExceptionMessage);
            }
            int schemaVersion = schemaVersionToken.Value<int>();
            if (schemaVersion == 1)
            {
                return JsonTemplateMapper.ReadJson<V21ManifestTemplate>(jsonString);
            }
            if (schemaVersion == 2)
            {
                // 'schemaVersion' of 2 can be either Docker V2.2 or OCI.
                string mediaType = obj.Value<string>("mediaType");
                if (V22ManifestTemplate.ManifestMediaType == mediaType)
                {
                    return JsonTemplateMapper.ReadJson<V22ManifestTemplate>(jsonString);
                }
                if (OCIManifestTemplate.ManifestMediaType == mediaType)
                {
                    return JsonTemplateMapper.ReadJson<OCIManifestTemplate>(jsonString);
                }
                throw new UnknownManifestFormatException("Unknown mediaType: " + mediaType);
            }
            throw new UnknownManifestFormatException(
                "Unknown schemaVersion: " + schemaVersion + " - only 1 and 2 are supported");
        }
    }

    /** Pulls an image's manifest. */
    internal class ManifestPuller<T> : RegistryEndpointProvider<T> where T : IManifestTemplate
    {
        private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
        private readonly string imageTag;

        public ManifestPuller(
            RegistryEndpointRequestProperties registryEndpointRequestProperties,
            string imageTag)
        {
            this.registryEndpointRequestProperties = registryEndpointRequestProperties;
            this.imageTag = imageTag;
        }

        public BlobHttpContent GetContent()
        {
            return null;
        }

        public IList<string> GetAccept()
        {
            if (typeof(T) == typeof(OCIManifestTemplate))
            {
                return new[] { OCIManifestTemplate.ManifestMediaType };
            }
            if (typeof(T) == typeof(V22ManifestTemplate))
            {
                return new[] { V22ManifestTemplate.ManifestMediaType };
            }
            if (typeof(T) == typeof(V21ManifestTemplate))
            {
                return new[] { V21ManifestTemplate.ManifestMediaType };
            }
            return new[]{
                OCIManifestTemplate.ManifestMediaType,
                V22ManifestTemplate.ManifestMediaType,
                V21ManifestTemplate.ManifestMediaType
            };
        }

        /** Parses the response body into a {@link ManifestTemplate}. */

        public async Task<T> HandleResponseAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                string jsonString;
                using (StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), Encoding.UTF8))
                {
                    jsonString = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
                return GetManifestTemplateFromJson(jsonString);
            }
            else
            {
                throw new HttpResponseException(response);
            }
        }

        public Uri GetApiRoute(string apiRouteBase)
        {
            return new Uri(
                apiRouteBase + registryEndpointRequestProperties.GetImageName() + "/manifests/" + imageTag);
        }

        public HttpMethod GetHttpMethod()
        {
            return HttpMethod.Get;
        }

        public string GetActionDescription()
        {
            return "pull image manifest for "
                + registryEndpointRequestProperties.GetRegistry()
                + "/"
                + registryEndpointRequestProperties.GetImageName()
                + ":"
                + imageTag;
        }

        /**
         * Instantiates a {@link ManifestTemplate} from a JSON string. This checks the {@code
         * schemaVersion} field of the JSON to determine which manifest version to use.
         */
        protected virtual T GetManifestTemplateFromJson(string jsonString)
        {
            return JsonTemplateMapper.ReadJson<T>(jsonString);
        }
    }
}
