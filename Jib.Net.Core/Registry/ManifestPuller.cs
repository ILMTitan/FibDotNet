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

using com.google.cloud.tools.jib.docker;
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.image.json;
using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry;
using Jib.Net.Core.Global;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jib.Net.Core.Registry
{
    internal class ManifestPuller : ManifestPuller<IManifestTemplate>
    {
        public ManifestPuller(RegistryEndpointRequestProperties registryEndpointRequestProperties, string imageTag) : base(registryEndpointRequestProperties, imageTag)
        {
        }

        protected override IManifestTemplate getManifestTemplateFromJson(string jsonString)
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
                return JsonTemplateMapper.readJson<V21ManifestTemplate>(jsonString);
            }
            if (schemaVersion == 2)
            {
                // 'schemaVersion' of 2 can be either Docker V2.2 or OCI.
                string mediaType = obj.Value<string>("mediaType");
                if (V22ManifestTemplate.MANIFEST_MEDIA_TYPE == mediaType)
                {
                    return JsonTemplateMapper.readJson<V22ManifestTemplate>(jsonString);
                }
                if (OCIManifestTemplate.MANIFEST_MEDIA_TYPE == mediaType)
                {
                    return JsonTemplateMapper.readJson<OCIManifestTemplate>(jsonString);
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

        public BlobHttpContent getContent()
        {
            return null;
        }

        public IList<string> getAccept()
        {
            if (typeof(T) == typeof(OCIManifestTemplate))
            {
                return new[] { OCIManifestTemplate.MANIFEST_MEDIA_TYPE };
            }
            if (typeof(T) == typeof(V22ManifestTemplate))
            {
                return new[] { V22ManifestTemplate.MANIFEST_MEDIA_TYPE };
            }
            if (typeof(T) == typeof(V21ManifestTemplate))
            {
                return new[] { V21ManifestTemplate.MEDIA_TYPE };
            }
            return Arrays.asList(
                OCIManifestTemplate.MANIFEST_MEDIA_TYPE,
                V22ManifestTemplate.MANIFEST_MEDIA_TYPE,
                V21ManifestTemplate.MEDIA_TYPE);
        }

        /** Parses the response body into a {@link ManifestTemplate}. */

        public async Task<T> handleResponseAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                string jsonString;
                using (StreamReader reader = new StreamReader(await response.getBodyAsync().ConfigureAwait(false), StandardCharsets.UTF_8))
                {
                    jsonString = CharStreams.toString(reader);
                }
                return getManifestTemplateFromJson(jsonString);
            }
            else
            {
                throw new HttpResponseException(response);
            }
        }

        public Uri getApiRoute(string apiRouteBase)
        {
            return new Uri(
                apiRouteBase + registryEndpointRequestProperties.getImageName() + "/manifests/" + imageTag);
        }

        public HttpMethod getHttpMethod()
        {
            return HttpMethod.Get;
        }

        public string getActionDescription()
        {
            return "pull image manifest for "
                + registryEndpointRequestProperties.getRegistry()
                + "/"
                + registryEndpointRequestProperties.getImageName()
                + ":"
                + imageTag;
        }

        /**
         * Instantiates a {@link ManifestTemplate} from a JSON string. This checks the {@code
         * schemaVersion} field of the JSON to determine which manifest version to use.
         */
        protected virtual T getManifestTemplateFromJson(string jsonString)
        {
            return JsonTemplateMapper.readJson<T>(jsonString);
        }
    }
}
