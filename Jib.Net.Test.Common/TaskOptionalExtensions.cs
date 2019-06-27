using com.google.cloud.tools.jib.api;
using System;
using System.Threading.Tasks;

namespace Jib.Net.Test.Common
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
