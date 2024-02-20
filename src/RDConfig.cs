//using System.Text.Json;
//using System.Text.Json.Serialization;

namespace RailroaderRD;

internal class RDConfig
{
    //[JsonIgnore]
    public static RDConfig Current { get; set; }

    public CalibrationData CalibrationData { get; set; }

}