using Newtonsoft.Json;

namespace ACControllerServer.Models
{

    public class CarData
    {

        [JsonIgnore]
        public string CarId { get; set; }

        [JsonIgnore, System.Text.Json.Serialization.JsonIgnore] 
        public string CarDirectory { get; set; }

        [JsonProperty("parent")]
        public string ParentId { get; set; }

        [JsonProperty("name")]
        public string CarName { get; set; }

        [JsonProperty("brand")]
        public string Brand { get; set; }

        [JsonProperty("description")]
        public string CarDescription { get; set; }

        [JsonProperty("tags")]
        public string[] CarTags { get; set; }

        [JsonProperty("class")]
        public string Class { get; set; }

        [JsonProperty("specs")]
        public CarSpecData CarSpecs { get; set; }

        [JsonIgnore]
        public List<CarSkinData> Skins { get; set; }

        public override string ToString()
        {
            return $"{CarId}: {CarName}";
        }
    }

    public class CarSpecData
    {
        public string bhp { get; set; }
        public string torque { get; set; }
        public string weight { get; set; }
        public string topspeed { get; set; }
        public string acceleration { get; set; }
        public string pwratio { get; set; }
        public int range { get; set; }

        public override string ToString()
        {
            return $"{bhp}, {torque}, {weight}, {topspeed}, {acceleration}, {pwratio}, {range}";
        }
    }

    public class CarSkinData
    {

        [JsonIgnore]
        public string SkinId { get; set; } 
        [JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
        public string SkinDirectory { get; set; }
        [JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
        public string SkinPreviewImage { get; set; }

        [JsonProperty("skinname")]
        public string SkinName { get; set; }

        [JsonProperty("drivername")]
        public string DriverName { get; set; } 

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("team")]
        public string Team { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }

        public override string ToString()
        {
            return $"{SkinId}: {SkinName}";
        }
    }

}
