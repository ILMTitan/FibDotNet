﻿// Copyright 2018 Google LLC.
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
using System.Runtime.Serialization;

namespace Fib.Net.Core.Registry
{
    [Serializable]
    internal class ConnectException : IOException
    {
        public ConnectException()
        {
        }

        public ConnectException(string message) : base(message)
        {
        }

        public ConnectException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ConnectException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ConnectException(string message, int hresult) : base(message, hresult)
        {
        }
    }
}