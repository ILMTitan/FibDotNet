/*
 * Copyright 2017 Google LLC.
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

namespace com.google.cloud.tools.jib.image {


















/** Tests for {@link Image}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class ImageTest {

  [Mock] private Layer mockLayer;
  [Mock] private DescriptorDigest mockDescriptorDigest;

  [TestInitialize]
  public void setUp() {
    Mockito.when(mockLayer.getBlobDescriptor())
        .thenReturn(new BlobDescriptor(mockDescriptorDigest));
  }

  [TestMethod]
  public void test_smokeTest() {
    Image image =
        Image.builder(typeof(V22ManifestTemplate))
            .setCreated(Instant.ofEpochSecond(10000))
            .addEnvironmentVariable("crepecake", "is great")
            .addEnvironmentVariable("VARIABLE", "VALUE")
            .setEntrypoint(Arrays.asList("some", "command"))
            .setProgramArguments(Arrays.asList("arg1", "arg2"))
            .addExposedPorts(ImmutableSet.of(Port.tcp(1000), Port.tcp(2000)))
            .addVolumes(
                ImmutableSet.of(
                    AbsoluteUnixPath.get("/a/path"), AbsoluteUnixPath.get("/another/path")))
            .setUser("john")
            .addLayer(mockLayer)
            .build();

    Assert.assertEquals(typeof(V22ManifestTemplate), image.getImageFormat());
    Assert.assertEquals(
        mockDescriptorDigest, image.getLayers().get(0).getBlobDescriptor().getDigest());
    Assert.assertEquals(Instant.ofEpochSecond(10000), image.getCreated());
    Assert.assertEquals(
        ImmutableMap.of("crepecake", "is great", "VARIABLE", "VALUE"), image.getEnvironment());
    Assert.assertEquals(Arrays.asList("some", "command"), image.getEntrypoint());
    Assert.assertEquals(Arrays.asList("arg1", "arg2"), image.getProgramArguments());
    Assert.assertEquals(ImmutableSet.of(Port.tcp(1000), Port.tcp(2000)), image.getExposedPorts());
    Assert.assertEquals(
        ImmutableSet.of(AbsoluteUnixPath.get("/a/path"), AbsoluteUnixPath.get("/another/path")),
        image.getVolumes());
    Assert.assertEquals("john", image.getUser());
  }

  [TestMethod]
  public void testDefaults() {
    Image image = Image.builder(typeof(V22ManifestTemplate)).build();
    Assert.assertEquals("amd64", image.getArchitecture());
    Assert.assertEquals("linux", image.getOs());
    Assert.assertEquals(Collections.emptyList(), image.getLayers());
    Assert.assertEquals(Collections.emptyList(), image.getHistory());
  }

  [TestMethod]
  public void testOsArch() {
    Image image =
        Image.builder(typeof(V22ManifestTemplate)).setArchitecture("wasm").setOs("js").build();
    Assert.assertEquals("wasm", image.getArchitecture());
    Assert.assertEquals("js", image.getOs());
  }
}
}
