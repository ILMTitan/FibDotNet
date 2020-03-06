/*
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

using Fib.Net.Test.Common;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Fib.Net.Core.Integration.Tests.Registry
{
    [TestFixture]
    public abstract class HttpRegistryTest
    {
        public static readonly LocalRegistry localRegistry = new LocalRegistry(5000);

        [OneTimeSetUp]
        public static async Task StartLocalRegistryAsync()
        {
            try
            {
                await localRegistry.StartAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await TestContext.Out.WriteLineAsync(e.ToString()).ConfigureAwait(false);
                throw new Exception(e.ToString(), e);
            }
        }

        [OneTimeTearDown]
        public static void StopLocalRegistry()
        {
            localRegistry.Stop();
        }
    }
}