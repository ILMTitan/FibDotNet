using System;
using System.Net.Http;

namespace com.google.cloud.tools.jib.http
{
    public interface IConnection : IDisposable
    {
        HttpResponseMessage send(HttpRequestMessage request);
    }
}