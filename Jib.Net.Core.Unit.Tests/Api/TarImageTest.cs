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

using Jib.Net.Core.Api;
using Jib.Net.Core.Global;
using NUnit.Framework;

namespace com.google.cloud.tools.jib.api
{
    /** Tests for {@link TarImage}. */

    public class TarImageTest
    {
        [Test]
        public void TestGetters()
        {
            TarImage tarImage = TarImage.Named("tar/image").SaveTo(Paths.Get("output/file"));
            Assert.AreEqual("tar/image", JavaExtensions.ToString(tarImage.GetImageReference()));
            Assert.AreEqual(Paths.Get("output/file"), tarImage.GetOutputFile());
        }
    }
}