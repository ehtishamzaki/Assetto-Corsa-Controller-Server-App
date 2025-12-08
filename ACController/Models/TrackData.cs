using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACController.Models
{
    public class TrackData
    {

        public string TrackId { get; set; }

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

        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string[] Tags { get; set; }

        public string[] GeoTags { get; set; }

        public string Country { get; set; }

        public string City { get; set; }

        public string Length { get; set; }

        public string Width { get; set; }

        public string PitBoxes { get; set; }

        public string Run { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
