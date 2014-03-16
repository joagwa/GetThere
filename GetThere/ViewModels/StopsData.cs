using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Location;

namespace GetThere.ViewModels
{
    public class StopsData
    {
        public int ID { get; set; }
        public GeoCoordinate location { get; set; }
        public string stopName { get; set; }
        public string stopURL { get; set; }
        public string walkingTime { get; set; }

    }
}
