using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetThere.ViewModels
{
    public class StopsList
    {
        public StopsList()
        {
            Stops = new List<StopsData>();
        }
        public List<StopsData> Stops {get; set;}
    }
}
