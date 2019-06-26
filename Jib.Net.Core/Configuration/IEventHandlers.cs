using com.google.cloud.tools.jib.api;

namespace com.google.cloud.tools.jib.configuration
{
    public interface IEventHandlers
    {
        void Dispatch(IJibEvent jibEvent);
    }
}