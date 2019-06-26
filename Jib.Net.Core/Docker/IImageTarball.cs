using System.IO;
using System.Threading.Tasks;

namespace com.google.cloud.tools.jib.docker
{
    public interface IImageTarball
    {
        Task WriteToAsync(Stream stream);
    }
}