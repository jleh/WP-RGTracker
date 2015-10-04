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
        private int RunnerId;
        private String RunnerName;
        private String ServerAddress;
        private String Password;

        private int errors = 0;
        private int successful = 0;

        const int MAX_LOCATION_ACCURACY = 15;

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
            InitializeGeolocator();
            IsTracking = true;

            Dispatcher.BeginInvoke(() =>
            {
                StopTrackingButton.IsEnabled = true;
                StartTrackingButton.IsEnabled = false;

                if (RunnerIDInput.Text == "")
                    RunnerIDInput.Text = "-1";

                RunnerId = int.Parse(RunnerIDInput.Text);
                ServerAddress = ServerAddressInput.Text;
                RunnerName = NameInput.Text;
                Password = PasswordInput.Text;

                // Save values
                IsolatedStorageSettings.ApplicationSettings["RunnerID"] = RunnerId;
                IsolatedStorageSettings.ApplicationSettings["ServerAddress"] = ServerAddress;
                IsolatedStorageSettings.ApplicationSettings["RunnerName"] = RunnerName;
                IsolatedStorageSettings.ApplicationSettings["Password"] = Password;

                IsolatedStorageSettings.ApplicationSettings.Save();
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
        }

        void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (!IsTracking)
                return;

            if (!App.RunningInBackground)
            {
                Dispatcher.BeginInvoke(() => {
                    String lat = args.Position.Coordinate.Latitude.ToString("0.000000");
                    String lon = args.Position.Coordinate.Longitude.ToString("0.000000");
                    String accuracy = args.Position.Coordinate.Accuracy.ToString("0") + " m";

                    if (args.Position.Coordinate.Accuracy > MAX_LOCATION_ACCURACY)
                        TrackingInfoTextBlock.Text = "Accuracy too low " + accuracy + " position data discarded.";
                    else
                        TrackingInfoTextBlock.Text = lat + " " + lon + " " + accuracy;
                });
            }

            if (args.Position.Coordinate.Accuracy <= MAX_LOCATION_ACCURACY)
                SendToServer(args.Position.Coordinate);
        }

        private void SendToServer(Geocoordinate coordinate)
        {
            DateTimeOffset time = coordinate.Timestamp;
            Int32 timestamp = (Int32)time.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            Int32 lat = Convert.ToInt32(Math.Floor(coordinate.Latitude * 1000000));
            Int32 lon = Convert.ToInt32(Math.Floor(coordinate.Longitude * 1000000));

            String AdditionalData = "act=s&n=" + RunnerName + "&c=" + RunnerId + "&p=" + Password;
            String RGData = timestamp + "," + lat + "," + lon;
            String URL = ServerAddress + "?" + AdditionalData + "&d=" + RGData;

            WebClient webClient = new WebClient();
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(RequestCompleted);
            webClient.DownloadStringAsync(new System.Uri(URL));
        }

        private void RequestCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                successful++;

                if (!App.RunningInBackground) {
                    Dispatcher.BeginInvoke(() =>
                    {
                        LastResponseText.Text = e.Result;
                        SendToServerText.Text = "Successful updates: " + successful;
                    });
                }
            }
            else
            {
                errors++;

                if (!App.RunningInBackground) {
                    Dispatcher.BeginInvoke(() =>
                    {
                        LastResponseText.Text = e.Error.Message;
                        SendToServerFailedText.Text = "Errors: " + errors;
                    });
                } 
            }
        }
    }
}