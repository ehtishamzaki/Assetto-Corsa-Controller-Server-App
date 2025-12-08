using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Models
{
    public class ACServerConfig
    {

        #region Constant

        public const string Section = "ACControllerServer";

        #endregion

        #region Properties

        public string CarsDirectory { get; set; }

        public string TracksDirectory { get; set; }

        #endregion

    }
}
