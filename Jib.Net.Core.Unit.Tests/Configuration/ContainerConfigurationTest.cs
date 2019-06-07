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

namespace com.google.cloud.tools.jib.configuration {













/** Tests for {@link ContainerConfiguration}. */
public class ContainerConfigurationTest {

  [TestMethod]
  public void testBuilder_nullValues() {
    // Java arguments element should not be null.
    try {
      ContainerConfiguration.builder().setProgramArguments(Arrays.asList("first", null));
      Assert.fail("The IllegalArgumentException should be thrown.");
    } catch (IllegalArgumentException ex) {
      Assert.assertEquals("program arguments list contains null elements", ex.getMessage());
    }

    // Entrypoint element should not be null.
    try {
      ContainerConfiguration.builder().setEntrypoint(Arrays.asList("first", null));
      Assert.fail("The IllegalArgumentException should be thrown.");
    } catch (IllegalArgumentException ex) {
      Assert.assertEquals("entrypoint contains null elements", ex.getMessage());
    }

    // Exposed ports element should not be null.
    Set<Port> badPorts = new HashSet<>(Arrays.asList(Port.tcp(1000), null));
    try {
      ContainerConfiguration.builder().setExposedPorts(badPorts);
      Assert.fail("The IllegalArgumentException should be thrown.");
    } catch (IllegalArgumentException ex) {
      Assert.assertEquals("ports list contains null elements", ex.getMessage());
    }

    // Volume element should not be null.
    Set<AbsoluteUnixPath> badVolumes =
        new HashSet<>(Arrays.asList(AbsoluteUnixPath.get("/"), null));
    try {
      ContainerConfiguration.builder().setVolumes(badVolumes);
      Assert.fail("The IllegalArgumentException should be thrown.");
    } catch (IllegalArgumentException ex) {
      Assert.assertEquals("volumes list contains null elements", ex.getMessage());
    }

    Map<string, string> nullKeyMap = new HashMap<>();
    nullKeyMap.put(null, "value");
    Map<string, string> nullValueMap = new HashMap<>();
    nullValueMap.put("key", null);

    // Label keys should not be null.
    try {
      ContainerConfiguration.builder().setLabels(nullKeyMap);
      Assert.fail("The IllegalArgumentException should be thrown.");
    } catch (IllegalArgumentException ex) {
      Assert.assertEquals("labels map contains null keys", ex.getMessage());
    }

    // Labels values should not be null.
    try {
      ContainerConfiguration.builder().setLabels(nullValueMap);
      Assert.fail("The IllegalArgumentException should be thrown.");
    } catch (IllegalArgumentException ex) {
      Assert.assertEquals("labels map contains null values", ex.getMessage());
    }

    // Environment keys should not be null.
    try {
      ContainerConfiguration.builder().setEnvironment(nullKeyMap);
      Assert.fail("The IllegalArgumentException should be thrown.");
    } catch (IllegalArgumentException ex) {
      Assert.assertEquals("environment map contains null keys", ex.getMessage());
    }

    // Environment values should not be null.
    try {
      ContainerConfiguration.builder().setEnvironment(nullValueMap);
      Assert.fail("The IllegalArgumentException should be thrown.");
    } catch (IllegalArgumentException ex) {
      Assert.assertEquals("environment map contains null values", ex.getMessage());
    }
  }

  [TestMethod]
  @SuppressWarnings("JdkObsolete") // for hashtable
  public void testBuilder_environmentMapTypes() {
    // Can accept empty environment.
    ContainerConfiguration.builder().setEnvironment(ImmutableMap.of()).build();

    // Can handle other map types (https://github.com/GoogleContainerTools/jib/issues/632)
    ContainerConfiguration.builder().setEnvironment(new TreeMap<>());
    ContainerConfiguration.builder().setEnvironment(new Hashtable<>());
  }

  [TestMethod]
  public void testBuilder_user() {
    ContainerConfiguration configuration = ContainerConfiguration.builder().setUser("john").build();
    Assert.assertEquals("john", configuration.getUser());
  }

  [TestMethod]
  public void testBuilder_workingDirectory() {
    ContainerConfiguration configuration =
        ContainerConfiguration.builder().setWorkingDirectory(AbsoluteUnixPath.get("/path")).build();
    Assert.assertEquals(AbsoluteUnixPath.get("/path"), configuration.getWorkingDirectory());
  }
}
}
