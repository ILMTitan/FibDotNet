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
using com.google.cloud.tools.jib.async;
using com.google.cloud.tools.jib.configuration;
using com.google.cloud.tools.jib.http;
using com.google.cloud.tools.jib.registry;
using Jib.Net.Core;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{








    /**
     * Authenticates push to a target registry using Docker Token Authentication.
     *
     * @see <a
     *     href="https://docs.docker.com/registry/spec/auth/token/">https://docs.docker.com/registry/spec/auth/token/</a>
     */
    internal class AuthenticatePushStep : AsyncStep<Authorization>
    {
        private static readonly string DESCRIPTION = "Authenticating with push to {0}";

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly RetrieveRegistryCredentialsStep retrieveTargetRegistryCredentialsStep;

        private readonly Task<Authorization> listenableFuture;

        public AuthenticatePushStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            RetrieveRegistryCredentialsStep retrieveTargetRegistryCredentialsStep)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.retrieveTargetRegistryCredentialsStep = retrieveTargetRegistryCredentialsStep;

            listenableFuture =
                AsyncDependencies.@using()
                    .addStep(retrieveTargetRegistryCredentialsStep)
                    .whenAllSucceed(call);
        }

        public Task<Authorization> getFuture()
        {
            return listenableFuture;
        }

        public Authorization call()
        {
            Credential registryCredential = NonBlockingSteps.get(retrieveTargetRegistryCredentialsStep);

            string registry = buildConfiguration.getTargetImageConfiguration().getImageRegistry();
            try
            {
                using (ProgressEventDispatcher ignored =
                        progressEventDispatcherFactory.create("authenticating push to " + registry, 1))
                using (TimerEventDispatcher ignored2 =
                        new TimerEventDispatcher(
                            buildConfiguration.getEventHandlers(), string.Format(DESCRIPTION, registry)))
                {
                    RegistryAuthenticator registryAuthenticator =
                        buildConfiguration
                            .newTargetImageRegistryClientFactory()
                            .newRegistryClient()
                            .getRegistryAuthenticator();
                    if (registryAuthenticator != null)
                    {
                        return registryAuthenticator.authenticatePush(registryCredential);
                    }
                }
            }
            catch (InsecureRegistryException)
            {
                // Cannot skip certificate validation or use HTTP; fall through.
            }

            return (registryCredential == null || registryCredential.isOAuth2RefreshToken())
                ? null
                : Authorization.fromBasicCredentials(
                    registryCredential.getUsername(), registryCredential.getPassword());
        }
    }
}
