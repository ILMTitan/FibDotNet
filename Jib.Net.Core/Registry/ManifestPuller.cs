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
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace com.google.cloud.tools.jib.registry
{






















    /** Pulls an image's manifest. */
    class ManifestPuller<T> : RegistryEndpointProvider<T> where T : ManifestTemplate
    {

  private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
  private readonly string imageTag;

  public ManifestPuller(
      RegistryEndpointRequestProperties registryEndpointRequestProperties,
      string imageTag) {
    this.registryEndpointRequestProperties = registryEndpointRequestProperties;
    this.imageTag = imageTag;
  }

  public BlobHttpContent getContent() {
    return null;
  }

  public IList<string> getAccept() {
    return Arrays.asList(
        OCIManifestTemplate.MANIFEST_MEDIA_TYPE,
        V22ManifestTemplate.MANIFEST_MEDIA_TYPE,
        V21ManifestTemplate.MEDIA_TYPE);
  }

  /** Parses the response body into a {@link ManifestTemplate}. */

  public T handleResponse(HttpResponseMessage response) {
    string jsonString =
        CharStreams.toString(new StreamReader(response.getBody(), StandardCharsets.UTF_8));
    return getManifestTemplateFromJson(jsonString);
  }

  public Uri getApiRoute(string apiRouteBase) {
    return new Uri(
        apiRouteBase + registryEndpointRequestProperties.getImageName() + "/manifests/" + imageTag);
  }

  public HttpMethod getHttpMethod() {
    return HttpMethod.Get;
  }

  public string getActionDescription() {
    return "pull image manifest for "
        + registryEndpointRequestProperties.getServerUrl()
        + "/"
        + registryEndpointRequestProperties.getImageName()
        + ":"
        + imageTag;
  }

  /**
   * Instantiates a {@link ManifestTemplate} from a JSON string. This checks the {@code
   * schemaVersion} field of the JSON to determine which manifest version to use.
   */
  private T getManifestTemplateFromJson(string jsonString)
      {
    ObjectNode node = new ObjectMapper().readValue<ObjectNode>(jsonString);
    if (!node.has("schemaVersion")) {
      throw new UnknownManifestFormatException("Cannot find field 'schemaVersion' in manifest");
    }

    if (!typeof(ManifestTemplate).IsAssignableFrom(typeof(T))) {
      return (T)JsonTemplateMapper.readJson<ManifestTemplate>(jsonString);
    }

    int schemaVersion = node.get("schemaVersion").asInt(-1);
    if (schemaVersion == -1) {
      throw new UnknownManifestFormatException("`schemaVersion` field is not an integer");
    }

    if (schemaVersion == 1) {
      return JsonTemplateMapper.readJson<T>(jsonString);
    }
    if (schemaVersion == 2) {
      // 'schemaVersion' of 2 can be either Docker V2.2 or OCI.
      string mediaType = node.get("mediaType").asText();
      if (V22ManifestTemplate.MANIFEST_MEDIA_TYPE.Equals(mediaType)) {
        return (T)JsonTemplateMapper.readJson<T>(jsonString);
      }
      if (OCIManifestTemplate.MANIFEST_MEDIA_TYPE.Equals(mediaType)) {
        return (T)JsonTemplateMapper.readJson<T>(jsonString);
      }
      throw new UnknownManifestFormatException("Unknown mediaType: " + mediaType);
    }
    throw new UnknownManifestFormatException(
        "Unknown schemaVersion: " + schemaVersion + " - only 1 and 2 are supported");
  }
}
}
