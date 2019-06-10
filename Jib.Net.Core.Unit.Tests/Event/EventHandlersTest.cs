/*
 * Copyright 2018 Google LLC. All rights reserved.
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

namespace com.google.cloud.tools.jib.event {









/** Tests for {@link EventHandlers}. */
public class EventHandlersTest {

  /** Test {@link JibEvent}. */
  private interface TestJibEvent1 extends JibEvent {

    string getPayload();
  }

  /** Test implementation of {@link JibEvent}. */
  private class TestJibEvent2 : JibEvent  {
    private string message;

    private void assertMessageCorrect(string name) {
      Assert.assertEquals("Hello " + name, message);
    }

    private void sayHello(string name) {
      Assert.assertNull(message);
      message = "Hello " + name;
    }
  }

  /** Test {@link JibEvent}. */
  private class TestJibEvent3 implements JibEvent {}

  [TestMethod]
  public void testAdd() {
    int[] counter = new int[1];
    EventHandlers eventHandlers =
        EventHandlers.builder()
            .add(
                typeof(TestJibEvent1),
                testJibEvent1 => Assert.assertEquals("payload", testJibEvent1.getPayload()))
            .add(typeof(TestJibEvent2), testJibEvent2 => testJibEvent2.sayHello("Jib"))
            .add(typeof(JibEvent), jibEvent => counter[0]++)
            .build();
    Assert.assertTrue(eventHandlers.getHandlers().containsKey(typeof(JibEvent)));
    Assert.assertTrue(eventHandlers.getHandlers().containsKey(typeof(TestJibEvent1)));
    Assert.assertTrue(eventHandlers.getHandlers().containsKey(typeof(TestJibEvent2)));
    Assert.assertEquals(1, eventHandlers.getHandlers().get(typeof(JibEvent)).size());
    Assert.assertEquals(1, eventHandlers.getHandlers().get(typeof(TestJibEvent1)).size());
    Assert.assertEquals(1, eventHandlers.getHandlers().get(typeof(TestJibEvent2)).size());

    TestJibEvent1 mockTestJibEvent1 = Mockito.mock(typeof(TestJibEvent1));
    Mockito.when(mockTestJibEvent1.getPayload()).thenReturn("payload");
    TestJibEvent2 testJibEvent2 = new TestJibEvent2();

    // Checks that the handlers handled their respective event types.
    eventHandlers.getHandlers().get(typeof(JibEvent)).asList().get(0).handle(mockTestJibEvent1);
    eventHandlers.getHandlers().get(typeof(JibEvent)).asList().get(0).handle(testJibEvent2);
    eventHandlers.getHandlers().get(typeof(TestJibEvent1)).asList().get(0).handle(mockTestJibEvent1);
    eventHandlers.getHandlers().get(typeof(TestJibEvent2)).asList().get(0).handle(testJibEvent2);

    Assert.assertEquals(2, counter[0]);
    Mockito.verify(mockTestJibEvent1).getPayload();
    Mockito.verifyNoMoreInteractions(mockTestJibEvent1);
    testJibEvent2.assertMessageCorrect("Jib");
  }

  [TestMethod]
  public void testDispatch() {
    IList<string> emissions = new List<>();

    EventHandlers eventHandlers =
        EventHandlers.builder()
            .add(typeof(TestJibEvent2), testJibEvent2 => emissions.add("handled 2 first"))
            .add(typeof(TestJibEvent2), testJibEvent2 => emissions.add("handled 2 second"))
            .add(typeof(TestJibEvent3), testJibEvent3 => emissions.add("handled 3"))
            .add(typeof(JibEvent), jibEvent => emissions.add("handled generic"))
            .build();

    TestJibEvent2 testJibEvent2 = new TestJibEvent2();
    TestJibEvent3 testJibEvent3 = new TestJibEvent3();

    eventHandlers.dispatch(testJibEvent2);
    eventHandlers.dispatch(testJibEvent3);

    Assert.assertEquals(
        Arrays.asList(
            "handled generic",
            "handled 2 first",
            "handled 2 second",
            "handled generic",
            "handled 3"),
        emissions);
  }
}
}
