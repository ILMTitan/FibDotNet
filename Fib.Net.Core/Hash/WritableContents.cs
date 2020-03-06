// Copyright 2019 Google LLC.
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
using System.IO;
using System.Threading.Tasks;

namespace Fib.Net.Core.Hash
{
    /**
     * As a function, writes some contents to an output stream. As a class, represents contents that can
     * be written to an output stream. This may be "unrealized-before-write" contents; for example, a
     * file may be open and read for input contents only when this function is called to write to an
     * output stream.
     */
    public delegate void WritableContents(Stream outputStream);

    /**
     * As a function, writes some contents to an output stream. As a class, represents contents that can
     * be written to an output stream. This may be "unrealized-before-write" contents; for example, a
     * file may be open and read for input contents only when this function is called to write to an
     * output stream.
     */
    public delegate Task WritableContentsAsync(Stream outputStream);

    public static class WCExtensions
    {
        public static void WriteTo(this WritableContents wc, Stream outputStream)
        {
            wc = wc ?? throw new ArgumentNullException(nameof(wc));
            wc(outputStream);
        }
    }
}
