using System;
using System.Diagnostics;
using System.Resources;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using GetThere.Resources;
using GetThere.ViewModels;
using System.IO.IsolatedStorage;
using System.Windows.Resources;
using System.IO;
using System.Device.Location;
using Microsoft.Phone.Marketplace;

namespace GetThere
{
    public partial class App : Application
    {
        private static LicenseInformation _licenseInfo = new LicenseInformation();
        private static bool _isTrial = true;
        public bool IsTrial
        {
            get
            {
                return _isTrial;
            }
        }

        /// <summary>
        /// Check the current license information for this application
        /// </summary>
        private void CheckLicense()
        {
            // When debugging, we want to simulate a trial mode experience. The following conditional allows us to set the _isTrial 
            // property to simulate trial mode being on or off. 
#if DEBUG
            string message = "This sample demonstrates the implementation of a trial mode in an application." +
                               "Press 'OK' to simulate trial mode. Press 'Cancel' to run the application in normal mode.";
            if (MessageBox.Show(message, "Debug Trial",
                 MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                _isTrial = true;
            }
            else
            {
                _isTrial = false;
            }
#else
            _isTrial = _licenseInfo.IsTrial();
#endif
        }

        private static MainViewModel viewModel = null;
        GeoCoordinateWatcher myGeolocator = new GeoCoordinateWatcher(desiredAccuracy: GeoPositionAccuracy.High);
        GeoCoordinate myLocation = new GeoCoordinate();
        //private static void CheckForLocationConsent()
        //{
        //    if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
        //    {
        //        //User has opted in or out of Locations
        //        //return;
        //    }
        //    else
        //    {
        //        MessageBoxResult result =
        //            MessageBox.Show("This app accesses your phone's location. If you say no, why did you download the app?",
        //            "Location",
        //            MessageBoxButton.OKCancel);

        //        if (result == MessageBoxResult.OK)
        //        {
        //            IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
        //        }
        //        else
        //        {
        //            IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
        //        }

        //        IsolatedStorageSettings.ApplicationSettings.Save();
        //    }
        //}

        //private void getLocation()
        //{
        //    if ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] != true)
        //    {
        //        //The user has opted out of location
        //        CheckForLocationConsent();
        //    }
        //    try
        //    {
        //        myGeolocator.Start();
        //        myLocation = myGeolocator.Position.Location;
        //        //Remove old layer

        //    }
        //    catch (Exception ex)
        //    {
        //        if ((uint)ex.HResult == 0x80004004)
        //        {
        //            // the application does not have permission/ability for location
        //            //StatusTextBlock.text = "location is disabled in phone settings.";
        //        }
        //    }
        //}

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static MainViewModel ViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (viewModel == null)
                    viewModel = new MainViewModel();

                return viewModel;
            }
        }
        //private static StopsModel viewModel = null;
        //public static StopsModel ViewModel
        //{
        //    get
        //    {
        //        if (viewModel == null)
        //            viewModel = new StopsModel();
        //        return viewModel;
        //    }
        //}
        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions.
         //   UnhandledException += Application_UnhandledException;

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Language display initialization
            InitializeLanguage();

            // Show graphics profiling information while debugging.
            if (Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode,
                // which shows areas of a page that are handed off GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Prevent the screen from turning off while under the debugger by disabling
                // the application's idle detection.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication();

            if (!isf.FileExists("brisbanetranslink.db"))
            {
                StreamResourceInfo sri = App.GetResourceStream(new Uri(@"/GetThere;component/Resources/brisbanetranslink.db", UriKind.Relative));

                IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("brisbanetranslink.db", FileMode.Create, IsolatedStorageFile.GetUserStoreForApplication());
                if (!sri.Equals(null)) { 
                long FileLength = (long)sri.Stream.Length;
                byte[] byteInput = new byte[FileLength];
                sri.Stream.Read(byteInput, 0, byteInput.Length);
                isfs.Write(byteInput, 0, byteInput.Length);
                sri.Stream.Close();
                isfs.Close();
                }
            }
            if (!isf.FileExists("adelaidemetro.db"))
            {
                StreamResourceInfo sri = App.GetResourceStream(new Uri(@"/GetThere;component/Resources/adelaidemetro.db", UriKind.Relative));

                IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("adelaidemetro.db", FileMode.Create, IsolatedStorageFile.GetUserStoreForApplication());
                if (!sri.Equals(null))
                {
                    long FileLength = (long)sri.Stream.Length;
                    byte[] byteInput = new byte[FileLength];
                    sri.Stream.Read(byteInput, 0, byteInput.Length);
                    isfs.Write(byteInput, 0, byteInput.Length);

                    sri.Stream.Close();
                    isfs.Close();
                }
            } 

        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            // Ensure that application state is restored appropriately
            if (!App.ViewModel.IsDataLoaded)
            {
                //App.ViewModel.LoadData(stopslist, myLocation);
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            // Ensure that required application state is persisted here.
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException6(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // If the app has received a 'reset' navigation, then we need to check
            // on the next navigation to see if the page stack should be reset
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Unregister the event so it doesn't get called again
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Only clear the stack for 'new' (forward) and 'refresh' navigations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // For UI consistency, clear the entire page stack
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
        }

        #endregion

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }
    }
}