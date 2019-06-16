using System.IO;

namespace com.google.cloud.tools.jib.docker
{
    public interface IImageTarball
    {
        void writeTo(Stream @out);
    }
}