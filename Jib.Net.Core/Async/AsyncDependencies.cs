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

using com.google.cloud.tools.jib.builder.steps;
using Jib.Net.Core;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.async
{
    /**
     * Builds a list of dependency {@link ListenableFuture}s to wait on before calling a {@link
     * Callable}.
     */
    public sealed class AsyncDependencies
    {
        /**
         * Initialize with a {@link ListeningExecutorService}.
         *
         * @param listeningExecutorService the {@link ListeningExecutorService}
         * @return a new {@link AsyncDependencies}
         */
        public static AsyncDependencies @using()
        {
            return new AsyncDependencies();
        }

        /** Stores the list of {@link ListenableFuture}s to wait on. */
        private readonly IList<Task> futures = new List<Task>();

        private AsyncDependencies()
        {
        }

        /**
         * Adds the future of an {@link AsyncStep}.
         *
         * @param asyncStep the {@link AsyncStep}
         * @return this
         */
        public AsyncDependencies addStep<T>(AsyncStep<T> asyncStep)
        {
            futures.add(asyncStep.getFuture());
            return this;
        }

        /**
         * Adds the futures of a list of {@link AsyncStep}s.
         *
         * @param asyncSteps the {@link AsyncStep}s
         * @return this
         */
        public AsyncDependencies addSteps<T>(IEnumerable<AsyncStep<T>> asyncSteps)
        {
            asyncSteps.forEach(this.addStep);
            return this;
        }

        internal async Task<TResult> whenAllSucceed<TResult>(Func<TResult> f)
        {
            await Task.WhenAll(futures).ConfigureAwait(false);
            return await Task.Run(f).ConfigureAwait(false);
        }

        internal Task<T> call<T>(Func<T> p)
        {
            throw new NotImplementedException();
        }
    }
}

namespace Jib.Net.Core
{
    public delegate C Callable<C>();
}