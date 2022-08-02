using System;
using Newtonsoft.Json;

namespace PoeTexasCorTap
{
    public class LightingDeviceState
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("displayname")]
        public string Displayname { get; set; }

        [JsonProperty("iface")]
        public Iface Iface { get; set; }

        [JsonProperty("outputid")]
        public string Outputid { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("commonanode")]
        public bool Commonanode { get; set; }

        [JsonProperty("twelvevolt")]
        public bool Twelvevolt { get; set; }

        [JsonProperty("parameters")]
        public Parameters Parameters { get; set; }

        [JsonProperty("powerwatts")]
        public long Powerwatts { get; set; }

        [JsonProperty("daylightlimited")]
        public bool Daylightlimited { get; set; }

        [JsonProperty("occupiedstate")]
        public bool Occupiedstate { get; set; }

        [JsonProperty("dmxaddr")]
        public long Dmxaddr { get; set; }

        [JsonProperty("level", NullValueHandling = NullValueHandling.Ignore)]
        public long? Level { get; set; }

        [JsonProperty("brightness", NullValueHandling = NullValueHandling.Ignore)]
        public long? Brightness { get; set; }

        [JsonProperty("colortemp", NullValueHandling = NullValueHandling.Ignore)]
        public long? Colortemp { get; set; }

        [JsonProperty("red", NullValueHandling = NullValueHandling.Ignore)]
        public long? Red { get; set; }

        [JsonProperty("green", NullValueHandling = NullValueHandling.Ignore)]
        public long? Green { get; set; }

        [JsonProperty("blue", NullValueHandling = NullValueHandling.Ignore)]
        public long? Blue { get; set; }

        [JsonProperty("white", NullValueHandling = NullValueHandling.Ignore)]
        public long? White { get; set; }

        [JsonProperty("amber", NullValueHandling = NullValueHandling.Ignore)]
        public long? Amber { get; set; }

        [JsonProperty("warmwhite", NullValueHandling = NullValueHandling.Ignore)]
        public long? Warmwhite { get; set; }

        [JsonProperty("coldwhite", NullValueHandling = NullValueHandling.Ignore)]
        public long? Coldwhite { get; set; }

        [JsonProperty("compassdegrees", NullValueHandling = NullValueHandling.Ignore)]
        public long? Compassdegrees { get; set; }
    }

    public class Iface
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("deviceoptions", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceOptions DeviceOptions { get; set; }

        [JsonProperty("driver", NullValueHandling = NullValueHandling.Ignore)]
        public Driver Driver { get; set; }
    }

    public class DeviceOptions
    {
        [JsonProperty("device_uuid", NullValueHandling = NullValueHandling.Ignore)]
        public string DeviceUuid { get; set; }

        [JsonProperty("device_type", NullValueHandling = NullValueHandling.Ignore)]
        public string DeviceType { get; set; }

        [JsonProperty("power_uuid", NullValueHandling = NullValueHandling.Ignore)]
        public string PowerUuid { get; set; }

        [JsonProperty("power_bus", NullValueHandling = NullValueHandling.Ignore)]
        public long? PowerBus { get; set; }

        [JsonProperty("output_channel", NullValueHandling = NullValueHandling.Ignore)]
        public string OutputChannel { get; set; }

        [JsonProperty("inverted", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Inverted { get; set; }

        [JsonProperty("deviceid", NullValueHandling = NullValueHandling.Ignore)]
        public string Deviceid { get; set; }

        [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
        public string Model { get; set; }

        [JsonProperty("repeat", NullValueHandling = NullValueHandling.Ignore)]
        public long? Repeat { get; set; }

        [JsonProperty("offset", NullValueHandling = NullValueHandling.Ignore)]
        public long? Offset { get; set; }

        [JsonProperty("eep", NullValueHandling = NullValueHandling.Ignore)]
        public string Eep { get; set; }

        [JsonProperty("fileName", NullValueHandling = NullValueHandling.Ignore)]
        public string FileName { get; set; }
    }

    public class Driver
    {
    }

    public class Parameters
    {
        [JsonProperty("dimoptions")]
        public string Dimoptions { get; set; }

        [JsonProperty("dimrate")]
        public string Dimrate { get; set; }

        [JsonProperty("brightenrate")]
        public string Brightenrate { get; set; }

        [JsonProperty("resptoocc")]
        public string Resptoocc { get; set; }

        [JsonProperty("resptovac")]
        public string Resptovac { get; set; }

        [JsonProperty("resptodl50")]
        public string Resptodl50 { get; set; }

        [JsonProperty("resptodl40")]
        public string Resptodl40 { get; set; }

        [JsonProperty("resptodl30")]
        public string Resptodl30 { get; set; }

        [JsonProperty("resptodl20")]
        public string Resptodl20 { get; set; }

        [JsonProperty("resptodl10")]
        public string Resptodl10 { get; set; }

        [JsonProperty("resptodl0")]
        public string Resptodl0 { get; set; }

        [JsonProperty("manualceiling")]
        public string Manualceiling { get; set; }

        [JsonProperty("manualfloor")]
        public string Manualfloor { get; set; }

        [JsonProperty("dimtooff")]
        public string Dimtooff { get; set; }

        [JsonProperty("dlenable")]
        public bool Dlenable { get; set; }

        [JsonProperty("powerinfo")]
        public Powerinfo Powerinfo { get; set; }

        [JsonProperty("mfg")]
        public Mfg Mfg { get; set; }
    }

    public class Mfg
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class Powerinfo
    {
        [JsonProperty("baseline")]
        public string Baseline { get; set; }

        [JsonProperty("budget")]
        public string Budget { get; set; }

        [JsonProperty("measured")]
        public string Measured { get; set; }

        [JsonProperty("maxload")]
        public string Maxload { get; set; }
    }
}
