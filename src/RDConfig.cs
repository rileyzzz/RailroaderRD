//using System.Text.Json;
//using System.Text.Json.Serialization;

using System.Collections.Generic;

namespace RailroaderRD;

internal class RDConfig
{
    //[JsonIgnore]
    public static RDConfig Current { get; set; }

    public CalibrationData CalibrationData { get; set; } = new();

    public bool AutoConnectOnStart { get; set; } = true;

    // public Dictionary<string, int> ControlBindings { get; set; } = new();
}