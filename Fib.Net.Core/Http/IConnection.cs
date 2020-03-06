using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fib.Net.Core.Http
{
    public interface IConnection : IDisposable
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
    }
}