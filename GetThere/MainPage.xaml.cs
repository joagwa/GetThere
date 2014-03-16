using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using GetThere.Resources;
using GetThere.ViewModels;
using System.IO.IsolatedStorage;
using Windows.Devices.Geolocation;
using System.Device.Location;
using Microsoft.Phone.Maps.Services;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Phone.Maps.Controls;
using Windows.Networking.Proximity;
using System.Globalization;
using Windows.Storage;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.UI.Core;
using System.Xml.Serialization;
using Microsoft.Phone.Marketplace;


namespace GetThere
{
    public partial class MainPage : PhoneApplicationPage
    {
        //Enumerator references
        public enum DistanceType { Miles, Kilometers };

        //Variable declarations
        bool _isTrial;
        bool _isRouteSearch = false;//flag to determine if route search is already being conducted
        public bool _trackingOn = true;//flag to determine if location tracking is on
        string myLocationAddress;//devices current location as a civic address
        string locationState;//devices current state
        int StopsListCounter = 0;//counter used for Stops list
        SQLite.SQLiteAsyncConnection conn;//transport database connection object
        SQLite.SQLiteAsyncConnection fconn = new SQLite.SQLiteAsyncConnection(System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "favourites.db"));//favourites database connection object
        DateTime Evening = DateTime.Parse("17:00:00");
        DateTime Daytime = DateTime.Parse("7:00:00");

        //List object declarations
        List<Stop> ClosestStops = new List<Stop>();// list of stop objects

        //funcational object declarations
        MapRoute MyMapRoute = null;//Map route object used to show walking directions
        ReverseGeocodeQuery MyReverseGeocodeQueryOrigin = new ReverseGeocodeQuery();//reverse geocode query object used to determine address based on GeoCoordinates
        GeocodeQuery MyGeocodeQuery = new GeocodeQuery();//Geocode query object to determine geocoordinates based on a civic address
        GeoCoordinateWatcher myGeolocator = new GeoCoordinateWatcher(desiredAccuracy: GeoPositionAccuracy.High);//Geolocator instance for location tracking
        GeoCoordinate myLocation = new GeoCoordinate();//Specified current location
        LicenseInformation licence = new LicenseInformation();

        public MainPage()
        {
            InitializeComponent();
            ApplicationBar = new ApplicationBar();

            ApplicationBarIconButton resetLocationButton = new ApplicationBarIconButton();
            resetLocationButton.IconUri = new Uri("/Assets/Images/appbar.location.circle.png", UriKind.RelativeOrAbsolute);
            resetLocationButton.Text = "Reset";
            resetLocationButton.Click += resetLocationButton_Click;
            ApplicationBar.Buttons.Add(resetLocationButton);

            ApplicationBarIconButton searchButton = new ApplicationBarIconButton();
            searchButton.IconUri = new Uri("/Assets/Images/appbar.magnify.png", UriKind.RelativeOrAbsolute);
            searchButton.Text = "Search";
            searchButton.Click += searchButton_Click;
            ApplicationBar.Buttons.Add(searchButton);

            ApplicationBarIconButton TransportMethodButton = new ApplicationBarIconButton();
            TransportMethodButton.IconUri = new Uri("/Assets/Images/appbar.transit.bus.png", UriKind.RelativeOrAbsolute);
            TransportMethodButton.Text = "Bus";
            TransportMethodButton.Click += TransportMethodButton_Click;
            ApplicationBar.Buttons.Add(TransportMethodButton);
            DataContext = App.ViewModel;
            myGeolocator.MovementThreshold = 100;
            //CreateFavouritesDatabase();

            ApplicationBarMenuItem LocationServices = new ApplicationBarMenuItem();
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                GetLocationConsent();
            }

            if (IsolatedStorageSettings.ApplicationSettings["LocationConsent"].Equals(true))
            {
                LocationServices.Text = "Location Services On";
                _trackingOn = true;
            }
            else
            {
                LocationServices.Text = "Location Services Off";
                _trackingOn = false;

            }
            LocationServices.Click += LocationServices_Click;
            ApplicationBar.MenuItems.Add(LocationServices);
        }

        void LocationServices_Click(object sender, EventArgs e)
        {
            ApplicationBarMenuItem item = (ApplicationBarMenuItem)ApplicationBar.MenuItems[0];
            if (IsolatedStorageSettings.ApplicationSettings["LocationConsent"].Equals(true))
            {
                item.Text = "Location Services Off";
                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
                _trackingOn = false;
            }
            else
            {
                item.Text = "Location Services On";
                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
                MessageBoxResult LocationServicesAlert =
                  MessageBox.Show("This app accesses your phone's location, but does not store it in any way."
                + "You have enabled Location Services. Press this button again to disable",
                  "Location",
                  MessageBoxButton.OK);
                _trackingOn = true;
            }
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData(ClosestStops, myLocation);
            }
            _isTrial = licence.IsTrial();
            if (!_isTrial)
            {
                adBar.Visibility = Visibility.Collapsed;
            }
            var x = Evening.CompareTo(DateTime.Now);
            var y = Daytime.CompareTo(DateTime.Now);

            if ((Evening.CompareTo(DateTime.Now) < 0) || (Daytime.CompareTo(DateTime.Now) > 0))
            {
                stopsMap.ColorMode = MapColorMode.Dark;
            }
            else
            {
                stopsMap.ColorMode = MapColorMode.Light;
            }
            CheckForLocationConsent();
            getLocation();
            stopsMap.Center = myLocation;
        }

        //////To come, favourites list, shows list of favourited stops for easy access. Walking distance and link to translink///
        //private async void RetrieveFavourites()  
        //{
        //    var results = await fconn.QueryAsync<Stop>("Select * from Stop");
        //    foreach (Stop item in results)
        //    {
        //        FavouritesList.Items.Add(item.stop_name);
        //    }
        //}
        private async void CreateFavouritesDatabase()
        {
            // Here we check whether the database alredy exists
            // It's not required, but added just to show an example of IsolatedStorage
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!storage.FileExists("favourites.db"))
                {
                    await fconn.CreateTableAsync<Stop>();
                }
            }
        }
        private static void CheckForLocationConsent()
        //Checks if user has opted in/out of location tracking. Requests consent if no preference
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                //User has opted in or out of Locations. If user has opted out of location tracking, ask again
                if (IsolatedStorageSettings.ApplicationSettings["LocationConsent"].Equals(false))
                {
                    GetLocationConsent();
                }
            }
            else
            {
                GetLocationConsent();
            }
        }
        public static void GetLocationConsent()
        //Prompts user for consent to location tracking
        {
            MessageBoxResult result =
                MessageBox.Show("This app accesses your phone's location. Do you consent to allowing the app to track your location?",
                "Location",
                MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
                //_trackingon = true;
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
            }

            IsolatedStorageSettings.ApplicationSettings.Save();
        }
        private void getLocation()
        //Check for location Consent and start tracking location
        {
            if ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] != true)
            {
                //The user has opted out of location
                CheckForLocationConsent();
            }
            try
            {
                progressBar.Visibility = Visibility.Visible;
                myGeolocator.Start();
                if (_trackingOn)
                {
                    myLocation = myGeolocator.Position.Location;
                }
                myGeolocator.PositionChanged -= myGeolocator_PositionChanged;
                myGeolocator.PositionChanged += myGeolocator_PositionChanged;
                stopsMap.Layers.Clear();
                if (myLocation.IsUnknown)
                {
                    return;
                }
                markUserLocation(myLocation);
                GetLocationAddress(myLocation.Latitude, myLocation.Longitude);

            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    // the application does not have permission/ability for location
                    //StatusTextBlock.text = "location is disabled in phone settings.";
                }
            }
        }
        private SQLite.SQLiteAsyncConnection CreateDatabaseConnection()
        {
            string databaselocation = "";
            switch (locationState)
            {
                case "Queensland":
                    databaselocation = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "brisbanetranslink.db");
                    break;
                case "South Australia":
                    databaselocation = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "adelaidemetro.db");
                    break;
                default:
                    databaselocation = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "brisbanetranslink.db");
                    break;

            }
            return new SQLite.SQLiteAsyncConnection(databaselocation);
        }
        private string createStopSQL()
        //Creates custom SQL based on Transport Method selection
        {
            string transportMethod = "";
            ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
            switch (btn.Text)
            {
                case "Train":
                    transportMethod = "and (length(stop_id) = 6 and substr(stop_id,1,1) = '6')";
                    break;
                case "Ferry":
                    transportMethod = "and (stop_id between 317571 and 317594)";
                    break;
                case "Bus":
                    transportMethod = "and not(stop_id between 317571 and 317594) and not (length(stop_id) = 6 and substr(stop_id,1,1) = '6')";
                    break;
            }

            string sql = string.Format("select * from stops where (stop_lat between {0} and {1}) and (stop_lon between {2} and {3}){4}",
                myLocation.Latitude - 0.05, myLocation.Latitude + 0.05, myLocation.Longitude - 0.05, myLocation.Longitude + 0.05, transportMethod);
            return sql;

        }
        private void getClosestStop(SQLite.SQLiteAsyncConnection conn)
        //Executes created SQL to determine closest public transport stops to the user
        {
            if (conn == null)
            {
                return;
            }
            string sql = createStopSQL();
            ClosestStops = conn.QueryAsync<Stop>(sql).Result;
            foreach (Stop stop in ClosestStops)
            {
                stop.distance = Distance(myLocation, new GeoCoordinate(stop.stop_lat, stop.stop_lon), DistanceType.Kilometers);
            }
            ClosestStops = ClosestStops.OrderBy(x => x.distance).ToList();
            MarkStopsLocations(ClosestStops);
            App.ViewModel.LoadData(ClosestStops, myLocation);
            if (MainLongListSelector.ItemsSource.Count > 0)
            {
                walkingRoute(myLocation, ParseGeoCoordinate(((GetThere.ViewModels.ItemViewModel)(MainLongListSelector.ItemsSource[0])).LineTwo.ToString()));

            }
        }
        private string getWalkingTime(Route WalkingRoute)
        //returns estimated walking time from selected route
        {
            return WalkingRoute.EstimatedDuration.ToString();
        }
        private void myGeolocator_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            progressBar.Visibility = Visibility.Collapsed;
            if (_trackingOn)
            {
                string lat = myLocation.Latitude.ToString();
                string lon = myLocation.Longitude.ToString();
                if (lat.Length >= 8 && lon.Length >= 8)
                {
                    lon = lon.Remove(8);
                    lat = lat.Remove(8);
                }

                string lon2 = myGeolocator.Position.Location.Longitude.ToString();
                string lat2 = myGeolocator.Position.Location.Latitude.ToString();
                if (lat2.Length >= 8 && lon2.Length >= 8)
                {
                    lon2 = lon2.Remove(8);
                    lat2 = lat2.Remove(8);
                }

                if (lon == lon2 && lat == lat2)
                {
                    return;
                }
                myLocation = myGeolocator.Position.Location;
                GetLocationAddress(myLocation.Latitude, myLocation.Longitude);
                stopsMap.Center = myLocation;
                markUserLocation(myLocation);
                conn = CreateDatabaseConnection();
                getClosestStop(conn);
            }
        }
        private void GetLocationAddress(double latitude, double longitude)
        //Returns the users current location as a civic address
        {
            if (MyReverseGeocodeQueryOrigin == null || !MyReverseGeocodeQueryOrigin.IsBusy)
            {
                progressBar.Visibility = Visibility.Visible;
                MyReverseGeocodeQueryOrigin = new ReverseGeocodeQuery();
                MyReverseGeocodeQueryOrigin.GeoCoordinate = new GeoCoordinate(latitude, longitude);
                MyReverseGeocodeQueryOrigin.QueryCompleted += CurrentLocationReverseGeocodeQuery_QC;
                MyReverseGeocodeQueryOrigin.QueryAsync();

            }
        }

        private void MarkStopsLocationsNew(List<Stop> ClosestStops)
        {
         //int maxStops = 100;

            foreach (Stop stop in ClosestStops)
            {
                switch (stop.stop_type)
                {
                    default:
                        break;
                    case "bus":
                        break;
                    case "train":
                        break;
                    case "ferry":
                        break;
                    case "tram":
                        break;
                }
            }
        }
        private void MarkStopsLocations(List<Stop> ClosestStops)
        //Marks closest 10 stops in ClosestStops list on the map
        {
            int i = 0;
            foreach (Stop stop in ClosestStops)
            {
                if (i < 10)
                {
                    markLocation(new GeoCoordinate(stop.stop_lat, stop.stop_lon));
                    i++;
                }
            }
        }
        private void SearchForAddress()
        //Searches for address specified and returns GeoCoordinate
        {
            progressBar.Visibility = System.Windows.Visibility.Visible;
            if (!_isRouteSearch)
            {
                SearchButton.Visibility = System.Windows.Visibility.Collapsed;
                SearchTextBox.Visibility = System.Windows.Visibility.Collapsed;
                MyGeocodeQuery.SearchTerm = SearchTextBox.Text;
                MyGeocodeQuery.GeoCoordinate = myLocation == null ? new GeoCoordinate(0, 0) : myLocation;
                MyGeocodeQuery.QueryCompleted += GeocodeQuery_QueryCompleted;
                MyGeocodeQuery.QueryAsync();
                _isRouteSearch = true;
            }
        }
        private void GeocodeQuery_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            progressBar.Visibility = System.Windows.Visibility.Collapsed;
            if (e.Result.Count == 0)
            {
                MessageBox.Show("Your specified addres was not located. Please try again");
                _isRouteSearch = false;
            }
            else
            {
                _trackingOn = false;
                _isRouteSearch = false;
                myLocation = ParseGeoCoordinate(e.Result.First().GeoCoordinate.ToString());
                stopsMap.Center = myLocation;
                markUserLocation(myLocation);
                getClosestStop(conn);

            }
        }
        private void CurrentLocationReverseGeocodeQuery_QC(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            progressBar.Visibility = Visibility.Collapsed;
            if (e.Error == null)
            {
                if (e.Result.Count > 0)
                {
                    MapAddress address = e.Result[0].Information.Address;
                    myLocationAddress = createAddressString(address);
                    CurrentLocationText.Text = "You are currently at " + myLocationAddress;
                    locationState = address.State.ToString();
                    conn = CreateDatabaseConnection();
                    getClosestStop(conn);
                }

            }
        }
        private string createAddressString(MapAddress address)
        //helper to create address string
        {
            string addressString = "";
            if (address.HouseNumber != "")
            {
                addressString = address.HouseNumber + " ";
            }
            if (address.Street != "")
            {
                addressString += address.Street;
                addressString += ", ";
            }
            if (address.District != "")
            {
                addressString += address.District;
            }
            return addressString;
        }
        public double Distance(GeoCoordinate pos1, GeoCoordinate pos2, DistanceType type)
        //determines gross distance from user to specified location
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
        //converts double to radian
        {
            return (Math.PI / 180) * val;
        }
        private void markLocation(GeoCoordinate location)
        //marks location of item on map
        {
            // Create a small circle to mark the stop location.
            Ellipse myCircle = new Ellipse();
            myCircle.Fill = new SolidColorBrush((Color)Application.Current.Resources["PhoneAccentColor"]);
            myCircle.Height = 10;
            myCircle.Width = 10;
            myCircle.Opacity = 20;

            // Create a MapOverlay to contain the circle.
            MapOverlay stopLocationOverlay = new MapOverlay();
            stopLocationOverlay.Content = myCircle;
            stopLocationOverlay.PositionOrigin = new System.Windows.Point(0.5, 0.5);
            stopLocationOverlay.GeoCoordinate = location;


            // Create a MapLayer to contain the MapOverlay.
            MapLayer stopLocationLayer = new MapLayer();
            stopsMap.Layers.Remove(stopLocationLayer);
            stopLocationLayer.Add(stopLocationOverlay);
            stopsMap.Layers.Add(stopLocationLayer);

        }
        private void markUserLocation(GeoCoordinate location)
        //Creates an icon and marks the map with it to show user location
        {
            //creates a triangle to mark location
            System.Windows.Shapes.Polygon myLocationShape = new Polygon();
            myLocationShape.Points.Add(new System.Windows.Point(0, 0));
            myLocationShape.Points.Add(new System.Windows.Point(-10, 0));
            myLocationShape.Points.Add(new System.Windows.Point(0, 20));
            myLocationShape.Points.Add(new System.Windows.Point(10, 0));
            myLocationShape.Fill = new SolidColorBrush((Color)Application.Current.Resources["PhoneAccentColor"]);
            myLocationShape.Opacity = 50;

            //creates a transparant circle to surround user's location
            Ellipse myCircle = new Ellipse();
            myCircle.Fill = new SolidColorBrush((Color)Application.Current.Resources["PhoneAccentColor"]);
            myCircle.Height = 100;
            myCircle.Width = 100;
            myCircle.Opacity = 0.2;

            // Create a MapOverlay to contain the circle.
            MapOverlay myLocationOverlay = new MapOverlay();
            myLocationOverlay.Content = myLocationShape;
            myLocationOverlay.PositionOrigin = new System.Windows.Point(0, 0.5);
            myLocationOverlay.GeoCoordinate = location;

            MapOverlay myLocationOverlay2 = new MapOverlay();
            myLocationOverlay2.Content = myCircle;
            myLocationOverlay2.PositionOrigin = new System.Windows.Point(0.5, 0.5);
            myLocationOverlay2.GeoCoordinate = location;


            // Create a MapLayer to contain the MapOverlay.
            MapLayer myLocationLayer = new MapLayer();
            stopsMap.Layers.Clear();
            myLocationLayer.Add(myLocationOverlay);
            myLocationLayer.Add(myLocationOverlay2);
            stopsMap.Layers.Add(myLocationLayer);
        }
        public GeoCoordinate ParseGeoCoordinate(string input)
        //Method to convert a string to a GeoCoordinate. Sourced from the internet
        {
            if (String.IsNullOrEmpty(input))
            {
                throw new ArgumentException("input");
            }

            if (input == "Unknown")
            {
                return GeoCoordinate.Unknown;
            }
            string[] parts = input.Split(',');

            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid format");
            }

            double latitude = Double.Parse(parts[0], CultureInfo.InvariantCulture);
            double longitude = Double.Parse(parts[1], CultureInfo.InvariantCulture);

            return new GeoCoordinate(latitude, longitude);
        }
        void walkingRoute(GeoCoordinate currentLocation, GeoCoordinate stopLocation)
        //Get walking route from selected location to selected stop
        {
            if (MyMapRoute != null)
            {

                stopsMap.RemoveRoute(MyMapRoute);
            }
            MarkStopsLocations(ClosestStops);
            List<GeoCoordinate> MyCoordinates = new List<GeoCoordinate>();
            MyCoordinates.Add(currentLocation);
            MyCoordinates.Add(stopLocation);
            RouteQuery WalkingRouteQuery = new RouteQuery();
            WalkingRouteQuery.Waypoints = MyCoordinates;
            WalkingRouteQuery.TravelMode = TravelMode.Walking;
            progressBar.Visibility = Visibility.Visible;
            progressBar.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            WalkingRouteQuery.QueryCompleted += WalkingRouteQuery_QueryCompleted;
            WalkingRouteQuery.QueryAsync();
        }
        void WalkingRouteQuery_QueryCompleted(object sender, QueryCompletedEventArgs<Route> e)
        {
            progressBar.Visibility = Visibility.Collapsed;
            if (e.Result.Legs.Count() != 0)
            {
                Route MyRoute = e.Result;
                MyMapRoute = new MapRoute(MyRoute);
                MyMapRoute.Color = new SolidColorBrush((Color)Application.Current.Resources["PhoneAccentColor"]).Color;
                stopsMap.AddRoute(MyMapRoute);
                if (((ItemViewModel)(MainLongListSelector.SelectedItem)) == null)
                {
                    ((ItemViewModel)(MainLongListSelector.ItemsSource[0])).LineFive = Convert.ToInt16(e.Result.EstimatedDuration.TotalMinutes).ToString() + " minutes walk";

                }
                else
                {
                    ((ItemViewModel)(MainLongListSelector.SelectedItem)).LineFive = Convert.ToInt16(e.Result.EstimatedDuration.TotalMinutes).ToString() + " minutes walk";

                }

            }
            else
            {
                MessageBox.Show("No walking routes available for chosen location. Please choose again");
            }
        }

        //Having issues with HereLaunchers - The type '<class>' exists in both '<dll location>' and '<dll location 2>' - removed feature, will try and redo later
        //private void LaunchNavigationApp(ItemViewModel selectedStop)
        ////Launches into default navigation app for walking directions
        //{
        //    if (!selectedStop.LineTwo.Equals(null))
        //    {
        //        GuidanceWalkTask walkto = new GuidanceWalkTask();
        //        walkto.Destination = ParseGeoCoordinate(selectedStop.LineTwo);
        //        walkto.Title = selectedStop.LineOne;
        //        walkto.Show();
        //    }
        //}

        private void MainLongListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (MyMapRoute != null)
            {
                stopsMap.RemoveRoute(MyMapRoute);
            }
            StopsListCounter = 0;
        }

        private void MainLongListSelector_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //on first tap, show route to selected stop. On second tap open timetable for stop.
        {
            if (MainLongListSelector.SelectedItem == null)
                return;
            if (StopsListCounter == 1)
            {

                string stopUrl = ((ItemViewModel)(MainLongListSelector.SelectedItem)).LineThree.ToString();
                NavigationService.Navigate(new Uri("/DetailsPage.xaml?stopUrl=" + stopUrl.ToString() +
                    "&walkTime=" + ((ItemViewModel)(MainLongListSelector.SelectedItem)).LineFive.ToString() +
                "&stopName=" + ((ItemViewModel)(MainLongListSelector.SelectedItem)).LineOne.ToString(), UriKind.RelativeOrAbsolute));
                StopsListCounter = 0;

            }
            else
            {
                walkingRoute(myLocation, ParseGeoCoordinate(((ItemViewModel)(MainLongListSelector.SelectedItem)).LineTwo.ToString()));
                StopsListCounter++;
            }
        }
        private void stopsMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //make loction centre of map on tap
        {
            if (MyMapRoute != null)
            {
                stopsMap.RemoveRoute(MyMapRoute);
            }

            _trackingOn = false;
            stopsMap.Layers.Clear();
            if (MyMapRoute != null)
            {
                stopsMap.RemoveRoute(MyMapRoute);
            }
            myLocation = stopsMap.Center;
            markUserLocation(myLocation);
            getClosestStop(conn);
        }
        private void stopsMap_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        //Reset loction to devices location
        {
            _trackingOn = true;
            getLocation();
            stopsMap.Center = myLocation;
            stopsMap.ZoomLevel = 16.5;
            stopsMap.Layers.Clear();
            markUserLocation(myLocation);
            getClosestStop(conn);
        }
        void searchButton_Click(object sender, EventArgs e)
        //Searches for a location to centre the map
        {
            SearchTextBox.Visibility = System.Windows.Visibility.Visible;
            SearchButton.Visibility = System.Windows.Visibility.Visible;
            SearchTextBox.Focus();
        }
        private void Search_Button_Click(object sender, RoutedEventArgs e)
        //allows user to search for location
        {
            SearchForAddress();
        }
        void resetLocationButton_Click(object sender, EventArgs e)
        //reverts to location tracking
        {
            _trackingOn = true;
            if (MyMapRoute != null)
            {
                stopsMap.RemoveRoute(MyMapRoute);

            }
            getLocation();
            stopsMap.Center = myLocation;
            stopsMap.ZoomLevel = 16.5;
            stopsMap.Layers.Clear();
            markUserLocation(myLocation);
            getClosestStop(conn);
        }
        void TransportMethodButton_Click(object sender, EventArgs e)
        //Changes appbar icon based on travel method
        {
            stopsMap.Layers.Clear();
            try
            {
                stopsMap.RemoveRoute(MyMapRoute);
            }
            catch
            {

            }
            ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
            switch (btn.Text)
            {
                case "Train":
                    btn.Text = "Ferry";
                    btn.IconUri = new Uri("/Assets/Images/appbar.ferry.png", UriKind.RelativeOrAbsolute);
                    break;
                case "Ferry":
                    btn.Text = "Bus";
                    btn.IconUri = new Uri("/Assets/Images/appbar.transit.bus.png", UriKind.RelativeOrAbsolute);
                    break;
                case "Bus":
                    btn.Text = "Train";
                    btn.IconUri = new Uri("/Assets/Images/appbar.train.png", UriKind.RelativeOrAbsolute);
                    break;
            }
            getLocation();
            getClosestStop(conn);
        }
        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        //when leaving text box, collapse
        {
            SearchButton.Visibility = System.Windows.Visibility.Collapsed;
            SearchTextBox.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void FavouriteButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Coming Soon!!!");
        }

        private void NavigateToButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Coming Soon!!!");
            //var a = sender as MenuItem;///uncommentwhen submitting with navigation token
            //ItemViewModel selectedStop = (ItemViewModel)a.DataContext;
            //LaunchNavigationApp(selectedStop);
        }
    }
}


