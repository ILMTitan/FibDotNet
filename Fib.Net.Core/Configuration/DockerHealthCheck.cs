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

using Fib.Net.Core.Api;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Fib.Net.Core.Configuration
{
    /** Configuration information for performing healthchecks on a Docker container. */
    public sealed class DockerHealthCheck
    {
        /** Builds the immutable {@link DockerHealthCheck}. */
        public class Builder
        {
            private readonly ImmutableArray<string> command;
            private Duration interval;
            private Duration timeout;
            private Duration startPeriod;
            private int retries;

            public Builder(ImmutableArray<string> command)
            {
                this.command = command;
            }

            /**
             * Sets the time between healthchecks.
             *
             * @param interval the duration to wait between healthchecks.
             * @return this
             */
            public Builder SetInterval(Duration interval)
            {
                this.interval = interval;
                return this;
            }

            /**
             * Sets the time until a healthcheck is considered hung.
             *
             * @param timeout the duration to wait until considering the healthcheck to be hung.
             * @return this
             */
            public Builder SetTimeout(Duration timeout)
            {
                this.timeout = timeout;
                return this;
            }

            /**
             * Sets the initialization time to wait before using healthchecks.
             *
             * @param startPeriod the duration to wait before using healthchecks
             * @return this
             */
            public Builder SetStartPeriod(Duration startPeriod)
            {
                this.startPeriod = startPeriod;
                return this;
            }

            /**
             * Sets the number of times to retry the healthcheck before the container is considered to be
             * unhealthy.
             *
             * @param retries the number of retries before the container is considered to be unhealthy
             * @return this
             */
            public Builder SetRetries(int retries)
            {
                this.retries = retries;
                return this;
            }

            public DockerHealthCheck Build()
            {
                return new DockerHealthCheck(command, interval, timeout, startPeriod, retries);
            }
        }

        /**
         * Creates a new {@link DockerHealthCheck.Builder} with the specified command.
         *
         * @param command the command
         * @return a new {@link DockerHealthCheck.Builder}
         */
        public static Builder FromCommand(IList<string> command)
        {
            command = command ?? throw new ArgumentNullException(nameof(command));
            Preconditions.CheckArgument(command.Count > 0, "command must not be empty");
            Preconditions.CheckArgument(!command.Contains(null), "command must not contain null elements");
            return new Builder(ImmutableArray.CreateRange(command));
        }

        private readonly ImmutableArray<string> command;
        private readonly Duration interval;
        private readonly Duration timeout;
        private readonly Duration startPeriod;
        private readonly int retries;

        private DockerHealthCheck(
            ImmutableArray<string> command,
            Duration interval,
            Duration timeout,
            Duration startPeriod,
            int retries)
        {
            this.command = command;
            this.interval = interval;
            this.timeout = timeout;
            this.startPeriod = startPeriod;
            this.retries = retries;
        }

        /**
         * Gets the optional healthcheck command. A missing command means that it will be inherited from
         * the base image.
         *
         * @return the healthcheck command
         */
        public IList<string> GetCommand()
        {
            return command;
        }

        /**
         * Gets the optional healthcheck interval. A missing command means that it will be inherited from
         * the base image.
         *
         * @return the healthcheck interval
         */
        public Maybe<Duration> GetInterval()
        {
            return Maybe.OfNullable(interval);
        }

        /**
         * Gets the optional healthcheck timeout. A missing command means that it will be inherited from
         * the base image.
         *
         * @return the healthcheck timeout
         */
        public Maybe<Duration> GetTimeout()
        {
            return Maybe.OfNullable(timeout);
        }

        /**
         * Gets the optional healthcheck start period. A missing command means that it will be inherited
         * from the base image.
         *
         * @return the healthcheck start period
         */
        public Maybe<Duration> GetStartPeriod()
        {
            return Maybe.OfNullable(startPeriod);
        }

        /**
         * Gets the optional healthcheck retry count. A missing command means that it will be inherited
         * from the base image.
         *
         * @return the healthcheck retry count
         */
        public Maybe<int> GetRetries()
        {
            return Maybe.OfNullable(retries);
        }
    }
}
