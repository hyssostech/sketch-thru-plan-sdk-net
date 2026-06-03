using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StpSDK;

[JsonObject]
public class ObjectSet : IEnumerable<StpObject>
{
    [JsonProperty("objects")]
    public List<StpObject> Objects { get; set; } = new List<StpObject>();

    [JsonIgnore]
    public int Count => Objects?.Count ?? 0;

    public ObjectSet() { }

    public ObjectSet(List<StpObject> objects)
    {
        Objects = objects ?? new List<StpObject>();
    }

    public IEnumerator<StpObject> GetEnumerator() => Objects.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public List<StpObject> GetTypedObjects()
    {
        return Objects?.Select(o => o.AsTypedObject()).ToList() ?? new List<StpObject>();
    }
}
