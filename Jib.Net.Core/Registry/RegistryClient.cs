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
using com.google.cloud.tools.jib.blob;
using com.google.cloud.tools.jib.builder;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.global;
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.image.json;
using Jib.Net.Core.Api;
using Jib.Net.Core.Blob;
using Jib.Net.Core.Global;
using System;
using System.IO;
using System.Text;

namespace com.google.cloud.tools.jib.registry {






















/** Interfaces with a registry. */
public class RegistryClient {

  /** Factory for creating {@link RegistryClient}s. */
  public class Factory {

    private readonly EventHandlers eventHandlers;
    private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;

    private bool allowInsecureRegistries = false;
    private string userAgentSuffix;
    private Authorization authorization;

    public Factory(
        EventHandlers eventHandlers,
        RegistryEndpointRequestProperties registryEndpointRequestProperties) {
      this.eventHandlers = eventHandlers;
      this.registryEndpointRequestProperties = registryEndpointRequestProperties;
    }

    /**
     * Sets whether or not to allow insecure registries (ignoring certificate validation failure or
     * communicating over HTTP if all else fail).
     *
     * @param allowInsecureRegistries if {@code true}, insecure connections will be allowed
     * @return this
     */
    public Factory setAllowInsecureRegistries(bool allowInsecureRegistries) {
      this.allowInsecureRegistries = allowInsecureRegistries;
      return this;
    }

    /**
     * Sets the authentication credentials to use to authenticate with the registry.
     *
     * @param authorization the {@link Authorization} to access the registry/repository
     * @return this
     */
    public Factory setAuthorization(Authorization authorization) {
      this.authorization = authorization;
      return this;
    }

    /**
     * Sets a suffix to append to {@code User-Agent} headers.
     *
     * @param userAgentSuffix the suffix to append
     * @return this
     */
    public Factory setUserAgentSuffix(string userAgentSuffix) {
      this.userAgentSuffix = userAgentSuffix;
      return this;
    }

    /**
     * Creates a new {@link RegistryClient}.
     *
     * @return the new {@link RegistryClient}
     */
    public RegistryClient newRegistryClient() {
      return new RegistryClient(
          eventHandlers,
          authorization,
          registryEndpointRequestProperties,
          allowInsecureRegistries,
          makeUserAgent());
    }

    /**
     * The {@code User-Agent} is in the form of {@code jib <version> <type>}. For example: {@code
     * jib 0.9.0 jib-maven-plugin}.
     *
     * @return the {@code User-Agent} header to send. The {@code User-Agent} can be disabled by
     *     setting the system property variable {@code _JIB_DISABLE_USER_AGENT} to any non-empty
     *     string.
     */
    private string makeUserAgent() {
      if (!JibSystemProperties.isUserAgentEnabled()) {
        return "";
      }

      StringBuilder userAgentBuilder = new StringBuilder();
      userAgentBuilder.append("jib");
      userAgentBuilder.append(" ").append(ProjectInfo.VERSION);
      if (userAgentSuffix != null) {
        userAgentBuilder.append(" ").append(userAgentSuffix);
      }
      return userAgentBuilder.toString();
    }
  }

  /**
   * Creates a new {@link Factory} for building a {@link RegistryClient}.
   *
   * @param eventHandlers the event handlers used for dispatching log events
   * @param serverUrl the server Uri for the registry (for example, {@code gcr.io})
   * @param imageName the image/repository name (also known as, namespace)
   * @return the new {@link Factory}
   */
  public static Factory factory(EventHandlers eventHandlers, string serverUrl, string imageName) {
    return new Factory(eventHandlers, new RegistryEndpointRequestProperties(serverUrl, imageName));
  }

  private readonly EventHandlers eventHandlers;
  private readonly Authorization authorization;
  private readonly RegistryEndpointRequestProperties registryEndpointRequestProperties;
  private readonly bool allowInsecureRegistries;
  private readonly string userAgent;

  /**
   * Instantiate with {@link #factory}.
   *
   * @param eventHandlers the event handlers used for dispatching log events
   * @param authorization the {@link Authorization} to access the registry/repository
   * @param registryEndpointRequestProperties properties of registry endpoint requests
   * @param allowInsecureRegistries if {@code true}, insecure connections will be allowed
   */
  private RegistryClient(
      EventHandlers eventHandlers,
      Authorization authorization,
      RegistryEndpointRequestProperties registryEndpointRequestProperties,
      bool allowInsecureRegistries,
      string userAgent) {
    this.eventHandlers = eventHandlers;
    this.authorization = authorization;
    this.registryEndpointRequestProperties = registryEndpointRequestProperties;
    this.allowInsecureRegistries = allowInsecureRegistries;
    this.userAgent = userAgent;
  }

  /**
   * @return the {@link RegistryAuthenticator} to authenticate pulls/pushes with the registry, or
   *     {@code null} if no token authentication is necessary
   * @throws IOException if communicating with the endpoint fails
   * @throws RegistryException if communicating with the endpoint fails
   */
  public RegistryAuthenticator getRegistryAuthenticator() {
    // Gets the WWW-Authenticate header (eg. 'WWW-Authenticate: Bearer
    // realm="https://gcr.io/v2/token",service="gcr.io"')
    return callRegistryEndpoint(
        new AuthenticationMethodRetriever(registryEndpointRequestProperties, getUserAgent()));
  }

  /**
   * Pulls the image manifest for a specific tag.
   *
   * @param <T> child type of ManifestTemplate
   * @param imageTag the tag to pull on
   * @param manifestTemplateClass the specific version of manifest template to pull, or {@link
   *     ManifestTemplate} to pull either {@link V22ManifestTemplate} or {@link V21ManifestTemplate}
   * @return the manifest template
   * @throws IOException if communicating with the endpoint fails
   * @throws RegistryException if communicating with the endpoint fails
   */
  public T pullManifest<T>(
      string imageTag) where T : ManifestTemplate {
    ManifestPuller<T> manifestPuller =
        new ManifestPuller<T>(registryEndpointRequestProperties, imageTag);
    T manifestTemplate = callRegistryEndpoint(manifestPuller);
    if (manifestTemplate == null) {
      throw new InvalidOperationException("ManifestPuller#handleResponse does not return null");
    }
    return manifestTemplate;
  }

  public ManifestTemplate pullManifest(string imageTag) {
    return pullManifest< ManifestTemplate>(imageTag);
  }

  /**
   * Pushes the image manifest for a specific tag.
   *
   * @param manifestTemplate the image manifest
   * @param imageTag the tag to push on
   * @return the digest of the pushed image
   * @throws IOException if communicating with the endpoint fails
   * @throws RegistryException if communicating with the endpoint fails
   */
  public DescriptorDigest pushManifest(BuildableManifestTemplate manifestTemplate, string imageTag)
      {
    return Verify.verifyNotNull(
        callRegistryEndpoint(
            new ManifestPusher(
                registryEndpointRequestProperties, manifestTemplate, imageTag, eventHandlers)));
  }

  /**
   * @param blobDigest the blob digest to check for
   * @return the BLOB's {@link BlobDescriptor} if the BLOB exists on the registry, or {@code null}
   *     if it doesn't
   * @throws IOException if communicating with the endpoint fails
   * @throws RegistryException if communicating with the endpoint fails
   */
  public BlobDescriptor checkBlob(DescriptorDigest blobDigest)
      {
    BlobChecker blobChecker = new BlobChecker(registryEndpointRequestProperties, blobDigest);
    return callRegistryEndpoint(blobChecker);
  }

  /**
   * Gets the BLOB referenced by {@code blobDigest}. Note that the BLOB is only pulled when it is
   * written out.
   *
   * @param blobDigest the digest of the BLOB to download
   * @param blobSizeListener callback to receive the total size of the BLOb to pull
   * @param writtenByteCountListener listens on byte count written to an output stream during the
   *     pull
   * @return a {@link Blob}
   */
  public Blob pullBlob(
      DescriptorDigest blobDigest,
      Consumer<long> blobSizeListener,
      Consumer<long> writtenByteCountListener) {
    return Blobs.from(
        outputStream => {
          try {
            callRegistryEndpoint(
                new BlobPuller(
                    registryEndpointRequestProperties,
                    blobDigest,
                    outputStream,
                    blobSizeListener,
                    writtenByteCountListener));

          } catch (RegistryException ex) {
            throw new IOException("", ex);
          }
        });
  }

  /**
   * Pushes the BLOB. If the {@code sourceRepository} is provided then the remote registry may skip
   * if the BLOB already exists on the registry.
   *
   * @param blobDigest the digest of the BLOB, used for existence-check
   * @param blob the BLOB to push
   * @param sourceRepository if pushing to the same registry then the source image, or {@code null}
   *     otherwise; used to optimize the BLOB push
   * @param writtenByteCountListener listens on byte count written to the registry during the push
   * @return {@code true} if the BLOB already exists on the registry and pushing was skipped; false
   *     if the BLOB was pushed
   * @throws IOException if communicating with the endpoint fails
   * @throws RegistryException if communicating with the endpoint fails
   */
  public bool pushBlob(
      DescriptorDigest blobDigest,
      Blob blob,
      string sourceRepository,
      Consumer<long> writtenByteCountListener)
      {
    BlobPusher blobPusher =
        new BlobPusher(registryEndpointRequestProperties, blobDigest, blob, sourceRepository);

    using (TimerEventDispatcher timerEventDispatcher =
        new TimerEventDispatcher(eventHandlers, "pushBlob")) {
      using (TimerEventDispatcher timerEventDispatcher2 =
          timerEventDispatcher.subTimer("pushBlob POST " + blobDigest)) {

        // POST /v2/<name>/blobs/uploads/ OR
        // POST /v2/<name>/blobs/uploads/?mount={blob.digest}&from={sourceRepository}
        Uri patchLocation = callRegistryEndpoint(blobPusher.initializer());
        if (patchLocation == null) {
          // The BLOB exists already.
          return true;
        }

        timerEventDispatcher2.lap("pushBlob PATCH " + blobDigest);

        // PATCH <Location> with BLOB
        Uri putLocation =
            callRegistryEndpoint(blobPusher.writer(patchLocation, writtenByteCountListener));
        Preconditions.checkNotNull(putLocation);

        timerEventDispatcher2.lap("pushBlob PUT " + blobDigest);

        // PUT <Location>?digest={blob.digest}
        callRegistryEndpoint(blobPusher.committer(putLocation));

        return false;
      }
    }
  }

  /** @return the registry endpoint's API root, without the protocol */

  public string getApiRouteBase() {
    return registryEndpointRequestProperties.getServerUrl() + "/v2/";
  }

  public string getUserAgent() {
    return userAgent;
  }

  /**
   * Calls the registry endpoint.
   *
   * @param registryEndpointProvider the {@link RegistryEndpointProvider} to the endpoint
   * @throws IOException if communicating with the endpoint fails
   * @throws RegistryException if communicating with the endpoint fails
   */
  private T callRegistryEndpoint<T>(RegistryEndpointProvider<T> registryEndpointProvider) {
    return new RegistryEndpointCaller<T>(
            eventHandlers,
            userAgent,
            getApiRouteBase(),
            registryEndpointProvider,
            authorization,
            registryEndpointRequestProperties,
            allowInsecureRegistries)
        .call();
  }
}
}
