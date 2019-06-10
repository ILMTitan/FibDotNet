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















/** Tests for {@link MainClassFinder}. */
[RunWith(typeof(MockitoJUnitRunner))]
public class MainClassFinderTest {

  [Mock] private Consumer<LogEvent> logEventConsumer;

  [TestMethod]
  public void testFindMainClass_simple() {
    SystemPath rootDirectory = Paths.get(Resources.getResource("core/class-finder-tests/simple").toURI());
    MainClassFinder.Result mainClassFinderResult =
        MainClassFinder.find(new DirectoryWalker(rootDirectory).walk(), logEventConsumer);
    Assert.assertSame(Type.MAIN_CLASS_FOUND, mainClassFinderResult.getType());
    Assert.assertThat(
        mainClassFinderResult.getFoundMainClass(), CoreMatchers.containsString("HelloWorld"));
  }

  [TestMethod]
  public void testFindMainClass_subdirectories() {
    SystemPath rootDirectory =
        Paths.get(Resources.getResource("core/class-finder-tests/subdirectories").toURI());
    MainClassFinder.Result mainClassFinderResult =
        MainClassFinder.find(new DirectoryWalker(rootDirectory).walk(), logEventConsumer);
    Assert.assertSame(Type.MAIN_CLASS_FOUND, mainClassFinderResult.getType());
    Assert.assertThat(
        mainClassFinderResult.getFoundMainClass(),
        CoreMatchers.containsString("multi.layered.HelloWorld"));
  }

  [TestMethod]
  public void testFindMainClass_noClass() {
    SystemPath rootDirectory =
        Paths.get(Resources.getResource("core/class-finder-tests/no-main").toURI());
    MainClassFinder.Result mainClassFinderResult =
        MainClassFinder.find(new DirectoryWalker(rootDirectory).walk(), logEventConsumer);
    Assert.assertEquals(Type.MAIN_CLASS_NOT_FOUND, mainClassFinderResult.getType());
  }

  [TestMethod]
  public void testFindMainClass_multiple() {
    SystemPath rootDirectory =
        Paths.get(Resources.getResource("core/class-finder-tests/multiple").toURI());
    MainClassFinder.Result mainClassFinderResult =
        MainClassFinder.find(new DirectoryWalker(rootDirectory).walk(), logEventConsumer);
    Assert.assertEquals(
        MainClassFinder.Result.Type.MULTIPLE_MAIN_CLASSES, mainClassFinderResult.getType());
    Assert.assertEquals(2, mainClassFinderResult.getFoundMainClasses().size());
    Assert.assertTrue(
        mainClassFinderResult.getFoundMainClasses().contains("multi.layered.HelloMoon"));
    Assert.assertTrue(mainClassFinderResult.getFoundMainClasses().contains("HelloWorld"));
  }

  [TestMethod]
  public void testFindMainClass_extension() {
    SystemPath rootDirectory =
        Paths.get(Resources.getResource("core/class-finder-tests/extension").toURI());
    MainClassFinder.Result mainClassFinderResult =
        MainClassFinder.find(new DirectoryWalker(rootDirectory).walk(), logEventConsumer);
    Assert.assertSame(Type.MAIN_CLASS_FOUND, mainClassFinderResult.getType());
    Assert.assertThat(
        mainClassFinderResult.getFoundMainClass(), CoreMatchers.containsString("main.MainClass"));
  }

  [TestMethod]
  public void testFindMainClass_importedMethods() {
    SystemPath rootDirectory =
        Paths.get(Resources.getResource("core/class-finder-tests/imported-methods").toURI());
    MainClassFinder.Result mainClassFinderResult =
        MainClassFinder.find(new DirectoryWalker(rootDirectory).walk(), logEventConsumer);
    Assert.assertSame(Type.MAIN_CLASS_FOUND, mainClassFinderResult.getType());
    Assert.assertThat(
        mainClassFinderResult.getFoundMainClass(), CoreMatchers.containsString("main.MainClass"));
  }

  [TestMethod]
  public void testFindMainClass_externalClasses() {
    SystemPath rootDirectory =
        Paths.get(Resources.getResource("core/class-finder-tests/external-classes").toURI());
    MainClassFinder.Result mainClassFinderResult =
        MainClassFinder.find(new DirectoryWalker(rootDirectory).walk(), logEventConsumer);
    Assert.assertSame(Type.MAIN_CLASS_FOUND, mainClassFinderResult.getType());
    Assert.assertThat(
        mainClassFinderResult.getFoundMainClass(), CoreMatchers.containsString("main.MainClass"));
  }

  [TestMethod]
  public void testFindMainClass_innerClasses() {
    SystemPath rootDirectory =
        Paths.get(Resources.getResource("core/class-finder-tests/inner-classes").toURI());
    MainClassFinder.Result mainClassFinderResult =
        MainClassFinder.find(new DirectoryWalker(rootDirectory).walk(), logEventConsumer);
    Assert.assertSame(Type.MAIN_CLASS_FOUND, mainClassFinderResult.getType());
    Assert.assertThat(
        mainClassFinderResult.getFoundMainClass(),
        CoreMatchers.containsString("HelloWorld$InnerClass"));
  }

  [TestMethod]
  public void testMainClass_varargs() {
    SystemPath rootDirectory =
        Paths.get(Resources.getResource("core/class-finder-tests/varargs").toURI());
    MainClassFinder.Result mainClassFinderResult =
        MainClassFinder.find(new DirectoryWalker(rootDirectory).walk(), logEventConsumer);
    Assert.assertSame(Type.MAIN_CLASS_FOUND, mainClassFinderResult.getType());
    Assert.assertThat(
        mainClassFinderResult.getFoundMainClass(), CoreMatchers.containsString("HelloWorld"));
  }
}
}
