// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Fib.Net.Core.Api;
using System;
using System.Threading.Tasks;

namespace Fib.Net.Test.Common
{
    public static class TaskOptionalExtensions
    {
        public static async Task<T> OrElseThrowAsync<T>(this Task<Maybe<T>> optionalTask, Func<Exception> execptionFunc)
        {
            optionalTask = optionalTask ?? throw new ArgumentNullException(nameof(optionalTask));
            var optional = await optionalTask.ConfigureAwait(false);
            return optional.OrElseThrow(execptionFunc);
        }
    }
}
