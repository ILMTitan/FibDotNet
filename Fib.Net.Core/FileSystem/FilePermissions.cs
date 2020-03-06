// Copyright 2018 Google LLC.
// Copyright 2020 James Przybylinski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// NOTICE: This file was modified by James Przybylinski to be C#.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Fib.Net.Core.FileSystem
{
    /** Represents read/write/execute file permissions for owner, group, and others. */
    public class FilePermissions
    {
        /** Default permissions for files added to the container. */
        public static readonly FilePermissions DefaultFilePermissions = FromOctalString("644");

        /** Default permissions for folders added to the container. */
        public static readonly FilePermissions DefaultFolderPermissions = FromOctalString("755");

        /**
         * Matches an octal string representation of file permissions. From left to right, each digit
         * represents permissions for owner, group, and other.
         */
        private const string OCTAL_PATTERN = "[0-7][0-7][0-7]";

        /**
         * Creates a new {@link FilePermissions} from an octal string representation (e.g. "123", "644",
         * "755", etc).
         *
         * @param octalPermissions the octal string representation of the permissions
         * @return a new {@link FilePermissions} with the given permissions
         */
        public static FilePermissions FromOctalString(string octalPermissions)
        {
            Preconditions.CheckArgument(
                IsOctalString(octalPermissions),
                "octalPermissions must be a 3-digit octal number (000-777)");

            return new FilePermissions((PosixFilePermissions)Convert.ToInt32(octalPermissions, 8));
        }

        private static bool IsOctalString(string octalString)
        {
            var match = Regex.Match(octalString, OCTAL_PATTERN);
            return match.Success && match.Value == octalString;
        }

        /**
         * Creates a new {@link FilePermissions} from a set of {@link PosixFilePermission}.
         *
         * @param posixFilePermissions the set of {@link PosixFilePermission}
         * @return a new {@link FilePermissions} with the given permissions
         */
        public static FilePermissions FromPosixFilePermissions(
            ISet<PosixFilePermissions> posixFilePermissions)
        {
            posixFilePermissions = posixFilePermissions ?? throw new ArgumentNullException(nameof(posixFilePermissions));
            PosixFilePermissions permissionBits = 0;
            foreach (PosixFilePermissions permission in posixFilePermissions)
            {
                permissionBits |= permission;
            }
            return new FilePermissions(permissionBits);
        }

        private readonly PosixFilePermissions permissionBits;

        public FilePermissions(PosixFilePermissions permissionBits)
        {
            this.permissionBits = permissionBits;
        }

        /**
         * Gets the corresponding permissions bits specified by the {@link FilePermissions}.
         *
         * @return the permission bits
         */
        public PosixFilePermissions GetPermissionBits()
        {
            return permissionBits;
        }

        /**
         * Gets the octal string representation of the permissions.
         *
         * @return the octal string representation of the permissions
         */
        public string ToOctalString()
        {
            return permissionBits.ToOctalString();
        }

        public override string ToString()
        {
            return permissionBits.ToString("G");
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }
            if (!(other is FilePermissions otherFilePermissions))
            {
                return false;
            }
            return permissionBits == otherFilePermissions.permissionBits;
        }

        public override int GetHashCode()
        {
            return permissionBits.GetHashCode();
        }
    }
}
