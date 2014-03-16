using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using GetThere.Resources;
using System.Device.Location;
using HaversineFormula;
using System.Collections.Generic;
using System.Linq;

namespace GetThere.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            this.Items = new ObservableCollection<ItemViewModel>();
        }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<ItemViewModel> Items { get; private set; }

        private string _sampleProperty = "Sample Runtime Property Value";
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding
        /// </summary>
        /// <returns></returns>
        public string SampleProperty
        {
            get
            {
                return _sampleProperty;
            }
            set
            {
                if (value != _sampleProperty)
                {
                    _sampleProperty = value;
                    NotifyPropertyChanged("SampleProperty");
                }
            }
        }

        /// <summary>
        /// Sample property that returns a localized string
        /// </summary>
        public string LocalizedSampleProperty
        {
            get
            {
                return AppResources.SampleProperty;
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        /// 
        public void LoadData(List<Stop> stopslist, GeoCoordinate myLocation)
        {
            this.Items.Clear();
            int i = 0;
            foreach (Stop stop in stopslist)
            {
                if (i < 10)
                {
                    this.Items.Add(new ItemViewModel()
                    {
                        ID = i.ToString(),
                        LineOne = stop.stop_name,
                        LineTwo = new GeoCoordinate(stop.stop_lat, stop.stop_lon).ToString(),
                        LineThree = stop.stop_url,
                        LineFive = ""
                    });
                    i++;
                }
            }
            this.IsDataLoaded = true;

        }

        public double Distance(GeoCoordinate pos1, GeoCoordinate pos2, DistanceType type)
        {

            double R = (type == DistanceType.Miles) ? 3960 : 6371;
            double dLat = this.toRadian(pos2.Latitude - pos1.Latitude);
            double dLon = this.toRadian(pos2.Longitude - pos1.Longitude);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +

                Math.Cos(this.toRadian(pos1.Latitude)) * Math.Cos(this.toRadian(pos2.Latitude)) *

                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            double d = R * c;
            return d;

        }

        private double toRadian(double val)
        {

            return (Math.PI / 180) * val;

        }

  

        //public void LoadData()
        //{

        //    // Sample data; replace with real data
        //    //for each item in result, this.items.add(new ItemViewModel() etc as below.
        //    //this.Items.Add(new ItemViewModel()
        //    //{
        //    //    ID = "0",
        //    //    LineOne = "Queen Street Bus Station",
        //    //    LineTwo = "-27.4699223,153.0248401",
        //    //    LineThree = "http://translink.com.au/stop/010018",
        //    //    LineFive = "3 minutes walk"});
        //    //this.Items.Add(new ItemViewModel()
        //    //{
        //    //    ID = "1",
        //    //    LineOne = "King George Square station",
        //    //    LineTwo = "-27.4710107,153.0234489",
        //    //    LineThree = "http://translink.com.au/stop/010023",
        //    //    LineFive = "5 minutes walk"});
        //    //this.Items.Add(new ItemViewModel()
        //    //{ 
        //    //    ID = "2", 
        //    //    LineOne = "Central Station", 
        //    //    LineTwo = "-27.465918,153.025939", 
        //    //    LineThree = "http://translink.com.au/stop/010000",
        //    //    LineFive = "10 minutes walk"});
            
           
        //    this.IsDataLoaded = true;
            

        //}

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}