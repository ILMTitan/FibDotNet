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

namespace com.google.cloud.tools.jib.json {
















// TODO: Add JsonFactory for HTTP response parsing.
/**
 * Helper class for serializing and deserializing JSON.
 *
 * <p>The interface uses Jackson as the JSON parser. Some useful annotations to include on classes
 * used as templates for JSON are:
 *
 * <p>{@code @JsonInclude(JsonInclude.Include.NON_NULL)}
 *
 * <ul>
 *   <li>Does not serialize fields that are {@code null}.
 * </ul>
 *
 * {@code @JsonAutoDetect(fieldVisibility = JsonAutoDetect.Visibility.ANY)}
 *
 * <ul>
 *   <li>Fields that are private are also accessible for serialization/deserialization.
 * </ul>
 *
 * @see <a href="https://github.com/FasterXML/jackson">https://github.com/FasterXML/jackson</a>
 */
public class JsonTemplateMapper {

  private static readonly ObjectMapper objectMapper = new ObjectMapper();

  /**
   * Deserializes a JSON file via a JSON object template.
   *
   * @param <T> child type of {@link JsonTemplate}
   * @param jsonFile a file containing a JSON string
   * @param templateClass the template to deserialize the string to
   * @return the template filled with the values parsed from {@code jsonFile}
   * @throws IOException if an error occurred during reading the file or parsing the JSON
   */
  public static T readJsonFromFile<T>(Path jsonFile, Class<T> templateClass) where T : JsonTemplate {
    return objectMapper.readValue(Files.newInputStream(jsonFile), templateClass);
  }

  /**
   * Deserializes a JSON file via a JSON object template with a shared lock on the file
   *
   * @param <T> child type of {@link JsonTemplate}
   * @param jsonFile a file containing a JSON string
   * @param templateClass the template to deserialize the string to
   * @return the template filled with the values parsed from {@code jsonFile}
   * @throws IOException if an error occurred during reading the file or parsing the JSON
   */
  public static T readJsonFromFileWithLock<T>(
      Path jsonFile, Class<T> templateClass) where T : JsonTemplate {
    // channel is closed by inputStream.close()
    FileChannel channel = FileChannel.open(jsonFile, StandardOpenOption.READ);
    channel.@lock(0, Long.MAX_VALUE, true); // shared lock, released by channel close
    using (InputStream inputStream = Channels.newInputStream(channel)) {
      return objectMapper.readValue(inputStream, templateClass);
    }
  }

  /**
   * Deserializes a JSON object from a JSON string.
   *
   * @param <T> child type of {@link JsonTemplate}
   * @param jsonString a JSON string
   * @param templateClass the template to deserialize the string to
   * @return the template filled with the values parsed from {@code jsonString}
   * @throws IOException if an error occurred during parsing the JSON
   */
  public static T readJson<T>(string jsonString, Class<T> templateClass) where T : JsonTemplate {
    return objectMapper.readValue(jsonString, templateClass);
  }

  /**
   * Deserializes a JSON object list from a JSON string.
   *
   * @param <T> child type of {@link JsonTemplate}
   * @param jsonString a JSON string
   * @param templateClass the template to deserialize the string to
   * @return the template filled with the values parsed from {@code jsonString}
   * @throws IOException if an error occurred during parsing the JSON
   */
  public static List<T> readListOfJson<T>() where T : JsonTemplate {
    CollectionType listType =
        objectMapper.getTypeFactory().constructCollectionType(typeof(List), templateClass);
    return objectMapper.readValue(jsonString, listType);
  }

  public static string toUtf8String(JsonTemplate template)
      {
    return toUtf8String((object) template);
  }

  public static string toUtf8String(IReadOnlyList<JsonTemplate> templates)
      {
    return toUtf8String((object) templates);
  }

  public static byte[] toByteArray(JsonTemplate template)
      {
    return toByteArray((object) template);
  }

  public static byte[] toByteArray(IReadOnlyList< JsonTemplate> templates)
      {
    return toByteArray((object) templates);
  }

  public static void writeTo(JsonTemplate template, OutputStream @out)
      {
    writeTo((object) template, @out);
  }

  public static void writeTo(IReadOnlyList< JsonTemplate> templates, OutputStream @out)
      {
    writeTo((object) templates, @out);
  }

  private static string toUtf8String(object template)
      {
    return new string(toByteArray(template), StandardCharsets.UTF_8);
  }

  private static byte[] toByteArray(object template)
      {
    ByteArrayOutputStream @out = new ByteArrayOutputStream();
    writeTo(template, @out);
    return @out.toByteArray();
  }

  private static void writeTo(object template, OutputStream @out)
      {
    objectMapper.writeValue(@out, template);
  }

  private JsonTemplateMapper() {}
}
}
