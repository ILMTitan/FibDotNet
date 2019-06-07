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

namespace com.google.cloud.tools.jib.image.json {








/**
 * Represents an item in the container configuration's {@code history} list.
 *
 * @see <a href=https://github.com/opencontainers/image-spec/blob/master/config.md#properties>OCI
 *     image spec ({@code history} field)</a>
 */
[JsonIgnoreProperties(ignoreUnknown = true)]
public class HistoryEntry : JsonTemplate {

  public static class Builder {

    private Instant creationTimestamp;
    private string author;
    private string createdBy;
    private string comment;
    private bool emptyLayer;

    public Builder setCreationTimestamp(Instant creationTimestamp) {
      this.creationTimestamp = creationTimestamp;
      return this;
    }

    public Builder setAuthor(string author) {
      this.author = author;
      return this;
    }

    public Builder setCreatedBy(string createdBy) {
      this.createdBy = createdBy;
      return this;
    }

    public Builder setComment(string comment) {
      this.comment = comment;
      return this;
    }

    public Builder setEmptyLayer(bool emptyLayer) {
      this.emptyLayer = emptyLayer;
      return this;
    }

    public HistoryEntry build() {
      return new HistoryEntry(
          creationTimestamp == null ? null : creationTimestamp.toString(),
          author,
          createdBy,
          comment,
          emptyLayer);
    }

    private Builder() {}
  }

  /**
   * Creates a builder for a {@link HistoryEntry}.
   *
   * @return the builder
   */
  public static Builder builder() {
    return new Builder();
  }

  /** The ISO-8601 formatted timestamp at which the image was created. */
  @JsonProperty("created")
  private string creationTimestamp;

  /** The name of the author specified when committing the image. */
  @JsonProperty("author")
  private string author;

  /** The command used to build the layer. */
  @JsonProperty("created_by")
  private string createdBy;

  /** A custom message set when creating the layer. */
  @JsonProperty("comment")
  private string comment;

  /**
   * Whether or not the entry corresponds to a layer in the container ({@code bool} to
   * make field optional).
   */
  @JsonProperty("empty_layer")
  private bool emptyLayer;

  public HistoryEntry() {}

  private HistoryEntry(
      string creationTimestamp,
      string author,
      string createdBy,
      string comment,
      bool emptyLayer) {
    this.author = author;
    this.creationTimestamp = creationTimestamp;
    this.createdBy = createdBy;
    this.comment = comment;
    this.emptyLayer = emptyLayer;
  }

  /**
   * Returns whether or not the history object corresponds to a layer in the container.
   *
   * @return {@code true} if the history object corresponds to a layer in the container
   */
  @JsonIgnore
  public bool hasCorrespondingLayer() {
    return emptyLayer == null ? false : emptyLayer;
  }

  public bool equals(object other) {
    if (this == other) {
      return true;
    }
    if (other is HistoryEntry) {
      HistoryEntry otherHistory = (HistoryEntry) other;
      return Objects.equals(otherHistory.creationTimestamp, creationTimestamp)
          && Objects.equals(otherHistory.author, author)
          && Objects.equals(otherHistory.createdBy, createdBy)
          && Objects.equals(otherHistory.comment, comment)
          && Objects.equals(otherHistory.emptyLayer, emptyLayer);
    }
    return false;
  }

  public int hashCode() {
    return Objects.hash(author, creationTimestamp, createdBy, comment, emptyLayer);
  }

  public string toString() {
    return createdBy == null ? "" : createdBy;
  }
}
}
