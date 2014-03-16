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
using Microsoft.Phone.Tasks;

namespace GetThere
{
    public partial class DetailsPage : PhoneApplicationPage
    {
        // Constructor
        string selectedStop = "";

        public DetailsPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (DataContext == null)
            {
                string walkingTime = "";
                string stopName = "";
                if (NavigationContext.QueryString.TryGetValue("stopUrl", out selectedStop))
                {
                    //int index = int.Parse(selectedIndex);
                    //DataContext = App.ViewModel.Items[index];
                    MiniBrowser.Source = new Uri(selectedStop);
                }
                if (NavigationContext.QueryString.TryGetValue("walkTime", out walkingTime))
                {
                    WalkingTimeTextbox.Text = walkingTime;
                }
                if (NavigationContext.QueryString.TryGetValue("stopName", out stopName))
                {
                    StopNameTextbox.Text = stopName;
                }
            }
        }

        private void ApplicationBarIconButtonShare_Click(object sender, EventArgs e)
        {
            ShareLinkTask shareLinkTask = new ShareLinkTask();
            shareLinkTask.LinkUri = MiniBrowser.Source;
            shareLinkTask.Message = "Here are the details for that Translink Stop!";
            shareLinkTask.Show();
        }
    }
}