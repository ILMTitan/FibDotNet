using com.google.cloud.tools.jib.api;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jib.Net.Test.Common
{
    public static class TaskOptionalExtensions
    {
        public static async Task<T> orElseThrowAsync<T>(this Task<Option<T>> optionalTask, Func<Exception> execptionFunc)
        {
            optionalTask = optionalTask ?? throw new ArgumentNullException(nameof(optionalTask));
            var optional = await optionalTask.ConfigureAwait(false);
            return optional.orElseThrow(execptionFunc);
        }
    }
}
