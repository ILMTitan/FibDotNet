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

namespace com.google.cloud.tools.jib.docker {



























/** Tests for {@link DockerClient}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class DockerClientTest {

  [Rule] public final TemporaryFolder temporaryFolder = new TemporaryFolder();

  [Mock] private ProcessBuilder mockProcessBuilder;
  [Mock] private Process mockProcess;
  [Mock] private ImageTarball imageTarball;

  [TestInitialize]
  public void setUp() {
    Mockito.when(mockProcessBuilder.start()).thenReturn(mockProcess);

    Mockito.doAnswer(
            AdditionalAnswers.answerVoid(
                (VoidAnswer1<OutputStream>)
                    out => out.write("jib".getBytes(StandardCharsets.UTF_8))))
        .when(imageTarball)
        .writeTo(Mockito.any(typeof(OutputStream)));
  }

  [TestMethod]
  public void testIsDockerInstalled_fail() {
    Assert.assertFalse(DockerClient.isDockerInstalled(Paths.get("path/to/nonexistent/file")));
  }

  [TestMethod]
  public void testLoad() {
    DockerClient testDockerClient =
        new DockerClient(
            subcommand => {
              Assert.assertEquals(Collections.singletonList("load"), subcommand);
              return mockProcessBuilder;
            });
    Mockito.when(mockProcess.waitFor()).thenReturn(0);

    // Captures stdin.
    ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
    Mockito.when(mockProcess.getOutputStream()).thenReturn(byteArrayOutputStream);

    // Simulates stdout.
    Mockito.when(mockProcess.getInputStream())
        .thenReturn(new ByteArrayInputStream("output".getBytes(StandardCharsets.UTF_8)));

    string output = testDockerClient.load(imageTarball);

    Assert.assertEquals(
        "jib", new string(byteArrayOutputStream.toByteArray(), StandardCharsets.UTF_8));
    Assert.assertEquals("output", output);
  }

  [TestMethod]
  public void testLoad_stdinFail() {
    DockerClient testDockerClient = new DockerClient(ignored => mockProcessBuilder);

    Mockito.when(mockProcess.getOutputStream())
        .thenReturn(
            new OutputStream() {

              public void write(int b) {
                throw new IOException();
              }
            });
    Mockito.when(mockProcess.getErrorStream())
        .thenReturn(new ByteArrayInputStream("error".getBytes(StandardCharsets.UTF_8)));

    try {
      testDockerClient.load(imageTarball);
      Assert.fail("Write should have failed");

    } catch (IOException ex) {
      Assert.assertEquals("'docker load' command failed with error: error", ex.getMessage());
    }
  }

  [TestMethod]
  public void testLoad_stdinFail_stderrFail() {
    DockerClient testDockerClient = new DockerClient(ignored => mockProcessBuilder);
    IOException expectedIOException = new IOException();

    Mockito.when(mockProcess.getOutputStream())
        .thenReturn(
            new OutputStream() {

              public void write(int b) {
                throw expectedIOException;
              }
            });
    Mockito.when(mockProcess.getErrorStream())
        .thenReturn(
            new InputStream() {

              public int read() {
                throw new IOException();
              }
            });

    try {
      testDockerClient.load(imageTarball);
      Assert.fail("Write should have failed");

    } catch (IOException ex) {
      Assert.assertSame(expectedIOException, ex);
    }
  }

  [TestMethod]
  public void testLoad_stdoutFail() {
    DockerClient testDockerClient = new DockerClient(ignored => mockProcessBuilder);
    Mockito.when(mockProcess.waitFor()).thenReturn(1);

    Mockito.when(mockProcess.getOutputStream()).thenReturn(ByteStreams.nullOutputStream());
    Mockito.when(mockProcess.getInputStream())
        .thenReturn(new ByteArrayInputStream("ignored".getBytes(StandardCharsets.UTF_8)));
    Mockito.when(mockProcess.getErrorStream())
        .thenReturn(new ByteArrayInputStream("error".getBytes(StandardCharsets.UTF_8)));

    try {
      testDockerClient.load(imageTarball);
      Assert.fail("Process should have failed");

    } catch (IOException ex) {
      Assert.assertEquals("'docker load' command failed with output: error", ex.getMessage());
    }
  }

  [TestMethod]
  public void testTag() {
    DockerClient testDockerClient =
        new DockerClient(
            subcommand => {
              Assert.assertEquals(Arrays.asList("tag", "original", "new"), subcommand);
              return mockProcessBuilder;
            });
    Mockito.when(mockProcess.waitFor()).thenReturn(0);

    testDockerClient.tag(ImageReference.of(null, "original", null), ImageReference.parse("new"));
  }

  [TestMethod]
  public void testDefaultProcessorBuilderFactory_customExecutable() {
    ProcessBuilder processBuilder =
        DockerClient.defaultProcessBuilderFactory("docker-executable", ImmutableMap.of())
            .apply(Arrays.asList("sub", "command"));

    Assert.assertEquals(
        Arrays.asList("docker-executable", "sub", "command"), processBuilder.command());
    Assert.assertEquals(System.getenv(), processBuilder.environment());
  }

  [TestMethod]
  public void testDefaultProcessorBuilderFactory_customEnvironment() {
    ImmutableMap<string, string> environment = ImmutableMap.of("Key1", "Value1");

    Map<string, string> expectedEnvironment = new HashMap<>(System.getenv());
    expectedEnvironment.putAll(environment);

    ProcessBuilder processBuilder =
        DockerClient.defaultProcessBuilderFactory("docker", environment)
            .apply(Collections.emptyList());

    Assert.assertEquals(expectedEnvironment, processBuilder.environment());
  }

  [TestMethod]
  public void testTag_fail() {
    DockerClient testDockerClient =
        new DockerClient(
            subcommand => {
              Assert.assertEquals(Arrays.asList("tag", "original", "new"), subcommand);
              return mockProcessBuilder;
            });
    Mockito.when(mockProcess.waitFor()).thenReturn(1);

    Mockito.when(mockProcess.getErrorStream())
        .thenReturn(new ByteArrayInputStream("error".getBytes(StandardCharsets.UTF_8)));

    try {
      testDockerClient.tag(ImageReference.of(null, "original", null), ImageReference.parse("new"));
      Assert.fail("docker tag should have failed");

    } catch (IOException ex) {
      Assert.assertEquals("'docker tag' command failed with error: error", ex.getMessage());
    }
  }
}
}
