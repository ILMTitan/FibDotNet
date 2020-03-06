// Copyright 2017 Google LLC.
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
using System.Globalization;
using System.Runtime.Serialization;

namespace Fib.Net.Core.Api
{
    /** Thrown because registry authentication failed. */
    [Serializable]
    public class RegistryAuthenticationFailedException : RegistryException
    {
        private const string REASON = "Failed to authenticate with registry {0}/{1} because: {2}";
        private readonly string registry;
        private readonly string imageName;

        public RegistryAuthenticationFailedException(string registry, string imageName, Exception cause)
            : base(string.Format(CultureInfo.CurrentCulture, REASON, registry, imageName, cause?.Message), cause)
        {
            this.registry = registry;
            this.imageName = imageName;
        }

        public RegistryAuthenticationFailedException(string registry, string imageName, string reason)
            : base(string.Format(CultureInfo.CurrentCulture, REASON, registry, imageName, reason))
        {
            this.registry = registry;
            this.imageName = imageName;
        }

        protected RegistryAuthenticationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /** @return the server being authenticated */
        public string GetRegistry()
        {
            return registry;
        }

        /** @return the image being authenticated */
        public string GetImageName()
        {
            return imageName;
        }
    }
}
