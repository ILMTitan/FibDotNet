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

namespace com.google.cloud.tools.jib.api {




/** Tests for {@link TarImage}. */
public class TarImageTest {

  [TestMethod]
  public void testGetters() {
    TarImage tarImage = TarImage.named("tar/image").saveTo(Paths.get("output/file"));
    Assert.assertEquals("tar/image", tarImage.getImageReference().toString());
    Assert.assertEquals(Paths.get("output/file"), tarImage.getOutputFile());
  }
}
}
