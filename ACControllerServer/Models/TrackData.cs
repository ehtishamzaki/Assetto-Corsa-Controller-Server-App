using Newtonsoft.Json;
using System.Collections.Generic;

namespace ACControllerServer.Models
{

    public class TrackData
    {

        public string TrackId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string TrackDirectory { get; set; }

        public string TrackName { get; set; }

        public bool IsMultipleLayouts => Layouts.Count > 1;

        public List<TrackLayout> Layouts { get; set; }

        public override string ToString()
        {
            return $"{TrackId}: {TrackName}";
        }
    }

    public class TrackLayout
    {

        [JsonIgnore]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
        public string LayoutDirectory { get; set; }

        [JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
        public string ThumbnailImage { get; set; }

        [JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
        public string OutlineMapImage { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("tags")]
        public string[] Tags { get; set; }

        [JsonProperty("geotags")]
        public string[] GeoTags { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("length")]
        public string Length { get; set; }

        [JsonProperty("width")]
        public string Width { get; set; }

        [JsonProperty("pitboxes")]
        public string PitBoxes { get; set; }

        [JsonProperty("run")]
        public string Run { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

}
