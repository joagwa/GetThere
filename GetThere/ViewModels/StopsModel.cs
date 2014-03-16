using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetThere.ViewModels
{
    class StopsModel
    {
        public StopsList stopsList { get; set; }

        public bool IsDataLoaded { get; set; }
        public void LoadData()
        {
            // Load data into the model
            IsDataLoaded = true;
        }
    }
}
