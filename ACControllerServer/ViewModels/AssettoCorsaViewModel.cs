using ACControllerServer.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.ViewModels
{

    public class AssettoCorsaViewModel : ObservableObject
    {
        private readonly ILogger<AssettoCorsaViewModel> _logger;
        private readonly IAcDirectoryService _acContent;

        public AssettoCorsaViewModel(ILogger<AssettoCorsaViewModel> logger,
            IAcDirectoryService acContent)
        {
            _logger = logger;
            _acContent = acContent;
        }

        #region Properties

        #endregion

    }
}
