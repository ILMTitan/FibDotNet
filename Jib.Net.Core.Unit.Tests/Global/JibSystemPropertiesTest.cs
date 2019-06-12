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

using Jib.Net.Core.Global;
using NUnit.Framework;
using System;

namespace com.google.cloud.tools.jib.global {






/** Tests for {@link JibSystemProperties}. */
public class JibSystemPropertiesTest {

  [SetUp]
  public void setUp() {
  }

  [TearDown]
  public void tearDown() {
    Environment.SetEnvironmentVariable((JibSystemProperties.HTTP_TIMEOUT), null);
  }

  [Test]
  public void testCheckHttpTimeoutProperty_ok() {
    Assert.IsNull(Environment.GetEnvironmentVariable(JibSystemProperties.HTTP_TIMEOUT));
    JibSystemProperties.checkHttpTimeoutProperty();
  }

  [Test]
  public void testCheckHttpTimeoutProperty_stringValue() {
    Environment.SetEnvironmentVariable(JibSystemProperties.HTTP_TIMEOUT, "random string");
    try {
      JibSystemProperties.checkHttpTimeoutProperty();
      Assert.Fail();
    } catch (FormatException ex) {
      Assert.AreEqual("jib.httpTimeout must be an integer: random string", ex.getMessage());
    }
  }

  [Test]
  public void testCheckHttpProxyPortProperty_undefined() {
            Environment.SetEnvironmentVariable("http.proxyPort", null);
            Environment.SetEnvironmentVariable("https.proxyPort", null);
    JibSystemProperties.checkProxyPortProperty();
  }

  [Test]
  public void testCheckHttpProxyPortProperty() {
    Environment.SetEnvironmentVariable("http.proxyPort", "0");
    Environment.SetEnvironmentVariable("https.proxyPort", "0");
    JibSystemProperties.checkProxyPortProperty();

    Environment.SetEnvironmentVariable("http.proxyPort", "1");
    Environment.SetEnvironmentVariable("https.proxyPort", "1");
    JibSystemProperties.checkProxyPortProperty();

    Environment.SetEnvironmentVariable("http.proxyPort", "65535");
    Environment.SetEnvironmentVariable("https.proxyPort", "65535");
    JibSystemProperties.checkProxyPortProperty();

    Environment.SetEnvironmentVariable("http.proxyPort", "65534");
    Environment.SetEnvironmentVariable("https.proxyPort", "65534");
    JibSystemProperties.checkProxyPortProperty();
  }

  [Test]
  public void testCheckHttpProxyPortProperty_negativeValue() {
    Environment.SetEnvironmentVariable("http.proxyPort", "-1");
            Environment.SetEnvironmentVariable("https.proxyPort", null);
    try {
      JibSystemProperties.checkProxyPortProperty();
      Assert.Fail();
    } catch (FormatException ex) {
      Assert.AreEqual("http.proxyPort cannot be less than 0: -1", ex.getMessage());
    }

            Environment.SetEnvironmentVariable("http.proxyPort", null);
    Environment.SetEnvironmentVariable("https.proxyPort", "-1");
    try {
      JibSystemProperties.checkProxyPortProperty();
      Assert.Fail();
    } catch (FormatException ex) {
      Assert.AreEqual("https.proxyPort cannot be less than 0: -1", ex.getMessage());
    }
  }

  [Test]
  public void testCheckHttpProxyPortProperty_over65535() {
    Environment.SetEnvironmentVariable("http.proxyPort", "65536");
            Environment.SetEnvironmentVariable("https.proxyPort", null);
    try {
      JibSystemProperties.checkProxyPortProperty();
      Assert.Fail();
    } catch (FormatException ex) {
      Assert.AreEqual("http.proxyPort cannot be greater than 65535: 65536", ex.getMessage());
    }

            Environment.SetEnvironmentVariable("http.proxyPort", null);
    Environment.SetEnvironmentVariable("https.proxyPort", "65536");
    try {
      JibSystemProperties.checkProxyPortProperty();
      Assert.Fail();
    } catch (FormatException ex) {
      Assert.AreEqual("https.proxyPort cannot be greater than 65535: 65536", ex.getMessage());
    }
  }

  [Test]
  public void testCheckHttpProxyPortProperty_stringValue() {
    Environment.SetEnvironmentVariable("http.proxyPort", "some string");
            Environment.SetEnvironmentVariable("https.proxyPort", null);
    try {
      JibSystemProperties.checkProxyPortProperty();
      Assert.Fail();
    } catch (FormatException ex) {
      Assert.AreEqual("http.proxyPort must be an integer: some string", ex.getMessage());
    }

            Environment.SetEnvironmentVariable("http.proxyPort", null);
    Environment.SetEnvironmentVariable("https.proxyPort", "some string");
    try {
      JibSystemProperties.checkProxyPortProperty();
      Assert.Fail();
    } catch (FormatException ex) {
      Assert.AreEqual("https.proxyPort must be an integer: some string", ex.getMessage());
    }
  }
}
}
