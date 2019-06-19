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
using System.Collections.Immutable;
using System.Threading.Tasks;
using com.google.cloud.tools.jib.async;
using com.google.cloud.tools.jib.cache;

namespace com.google.cloud.tools.jib.builder.steps
{
    internal static class AsyncSteps
    {
        internal static AsyncStep<T> immediate<T>(T value)
        {
            return AsyncStep.Of(() => Task.FromResult(value));
        }

        internal static AsyncStep<IReadOnlyList<T>> fromTasks<T>(IEnumerable<Task<T>> tasks) {
            async Task<IReadOnlyList<T>> f() => await Task.WhenAll(tasks);
            return AsyncStep.Of(f);
        }
    }
}