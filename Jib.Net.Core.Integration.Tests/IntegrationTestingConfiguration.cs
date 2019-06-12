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

using com.google.cloud.tools.jib.api;
using NUnit.Framework;
using System;

namespace com.google.cloud.tools.jib
{
    /** Configuration for integration tests. */
    public sealed class IntegrationTestingConfiguration
    {
        public static string getGCPProject()
        {
            string projectId = Environment.GetEnvironmentVariable("JIB_INTEGRATION_TESTING_PROJECT");
            if (Strings.isNullOrEmpty(projectId))
            {
                Assert.Fail(
                    "Must set environment variable JIB_INTEGRATION_TESTING_PROJECT to the GCP project to use for integration testing.");
            }
            return projectId;
        }

        private IntegrationTestingConfiguration() { }
    }
}
