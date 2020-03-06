using System.IO;

namespace Fib.Net.Core.FileSystem
{
    public static class FileSystemExtensions
    {
        public static SystemPath ToPath(this FileSystemInfo fileInfo)
        {
            return new SystemPath(fileInfo);
        }
    }
}
