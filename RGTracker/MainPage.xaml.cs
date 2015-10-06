using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using RGTracker.Resources;
using Windows.Devices.Geolocation;
using System.IO.IsolatedStorage;

namespace RGTracker
{
    public partial class MainPage : PhoneApplicationPage
    {

        private Boolean IsTracking = false;
        private RGSender RGSender;
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            LoadSavedSettings();
        }

        private void LoadSavedSettings()
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("RunnerID"))
            {
                Dispatcher.BeginInvoke(() =>
                {
                    int RunnerIdValue = (int)IsolatedStorageSettings.ApplicationSettings["RunnerID"];

                    ServerAddressInput.Text = (String)IsolatedStorageSettings.ApplicationSettings["ServerAddress"];
                    RunnerIDInput.Text = RunnerIdValue.ToString();
                    NameInput.Text = (String)IsolatedStorageSettings.ApplicationSettings["RunnerName"];
                    PasswordInput.Text = (String)IsolatedStorageSettings.ApplicationSettings["Password"];
                });
            }
        }

        private void StartTrackingClick(object sender, RoutedEventArgs e)
        {
            IsTracking = true;

            Dispatcher.BeginInvoke(() =>
            {
                StopTrackingButton.IsEnabled = true;
                StartTrackingButton.IsEnabled = false;

                if (RunnerIDInput.Text == "")
                    RunnerIDInput.Text = "-1";

                // Save values
                IsolatedStorageSettings.ApplicationSettings["RunnerID"] = int.Parse(RunnerIDInput.Text);
                IsolatedStorageSettings.ApplicationSettings["ServerAddress"] = ServerAddressInput.Text;
                IsolatedStorageSettings.ApplicationSettings["RunnerName"] = NameInput.Text;
                IsolatedStorageSettings.ApplicationSettings["Password"] = PasswordInput.Text;

                IsolatedStorageSettings.ApplicationSettings.Save();

                RGSender = new RGSender(this);
                InitializeGeolocator();
            });
        }

        private void InitializeGeolocator()
        {
            if (App.Geolocator == null)
            {
                App.Geolocator = new Geolocator();
                App.Geolocator.DesiredAccuracy = PositionAccuracy.High;
                App.Geolocator.MovementThreshold = 5;
                App.Geolocator.PositionChanged += geolocator_PositionChanged;
            }
        }

        private void StopTrackingClick(object sender, RoutedEventArgs args)
        {
            StopTracking();
        }

        private void StopTracking()
        {
            IsTracking = false;

            Dispatcher.BeginInvoke(() => {
                StopTrackingButton.IsEnabled = false;
                StartTrackingButton.IsEnabled = true;
            });

            RGSender.Stop();
            RGSender = null;
        }

        void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (!IsTracking)
                return;
            else
                RGSender.AddPoint(args.Position);
        }

        public void UpdateCoordinateField(String text)
        {
            if (!App.RunningInBackground)
                Dispatcher.BeginInvoke(() =>
                {
                    TrackingInfoTextBlock.Text = text;
                });
        }

        public void UpdateStatusDisplay(int successful, int errors, int discarded, String status)
        {
            if (!App.RunningInBackground)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    LastResponseText.Text = status;
                    SendToServerFailedText.Text = "Errors: " + errors + " Discarded: " + discarded;
                    SendToServerText.Text = "Successful updates: " + successful;
                });
            } 
        }
    }
}