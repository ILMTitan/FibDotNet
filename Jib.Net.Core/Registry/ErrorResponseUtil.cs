/*
 * Copyright 2018 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using com.google.cloud.tools.jib.json;
using com.google.cloud.tools.jib.registry.json;
using Jib.Net.Core.Global;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace com.google.cloud.tools.jib.registry {







/** Utility methods for parsing {@link ErrorResponseTemplate JSON-encoded error responses}. */
public class ErrorResponseUtil {

  /**
   * Extract an {@link ErrorCodes} response from the error object encoded in an {@link
   * HttpResponseException}.
   *
   * @param httpResponseException the response exception
   * @return the parsed {@link ErrorCodes} if found
   * @throws HttpResponseException rethrows the original exception if an error object could not be
   *     parsed, if there were multiple error objects, or if the error code is unknown.
   */
  public static ErrorCodes getErrorCode(HttpResponseMessage httpResponse)
      {
    // Obtain the error response code.
    string errorContent = httpResponse.getContent();
    if (errorContent == null) {
      throw new HttpResponseException(httpResponse);
    }

    try {
      ErrorResponseTemplate errorResponse =
          JsonTemplateMapper.readJson< ErrorResponseTemplate>(errorContent);
      IReadOnlyList<ErrorEntryTemplate> errors = errorResponse.getErrors();
      // There may be multiple error objects
      if (errors.size() == 1) {
        string errorCodeString = errors.get(0).getCode();
        // May not get an error code back.
        if (Enum.TryParse(errorCodeString, out ErrorCodes result))
                    {
                        return result;
                    }
        }
      

    } catch (Exception e) when (e is IOException || e is ArgumentException) {
      // Parse exception: either isn't an error object or unknown error code
    }

    // rethrow the original exception
    throw new HttpResponseException(httpResponse);
  }

  // not intended to be instantiated
  private ErrorResponseUtil() {}
}
}
