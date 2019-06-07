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

namespace com.google.cloud.tools.jib.http {



/** Tests for {@link Request}. */
public class RequestTest {

  [TestMethod]
  public void testGetHttpTimeout() {
    Request request = Request.builder().build();

    Assert.assertNull(request.getHttpTimeout());
  }

  [TestMethod]
  public void testSetHttpTimeout() {
    Request request = Request.builder().setHttpTimeout(3000).build();

    Assert.assertEquals(Integer.valueOf(3000), request.getHttpTimeout());
  }
}
}
