using Jib.Net.Core.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jib.Net.Cli
{
    public class JsonPortsConverter : JsonConverter<IReadOnlyCollection<Port>>
    {
        public override IReadOnlyCollection<Port> ReadJson(JsonReader reader, Type objectType, IReadOnlyCollection<Port> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            IEnumerable<string> ports = serializer.Deserialize<IEnumerable<string>>(reader);
            return Port.Parse(ports);
        }

        public override void WriteJson(JsonWriter writer, IReadOnlyCollection<Port> value, JsonSerializer serializer)
        {
            serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            serializer.Serialize(writer, value.Select(p => p.ToString()));
        }
    }
}