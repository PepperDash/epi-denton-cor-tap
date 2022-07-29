using System;
using Newtonsoft.Json;

namespace PoeTexasCorTap
{
    public class LightingGatewayState
    {
        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("displayname")]
        public string Displayname { get; set; }

        [JsonProperty("room")]
        public string Room { get; set; }

        [JsonProperty("iface")]
        public Iface Iface { get; set; }

        [JsonProperty("outputid")]
        public long Outputid { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("candledim")]
        public bool Candledim { get; set; }

        [JsonProperty("twelvevolt")]
        public bool Twelvevolt { get; set; }

        [JsonProperty("parameters")]
        public Parameters Parameters { get; set; }

        [JsonProperty("level")]
        public long Level { get; set; }

        [JsonProperty("lastupdated")]
        public DateTime Lastupdated { get; set; }

        [JsonProperty("powerwatts")]
        public long Powerwatts { get; set; }

        [JsonProperty("daylightlimited")]
        public bool Daylightlimited { get; set; }

        [JsonProperty("occupiedstate")]
        public bool Occupiedstate { get; set; }

        [JsonProperty("dmxaddr")]
        public long Dmxaddr { get; set; }
    }

    public class Iface
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("deviceoptions")]
        public Deviceoptions Deviceoptions { get; set; }

        [JsonProperty("driver")]
        public Driver Driver { get; set; }
    }

    public class Deviceoptions
    {
        [JsonProperty("device_uuid")]
        public string DeviceUuid { get; set; }

        [JsonProperty("device_type")]
        public string DeviceType { get; set; }

        [JsonProperty("output_start")]
        public long OutputStart { get; set; }

        [JsonProperty("power_uuid")]
        public string PowerUuid { get; set; }

        [JsonProperty("power")]
        public long Power { get; set; }
    }

    public class Driver
    {
    }

    public class Parameters
    {
        [JsonProperty("dimoptions")]
        public long Dimoptions { get; set; }

        [JsonProperty("dimrate")]
        public long Dimrate { get; set; }

        [JsonProperty("brightenrate")]
        public long Brightenrate { get; set; }

        [JsonProperty("resptoocc")]
        public long Resptoocc { get; set; }

        [JsonProperty("resptovac")]
        public long Resptovac { get; set; }

        [JsonProperty("resptodl50")]
        public long Resptodl50 { get; set; }

        [JsonProperty("resptodl40")]
        public long Resptodl40 { get; set; }

        [JsonProperty("resptodl30")]
        public long Resptodl30 { get; set; }

        [JsonProperty("resptodl20")]
        public long Resptodl20 { get; set; }

        [JsonProperty("resptodl10")]
        public long Resptodl10 { get; set; }

        [JsonProperty("resptodl0")]
        public long Resptodl0 { get; set; }

        [JsonProperty("manualceiling")]
        public long Manualceiling { get; set; }

        [JsonProperty("manualfloor")]
        public long Manualfloor { get; set; }

        [JsonProperty("dimtooff")]
        public long Dimtooff { get; set; }

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
        public long Baseline { get; set; }

        [JsonProperty("budget")]
        public long Budget { get; set; }

        [JsonProperty("maxload")]
        public long Maxload { get; set; }

        [JsonProperty("measured")]
        public string Measured { get; set; }
    }
}
