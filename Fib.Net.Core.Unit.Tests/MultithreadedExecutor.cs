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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fib.Net.Core.Unit.Tests
{
    /** Testing infrastructure for running code across multiple threads. */
    public static class MultithreadedExecutor
    {
        public static async Task<T> InvokeAsync<T>(Func<T> callable)
        {
            return await Task.Run(callable).ConfigureAwait(false);
        }

        public static async Task InvokeAsync(Action a)
        {
            await Task.Run(a).ConfigureAwait(false);
        }

        public static async Task InvokeAllAsync(IEnumerable<Action> callables)
        {
            IEnumerable<Task> futures = callables.Select(Task.Run);
            await Task.WhenAll(futures).ConfigureAwait(false);
        }

        public static async Task<IList<T>> InvokeAllAsync<T>(IEnumerable<Func<T>> callables)
        {
            IEnumerable<Task<T>> futures = callables.Select(Task.Run);
            return await Task.WhenAll(futures).ConfigureAwait(false);
        }
    }
}
