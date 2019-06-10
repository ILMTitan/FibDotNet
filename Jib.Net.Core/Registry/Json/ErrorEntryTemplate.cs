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

using com.google.cloud.tools.jib.json;

namespace com.google.cloud.tools.jib.registry.json {




// TODO: Should include detail field as well - need to have custom parser
[JsonIgnoreProperties(ignoreUnknown = true)]
public class ErrorEntryTemplate : JsonTemplate {

  private string code;
  private string message;

  public ErrorEntryTemplate(string code, string message) {
    this.code = code;
    this.message = message;
  }

  private ErrorEntryTemplate() {}

  public string getCode() {
    return code;
  }

  public string getMessage() {
    return message;
  }
}
}
