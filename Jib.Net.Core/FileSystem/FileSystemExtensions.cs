using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jib.Net.Core.FileSystem
{
    public static class FileSystemExtensions
    {
        public static SystemPath ToPath(this FileSystemInfo fileInfo)
        {
            return new SystemPath(fileInfo);
        }
    }
}
