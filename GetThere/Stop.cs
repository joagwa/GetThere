using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stopsNearMe
{
    public class Stop
    {
        public int stop_id { get; set; }
        public string location_type { get; set; }
        public string parent_station { get; set; }
        public string stop_code { get; set; }
        public string stop_name { get; set; }
        public string stop_desc { get; set; }
        public double stop_lon { get; set; }
        public double stop_lat { get; set; }
        public string stop_url { get; set; }
        public string stop_type { get; set; }
        public double distance { get; set; }

    }
}
