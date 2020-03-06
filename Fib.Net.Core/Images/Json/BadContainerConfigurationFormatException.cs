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
using System.Runtime.Serialization;

namespace Fib.Net.Core.Images.Json
{
    /** Exception thrown when trying to parse a bad image configuration format. */
    [Serializable]
    public class BadContainerConfigurationFormatException : Exception
    {
        // TODO: Potentially provide Path or source object to problem configuration file
        public BadContainerConfigurationFormatException(string message) : base(message)
        {
        }

        public BadContainerConfigurationFormatException(string message, Exception cause) : base(message, cause)
        {
        }

        protected BadContainerConfigurationFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
