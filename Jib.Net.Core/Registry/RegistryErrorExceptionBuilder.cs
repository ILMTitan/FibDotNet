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

using com.google.cloud.tools.jib.registry.json;
using Jib.Net.Core.Global;
using System;
using System.Net.Http;
using System.Text;

namespace com.google.cloud.tools.jib.registry {




/** Builds a {@link RegistryErrorException} with multiple causes. */
public class RegistryErrorExceptionBuilder {

  private readonly HttpResponseMessage cause;
  private readonly StringBuilder errorMessageBuilder = new StringBuilder();

  private bool firstErrorReason = true;

  /**
   * Gets the reason for certain errors.
   *
   * @param errorCodeString string form of {@link ErrorCodes}
   * @param message the original received error message, which may or may not be used depending on
   *     the {@code errorCode}
   */
  private static string getReason(string errorCodeString, string message) {
    if (message == null) {
      message = "no details";
    }

      if (!Enum.TryParse<ErrorCodes>(errorCodeString, true, out var errorCode))
                {
                    // Unknown errorCodeString
                    return "unknown: " + message;
                }

                

      if (errorCode == ErrorCodes.MANIFEST_INVALID || errorCode == ErrorCodes.BLOB_UNKNOWN) {
        return message + " (something went wrong)";

      } else if (errorCode == ErrorCodes.MANIFEST_UNKNOWN
          || errorCode == ErrorCodes.TAG_INVALID
          || errorCode == ErrorCodes.MANIFEST_UNVERIFIED) {
        return message;

      } else {
        return "other: " + message;
      }
  }

  /** @param method the registry method that errored */
  public RegistryErrorExceptionBuilder(string method, HttpResponseMessage cause) {
    this.cause = cause;

    errorMessageBuilder.append("Tried to ");
    errorMessageBuilder.append(method);
    errorMessageBuilder.append(" but failed because: ");
  }

  /** @param method the registry method that errored */
  public RegistryErrorExceptionBuilder(string method) : this(method, null) {
    
  }

  // TODO: Don't use a JsonTemplate as a data object to pass around.
  /**
   * Builds an entry to the error reasons from an {@link ErrorEntryTemplate}.
   *
   * @param errorEntry the {@link ErrorEntryTemplate} to add
   */
  public RegistryErrorExceptionBuilder addReason(ErrorEntryTemplate errorEntry) {
    string reason = getReason(errorEntry.getCode(), errorEntry.getMessage());
    addReason(reason);
    return this;
  }

  /** Adds an entry to the error reasons. */
  public RegistryErrorExceptionBuilder addReason(string reason) {
    if (!firstErrorReason) {
      errorMessageBuilder.append(", ");
    }
    errorMessageBuilder.append(reason);
    firstErrorReason = false;
    return this;
  }

  public RegistryErrorException build() {
    // Provides a feedback channel.
    errorMessageBuilder.append(
        " | If this is a bug, please file an issue at " + ProjectInfo.GITHUB_NEW_ISSUE_URL);
    return new RegistryErrorException(errorMessageBuilder.toString(), cause);
  }
}
}
