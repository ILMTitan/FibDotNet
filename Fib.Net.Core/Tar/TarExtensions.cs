using ICSharpCode.SharpZipLib.Tar;
using System;

namespace Fib.Net.Core.Tar
{
    public static class TarExtensions
    {
        public static bool IsFile(this TarEntry entry)
        {
            entry = entry ?? throw new ArgumentNullException(nameof(entry));
            return entry.TarHeader.TypeFlag != TarHeader.LF_DIR;
        }

        public static void SetMode(this TarEntry entry, PosixFilePermissions mode)
        {
            entry = entry ?? throw new ArgumentNullException(nameof(entry));
            entry.TarHeader.Mode = (int)mode;
        }

        public static PosixFilePermissions GetMode(this TarEntry entry)
        {
            entry = entry ?? throw new ArgumentNullException(nameof(entry));
            return (PosixFilePermissions)entry.TarHeader.Mode;
        }
    }
}
