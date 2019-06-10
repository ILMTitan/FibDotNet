﻿/*
 * Copyright 2018 Google LLC.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

using System;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Net;
using System.Net.Http.Headers;

namespace com.google.cloud.tools.jib.registry
{
    [Serializable]
    public class HttpResponseException : Exception
    {

        public HttpResponseException()
        {
        }

        public HttpResponseException(HttpResponseMessage message)
        {
            this.Cause = message;
        }

        public HttpResponseException(string message) : base(message)
        {
        }

        public HttpResponseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected HttpResponseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public HttpResponseMessage Cause { get; }

        public HttpStatusCode getStatusCode()
        {
            return Cause.StatusCode;
        }
        public HttpResponseHeaders getHeaders()
        {
            return Cause.Headers;
        }

        public HttpContent getContent()
        {
            return Cause.Content;
        }
    }
}