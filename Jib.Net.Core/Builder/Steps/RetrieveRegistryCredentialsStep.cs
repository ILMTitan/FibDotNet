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
using Jib.Net.Core;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.builder.steps
{
    /** Attempts to retrieve registry credentials. */
    public sealed class RetrieveRegistryCredentialsStep : IAsyncStep<Credential>
    {
        private static string MakeDescription(string registry)
        {
            return "Retrieving registry credentials for " + registry;
        }

        /** Retrieves credentials for the base image. */
        public static RetrieveRegistryCredentialsStep ForBaseImage(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory)
        {
            buildConfiguration = buildConfiguration ?? throw new ArgumentNullException(nameof(buildConfiguration));
            return new RetrieveRegistryCredentialsStep(
                buildConfiguration,
                progressEventDispatcherFactory,
                buildConfiguration.GetBaseImageConfiguration().GetImageRegistry(),
                buildConfiguration.GetBaseImageConfiguration().GetCredentialRetrievers());
        }

        /** Retrieves credentials for the target image. */
        public static RetrieveRegistryCredentialsStep ForTargetImage(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory)
        {
            buildConfiguration = buildConfiguration ?? throw new ArgumentNullException(nameof(buildConfiguration));
            return new RetrieveRegistryCredentialsStep(
                buildConfiguration,
                progressEventDispatcherFactory,
                buildConfiguration.GetTargetImageConfiguration().GetImageRegistry(),
                buildConfiguration.GetTargetImageConfiguration().GetCredentialRetrievers());
        }

        private readonly BuildConfiguration buildConfiguration;
        private readonly ProgressEventDispatcher.Factory progressEventDispatcherFactory;

        private readonly string registry;
        private readonly ImmutableArray<CredentialRetriever> credentialRetrievers;

        private readonly Task<Credential> listenableFuture;

        private RetrieveRegistryCredentialsStep(
            BuildConfiguration buildConfiguration,
            ProgressEventDispatcher.Factory progressEventDispatcherFactory,
            string registry,
            ImmutableArray<CredentialRetriever> credentialRetrievers)
        {
            this.buildConfiguration = buildConfiguration;
            this.progressEventDispatcherFactory = progressEventDispatcherFactory;
            this.registry = registry;
            this.credentialRetrievers = credentialRetrievers;
            listenableFuture = Task.Run(Call);
        }

        public Task<Credential> GetFuture()
        {
            return listenableFuture;
        }

        public Credential Call()
        {
            string description = MakeDescription(registry);

            buildConfiguration.GetEventHandlers().Dispatch(LogEvent.Progress(description + "..."));

            using (progressEventDispatcherFactory.Create("retrieving credentials for " + registry, 1))
            using (new TimerEventDispatcher(buildConfiguration.GetEventHandlers(), description))
            {
                foreach (CredentialRetriever credentialRetriever in credentialRetrievers)
                {
                    Option<Credential> optionalCredential = credentialRetriever.Retrieve();
                    if (optionalCredential.IsPresent())
                    {
                        return optionalCredential.Get();
                    }
                }

                // If no credentials found, give an info (not warning because in most cases, the base image is
                // public and does not need extra credentials) and return null.
                buildConfiguration
                    .GetEventHandlers()
                    .Dispatch(LogEvent.Info("No credentials could be retrieved for registry " + registry));
                return null;
            }
        }
    }
}
