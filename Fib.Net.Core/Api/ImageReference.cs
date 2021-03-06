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

using Fib.Net.Core.Registry;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Fib.Net.Core.Api
{
    /**
     * Represents an image reference.
     *
     * @see <a
     *     href="https://github.com/docker/distribution/blob/master/reference/reference.go">https://github.com/docker/distribution/blob/master/reference/reference.go</a>
     * @see <a
     *     href="https://docs.docker.com/engine/reference/commandline/tag/#extended-description">https://docs.docker.com/engine/reference/commandline/tag/#extended-description</a>
     */
    public sealed class ImageReference : IImageReference
    {
        private const string DOCKER_HUB_REGISTRY = "registry-1.docker.io";
        private const string DEFAULT_TAG = "latest";
        private const string LIBRARY_REPOSITORY_PREFIX = "library/";

        /**
         * Matches all sequences of alphanumeric characters possibly separated by any number of dashes in
         * the middle.
         */
        private const string REGISTRY_COMPONENT_REGEX =
            "(?:[a-zA-Z\\d]|(?:[a-zA-Z\\d][a-zA-Z\\d-]*[a-zA-Z\\d]))";

        /**
         * Matches sequences of {@code REGISTRY_COMPONENT_REGEX} separated by a dot, with an optional
         * {@code :port} at the end.
         */
        private static readonly string REGISTRY_REGEX =
            $"{REGISTRY_COMPONENT_REGEX}(?:\\.{REGISTRY_COMPONENT_REGEX})*(?::\\d+)?";

        /**
         * Matches all sequences of alphanumeric characters separated by a separator.
         *
         * <p>A separator is either an underscore, a dot, two underscores, or any number of dashes.
         */
        private const string REPOSITORY_COMPONENT_REGEX = "[a-z\\d]+(?:(?:[_.]|__|-+)[a-z\\d]+)*";

        /** Matches all repetitions of {@code REPOSITORY_COMPONENT_REGEX} separated by a backslash. */
        private static readonly string REPOSITORY_REGEX =
            $"(?:{REPOSITORY_COMPONENT_REGEX}/)*{REPOSITORY_COMPONENT_REGEX}";

        /** Matches a tag of max length 128. */
        private const string TAG_REGEX = "[\\w][\\w.-]{0,127}";

        /**
         * Matches a full image reference, which is the registry, repository, and tag/digest separated by
         * backslashes. The repository is required, but the registry and tag/digest are optional.
         */
        private static readonly string REFERENCE_REGEX =
            $"^(?:({REGISTRY_REGEX})/)?({REPOSITORY_REGEX})(?:(?::({TAG_REGEX}))|(?:@({DescriptorDigest.DigestRegex})))?$";

        private static readonly Regex REFERENCE_PATTERN = new Regex(REFERENCE_REGEX);

        /**
         * Parses a string {@code reference} into an {@link ImageReference}.
         *
         * <p>Image references should generally be in the form: {@code <registry>/<repository>:<tag>} For
         * example, an image reference could be {@code gcr.io/distroless/java:debug}.
         *
         * <p>See <a
         * href="https://docs.docker.com/engine/reference/commandline/tag/#extended-description">https://docs.docker.com/engine/reference/commandline/tag/#extended-description</a>
         * for a description of valid image reference format. Note, however, that the image reference is
         * referred confusingly as {@code tag} on that page.
         *
         * @param reference the string to parse
         * @return an {@link ImageReference} parsed from the string
         * @throws InvalidImageReferenceException if {@code reference} is formatted incorrectly
         */
        public static ImageReference Parse(string reference)
        {
            Match matcher = REFERENCE_PATTERN.Match(reference);

            if (!matcher.Success || matcher.Groups.Count < 4)
            {
                throw new InvalidImageReferenceException(reference);
            }

            string registry = matcher.Groups[1].Value;
            string repository = matcher.Groups[2].Value;
            string tag = matcher.Groups[3].Value;
            string digest = matcher.Groups[4].Value;

            // If no registry was matched, use Docker Hub by default.
            if (string.IsNullOrEmpty(registry))
            {
                registry = DOCKER_HUB_REGISTRY;
            }

            if (string.IsNullOrEmpty(repository))
            {
                throw new InvalidImageReferenceException(reference);
            }
            /*
             * If a registry was matched but it does not contain any dots or colons, it should actually be
             * part of the repository unless it is "localhost".
             *
             * See https://github.com/docker/distribution/blob/245ca4659e09e9745f3cc1217bf56e946509220c/reference/normalize.go#L62
             */
            if (!registry.Contains(".") && !registry.Contains(":") && "localhost" != registry)
            {
                repository = registry + "/" + repository;
                registry = DOCKER_HUB_REGISTRY;
            }

            /*
             * For Docker Hub, if the repository is only one component, then it should be prefixed with
             * 'library/'.
             *
             * See https://docs.docker.com/engine/reference/commandline/pull/#pull-an-image-from-docker-hub
             */
            if (DOCKER_HUB_REGISTRY == registry && repository.IndexOf('/') < 0)
            {
                repository = LIBRARY_REPOSITORY_PREFIX + repository;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                if (!string.IsNullOrEmpty(digest))
                {
                    // Cannot have matched both tag and digest.
                    throw new InvalidImageReferenceException(reference);
                }
            }
            else if (!string.IsNullOrEmpty(digest))
            {
                tag = digest;
            }
            else
            {
                tag = DEFAULT_TAG;
            }

            return new ImageReference(registry, repository, tag);
        }

        /**
         * Constructs an {@link ImageReference} from the image reference components, consisting of an
         * optional registry, a repository, and an optional tag.
         *
         * @param registry the image registry, or {@code null} to use the default registry (Docker Hub)
         * @param repository the image repository
         * @param tag the image tag, or {@code null} to use the default tag ({@code latest})
         * @return an {@link ImageReference} built from the given registry, repository, and tag
         */
        public static ImageReference Of(
            string registry, string repository, string tag)
        {
            if (!string.IsNullOrEmpty(registry) && !IsValidRegistry(registry))
            {
                throw new ArgumentException($"'{registry}' is not a valid registry", nameof(registry));
            }
            if (!IsValidRepository(repository))
            {
                throw new ArgumentException($"'{repository}' is not a valid repository", nameof(repository));
            }
            if (!string.IsNullOrEmpty(tag) && !IsValidTag(tag))
            {
                throw new ArgumentException($"'{tag}' is not a valid tag", nameof(tag));
            }

            if (string.IsNullOrEmpty(registry))
            {
                registry = DOCKER_HUB_REGISTRY;
            }
            if (string.IsNullOrEmpty(tag))
            {
                tag = DEFAULT_TAG;
            }
            return new ImageReference(registry, repository, tag);
        }

        /**
         * Constructs an {@link ImageReference} with an empty registry and tag component, and repository
         * set to "scratch".
         *
         * @return an {@link ImageReference} with an empty registry and tag component, and repository set
         *     to "scratch"
         */
        public static ImageReference Scratch()
        {
            return new ImageReference("", "scratch", "");
        }

        /**
         * Returns {@code true} if {@code registry} is a valid registry string. For example, a valid
         * registry could be {@code gcr.io} or {@code localhost:5000}.
         *
         * @param registry the registry to check
         * @return {@code true} if is a valid registry; {@code false} otherwise
         */
        public static bool IsValidRegistry(string registry)
        {
            var match = Regex.Match(registry, REFERENCE_REGEX);
            return match.Success && match.Value == registry;
        }

        /**
         * Returns {@code true} if {@code repository} is a valid repository string. For example, a valid
         * repository could be {@code distroless} or {@code my/container-image/repository}.
         *
         * @param repository the repository to check
         * @return {@code true} if is a valid repository; {@code false} otherwise
         */
        public static bool IsValidRepository(string repository)
        {
            var match = Regex.Match(repository, REPOSITORY_REGEX);
            return match.Success && match.Value == repository;
        }

        /**
         * Returns {@code true} if {@code tag} is a valid tag string. For example, a valid tag could be
         * {@code v120.5-release}.
         *
         * @param tag the tag to check
         * @return {@code true} if is a valid tag; {@code false} otherwise
         */
        public static bool IsValidTag(string tag)
        {
            return IsValidTagName(tag) || DescriptorDigest.IsValidDigest(tag);
        }

        private static bool IsValidTagName(string tag)
        {
            var match = Regex.Match(tag, TAG_REGEX);
            return match.Success && match.Value == tag;
        }

        /**
         * Returns {@code true} if {@code tag} is the default tag ((@code latest} or empty); {@code false}
         * if not.
         *
         * @param tag the tag to check
         * @return {@code true} if {@code tag} is the default tag ((@code latest} or empty); {@code false}
         *     if not
         */
        public static bool IsDefaultTag(string tag)
        {
            return string.IsNullOrEmpty(tag) || DEFAULT_TAG == tag;
        }

        private readonly string registry;
        private readonly string repository;
        private readonly string tag;

        /** Construct with {@link #parse}. */
        private ImageReference(string registry, string repository, string tag)
        {
            this.registry = RegistryAliasGroup.GetHost(registry);
            this.repository = repository;
            this.tag = tag;
        }

        /**
         * Gets the registry portion of the {@link ImageReference}.
         *
         * @return the registry host
         */
        public string GetRegistry()
        {
            return registry;
        }

        /**
         * Gets the repository portion of the {@link ImageReference}.
         *
         * @return the repository
         */
        public string GetRepository()
        {
            return repository;
        }

        /**
         * Gets the tag portion of the {@link ImageReference}.
         *
         * @return the tag
         */
        public string GetTag()
        {
            return tag;
        }

        /**
         * Returns {@code true} if the {@link ImageReference} uses the default tag ((@code latest} or
         * empty); {@code false} if not
         *
         * @return {@code true} if uses the default tag; {@code false} if not
         */
        public bool UsesDefaultTag()
        {
            return IsDefaultTag(tag);
        }

        /**
         * Returns {@code true} if the {@link ImageReference} uses a SHA-256 digest as its tag; {@code
         * false} if not.
         *
         * @return {@code true} if tag is a SHA-256 digest; {@code false} if not
         */
        public bool IsTagDigest()
        {
            return DescriptorDigest.IsValidDigest(tag);
        }

        /**
         * Returns {@code true} if the {@link ImageReference} is a scratch image; {@code false} if not.
         *
         * @return {@code true} if the {@link ImageReference} is a scratch image; {@code false} if not
         */
        public bool IsScratch()
        {
            return string.IsNullOrEmpty(registry) && "scratch" == repository && string.IsNullOrEmpty(tag);
        }

        /**
         * Gets an {@link ImageReference} with the same registry and repository, but a different tag.
         *
         * @param newTag the new tag
         * @return an {@link ImageReference} with the same registry/repository and the new tag
         */
        public ImageReference WithTag(string newTag)
        {
            return Of(registry, repository, newTag);
        }

        /**
         * Stringifies the {@link ImageReference}. When the tag is a digest, it is prepended with the at
         * {@code @} symbol instead of a colon {@code :}.
         *
         * @return the image reference in Docker-readable format (inverse of {@link #parse})
         */

        public override string ToString()
        {
            StringBuilder referenceString = new StringBuilder();

            if (DOCKER_HUB_REGISTRY != registry)
            {
                // Use registry and repository if not Docker Hub.
                referenceString.Append(registry).Append('/').Append(repository);
            }
            else if (repository.StartsWith(LIBRARY_REPOSITORY_PREFIX, StringComparison.Ordinal))
            {
                // If Docker Hub and repository has 'library/' prefix, remove the 'library/' prefix.
                string repositorySubstring = repository.Substring(LIBRARY_REPOSITORY_PREFIX.Length);
                referenceString.Append(repositorySubstring);
            }
            else
            {
                // Use just repository if Docker Hub.
                referenceString.Append(repository);
            }

            // Use tag if not the default tag.
            if (DEFAULT_TAG != tag)
            {
                // Append with "@tag" instead of ":tag" if tag is a digest
                referenceString.Append(IsTagDigest() ? '@' : ':').Append(tag);
            }

            return referenceString.ToString();
        }

        /**
         * Stringifies the {@link ImageReference}, without hiding the tag.
         *
         * @return the image reference in Docker-readable format, without hiding the tag
         */
        public string ToStringWithTag()
        {
            return ToString() + (UsesDefaultTag() ? ":" + DEFAULT_TAG : "");
        }
    }
}
