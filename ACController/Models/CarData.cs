using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACController.Models
{
    public class CarData
    {

        public string CarId { get; set; }

        public string ParentId { get; set; }

        public string CarName { get; set; }

        public string Brand { get; set; }

        public string CarDescription { get; set; }

        public string[] CarTags { get; set; }

        public string Class { get; set; }

        public CarSpecData CarSpecs { get; set; }

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

        public string SkinId { get; set; }
        
        public string SkinName { get; set; }

        public string DriverName { get; set; }

        public string Country { get; set; }

        public string Team { get; set; }

        public string Number { get; set; }

        public int Priority { get; set; }

        public override string ToString()
        {
            return $"{SkinId}: {SkinName}";
        }

    }
}
