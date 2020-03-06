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

using System;
using System.Threading.Tasks;
using Fib.Net.Core.Async;

namespace Fib.Net.Core.BuildSteps
{
    internal static class AsyncStep
    {
        internal static IAsyncStep<T> Of<T>(Func<Task<T>> p)
        {
            return new FunStep<T>(p);
        }

        private class FunStep<T> : IAsyncStep<T>
        {
            private readonly Task<T> future;

            public FunStep(Func<Task<T>> p)
            {
                future = p();
            }

            public Task<T> GetFuture()
            {
                return future;
            }
        }
    }
}