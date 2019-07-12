using Jib.Net.Core.Api;

namespace com.google.cloud.tools.jib.configuration
{
    public interface IEventHandlers
    {
        void Dispatch(IJibEvent jibEvent);
    }
}