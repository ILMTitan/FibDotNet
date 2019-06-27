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
using com.google.cloud.tools.jib.registry.credentials;
using Jib.Net.Core.Api;
using Jib.Net.Core.Events;
using Jib.Net.Core.FileSystem;
using Jib.Net.Core.Global;
using Jib.Net.Core.Registry.Credentials;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Jib.Net.Core.Frontend
{
    /** Used for passing in mock {@link DockerCredentialHelper}s for testing. */
    public delegate IDockerCredentialHelper DockerCredentialHelperFactory(string registry, SystemPath credentialHelper);

    /** Static factories for various {@link CredentialRetriever}s. */
    public class CredentialRetrieverFactory
    {
        /**
         * Defines common credential helpers to use as defaults. Maps from registry suffix to credential
         * helper suffix.
         */
        private static readonly ImmutableDictionary<string, string> COMMON_CREDENTIAL_HELPERS =
            ImmutableDictionary.CreateRange(new Dictionary<string, string> { ["gcr.io"] = "gcr", ["amazonaws.com"] = "ecr-login" });

        /**
         * Creates a new {@link CredentialRetrieverFactory} for an image.
         *
         * @param imageReference the image the credential are for
         * @param logger a consumer for handling log events
         * @return a new {@link CredentialRetrieverFactory}
         */
        public static CredentialRetrieverFactory ForImage(
            ImageReference imageReference, Action<LogEvent> logger)
        {
            return new CredentialRetrieverFactory(imageReference, logger, Registry.Credentials.DockerCredentialHelper.Create);
        }

        /**
         * Creates a new {@link CredentialRetrieverFactory} for an image.
         *
         * @param imageReference the image the credential are for
         * @return a new {@link CredentialRetrieverFactory}
         */
        public static CredentialRetrieverFactory ForImage(ImageReference imageReference)
        {
            return new CredentialRetrieverFactory(
                imageReference, _ => { }, Registry.Credentials.DockerCredentialHelper.Create);
        }

        private readonly ImageReference imageReference;
        private readonly Action<LogEvent> logger;
        private readonly DockerCredentialHelperFactory dockerCredentialHelperFactory;

        public CredentialRetrieverFactory(
            ImageReference imageReference,
            Action<LogEvent> logger,
            DockerCredentialHelperFactory dockerCredentialHelperFactory)
        {
            this.imageReference = imageReference;
            this.logger = logger;
            this.dockerCredentialHelperFactory = dockerCredentialHelperFactory;
        }

        /**
         * Creates a new {@link CredentialRetriever} that returns a known {@link Credential}.
         *
         * @param credential the known credential
         * @param credentialSource the source of the credentials (for logging)
         * @return a new {@link CredentialRetriever}
         */
        public CredentialRetriever Known(Credential credential, string credentialSource)
        {
            return () =>
            {
                LogGotCredentialsFrom(credentialSource);
                return Option.Of(credential);
            };
        }

        /**
         * Creates a new {@link CredentialRetriever} for retrieving credentials via a Docker credential
         * helper, such as {@code docker-credential-gcr}.
         *
         * @param credentialHelper the credential helper executable
         * @return a new {@link CredentialRetriever}
         */
        public CredentialRetriever DockerCredentialHelper(string credentialHelper)
        {
            return DockerCredentialHelper(Paths.Get(credentialHelper));
        }

        /**
         * Creates a new {@link CredentialRetriever} for retrieving credentials via a Docker credential
         * helper, such as {@code docker-credential-gcr}.
         *
         * @param credentialHelper the credential helper executable
         * @return a new {@link CredentialRetriever}
         * @see <a
         *     href="https://github.com/docker/docker-credential-helpers#development">https://github.com/docker/docker-credential-helpers#development</a>
         */
        public CredentialRetriever DockerCredentialHelper(SystemPath credentialHelper)
        {
            return () =>
            {
                logger(LogEvent.Info("Checking credentials from " + credentialHelper));

                try
                {
                    return Option.Of(RetrieveFromDockerCredentialHelper(credentialHelper));
                }
                catch (CredentialHelperUnhandledServerUrlException)
                {
                    logger(
                        LogEvent.Info(
                            "No credentials for " + imageReference.GetRegistry() + " in " + credentialHelper));
                    return Option.Empty<Credential>();
                }
                catch (IOException ex)
                {
                    throw new CredentialRetrievalException(ex);
                }
            };
        }

        /**
         * Creates a new {@link CredentialRetriever} that tries common Docker credential helpers to
         * retrieve credentials based on the registry of the image, such as {@code docker-credential-gcr}
         * for images with the registry as {@code gcr.io}.
         *
         * @return a new {@link CredentialRetriever}
         */
        public CredentialRetriever InferCredentialHelper()
        {
            IList<string> inferredCredentialHelperSuffixes = new List<string>();
            foreach (string registrySuffix in COMMON_CREDENTIAL_HELPERS.Keys)
            {
                if (!JavaExtensions.EndsWith(imageReference.GetRegistry(), registrySuffix))
                {
                    continue;
                }
                string inferredCredentialHelperSuffix = COMMON_CREDENTIAL_HELPERS[registrySuffix];
                if (inferredCredentialHelperSuffix == null)
                {
                    throw new InvalidOperationException(
                        Resources.CredentialRetrieverFactoryCommonCredentialNullExceptionMessage);
                }
                JavaExtensions.Add(inferredCredentialHelperSuffixes, inferredCredentialHelperSuffix);
            }

            return () =>
            {
                foreach (string inferredCredentialHelperSuffix in inferredCredentialHelperSuffixes)
                {
                    try
                    {
                        return Option.Of(
                            RetrieveFromDockerCredentialHelper(
                                Paths.Get(
                                    Registry.Credentials.DockerCredentialHelper.CredentialHelperPrefix
                                        + inferredCredentialHelperSuffix)));
                    }
                    catch (Exception ex) when (ex is CredentialHelperNotFoundException || ex is CredentialHelperUnhandledServerUrlException)
                    {
                        if (ex.GetMessage() != null)
                        {
                            // Warns the user that the specified (or inferred) credential helper cannot be used.
                            logger(LogEvent.Info(ex.GetMessage()));
                            if (ex.InnerException?.GetMessage() != null)
                            {
                                logger(LogEvent.Info("  Caused by: " + ex.InnerException.GetMessage()));
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        throw new CredentialRetrievalException(ex);
                    }
                }
                return Option.Empty<Credential>();
            };
        }

        /**
         * Creates a new {@link CredentialRetriever} that tries to retrieve credentials from Docker config
         * (located at {@code $USER_HOME/.docker/config.json}).
         *
         * @return a new {@link CredentialRetriever}
         * @see DockerConfigCredentialRetriever
         */
        public CredentialRetriever DockerConfig()
        {
            return DockerConfig(new DockerConfigCredentialRetriever(imageReference.GetRegistry()));
        }

        /**
         * Creates a new {@link CredentialRetriever} that tries to retrieve credentials from a custom path
         * to a Docker config.
         *
         * @param dockerConfigFile the path to the Docker config file
         * @return a new {@link CredentialRetriever}
         * @see DockerConfigCredentialRetriever
         */
        public CredentialRetriever DockerConfig(SystemPath dockerConfigFile)
        {
            return DockerConfig(
                new DockerConfigCredentialRetriever(imageReference.GetRegistry(), dockerConfigFile));
        }

        public CredentialRetriever DockerConfig(
            IDockerConfigCredentialRetriever dockerConfigCredentialRetriever)
        {
            return () =>
            {
                try
                {
                    Option<Credential> dockerConfigCredentials =
                        dockerConfigCredentialRetriever.Retrieve(logger);
                    if (dockerConfigCredentials.IsPresent())
                    {
                        logger(
                            LogEvent.Info(
                                "Using credentials from Docker config for " + imageReference.GetRegistry()));
                        return dockerConfigCredentials;
                    }
                }
                catch (IOException)
                {
                    logger(LogEvent.Info("Unable to parse Docker config"));
                }
                return Option.Empty<Credential>();
            };
        }

        private Credential RetrieveFromDockerCredentialHelper(SystemPath credentialHelper)
        {
            Credential credentials =
                dockerCredentialHelperFactory(imageReference.GetRegistry(), credentialHelper)
                    .Retrieve();
            LogGotCredentialsFrom(JavaExtensions.ToString(credentialHelper.GetFileName()));
            return credentials;
        }

        private void LogGotCredentialsFrom(string credentialSource)
        {
            logger(
                LogEvent.Info("Using " + credentialSource + " for " + imageReference.GetRegistry()));
        }
    }
}
