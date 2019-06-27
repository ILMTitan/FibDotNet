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
using Jib.Net.Core.Events.Progress;
using Jib.Net.Core.Events.Time;
using System.Globalization;
using System.Threading.Tasks;

namespace Jib.Net.Core.BuildSteps
{
    /**
     * Authenticates push to a target registry using Docker Token Authentication.
     *
     * @see <a
     *     href="https://docs.docker.com/registry/spec/auth/token/">https://docs.docker.com/registry/spec/auth/token/</a>
     */
    internal class AuthenticatePushStep : IAsyncStep<Authorization>
    {
        private const string DESCRIPTION = "Authenticating with push to {0}";

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

            listenableFuture = CallAsync();
        }

        public Task<Authorization> GetFuture()
        {
            return listenableFuture;
        }

        public async Task<Authorization> CallAsync()
        {
            Credential registryCredential = await retrieveTargetRegistryCredentialsStep.GetFuture().ConfigureAwait(false);

            string registry = buildConfiguration.GetTargetImageConfiguration().GetImageRegistry();
            try
            {
                using (progressEventDispatcherFactory.Create("authenticating push to " + registry, 1))
                using (new TimerEventDispatcher(
                            buildConfiguration.GetEventHandlers(), string.Format(CultureInfo.CurrentCulture, DESCRIPTION, registry)))
                {
                    RegistryAuthenticator registryAuthenticator =
                        await buildConfiguration
                            .NewTargetImageRegistryClientFactory()
                            .NewRegistryClient()
                            .GetRegistryAuthenticatorAsync().ConfigureAwait(false);
                    if (registryAuthenticator != null)
                    {
                        return await registryAuthenticator.AuthenticatePushAsync(registryCredential).ConfigureAwait(false);
                    }
                }
            }
            catch (InsecureRegistryException)
            {
                // Cannot skip certificate validation or use HTTP; fall through.
            }

            return registryCredential?.IsOAuth2RefreshToken() != false
                ? null
                : Authorization.FromBasicCredentials(
                    registryCredential.GetUsername(), registryCredential.GetPassword());
        }
    }
}
