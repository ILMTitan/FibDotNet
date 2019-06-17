using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.http
{
    public interface IConnection : IDisposable
    {
        Task<HttpResponseMessage> sendAsync(HttpRequestMessage request);
    }
}