/*
 * Copyright 2018 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using com.google.cloud.tools.jib.api;
using Jib.Net.Core.Global;
using NUnit.Framework;
using System;

namespace com.google.cloud.tools.jib.registry
{
    /** Tests for {@link RegistryAuthenticationFailedException}. */
    public class RegistryAuthenticationFailedExceptionTest
    {
        [Test]
        public void testRegistryAuthenticationFailedException_message()
        {
            RegistryAuthenticationFailedException exception =
                new RegistryAuthenticationFailedException("serverUrl", "imageName", "message");
            Assert.AreEqual("serverUrl", exception.getRegistry());
            Assert.AreEqual("imageName", exception.getImageName());
            Assert.AreEqual(
                "Failed to authenticate with registry serverUrl/imageName because: message",
                exception.getMessage());
        }

        [Test]
        public void testRegistryAuthenticationFailedException_exception()
        {
            Exception cause = new Exception("message");
            RegistryAuthenticationFailedException exception =
                new RegistryAuthenticationFailedException("serverUrl", "imageName", cause);
            Assert.AreEqual("serverUrl", exception.getRegistry());
            Assert.AreEqual("imageName", exception.getImageName());
            Assert.AreSame(cause, exception.getCause());
            Assert.AreEqual(
                "Failed to authenticate with registry serverUrl/imageName because: message",
                exception.getMessage());
        }
    }
}
