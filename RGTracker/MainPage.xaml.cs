﻿using System;
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
using System.Reflection;
using System.Windows.Threading;

namespace RGTracker
{
    public partial class MainPage : PhoneApplicationPage
    {

        private Boolean isTracking = false;
        private RGSender RGSender;
        private AssemblyName appName = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
        private DispatcherTimer updater = new DispatcherTimer();
        
        private Geoposition lastKnownPosition;
        private int positionLoggingCounter;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            LoadSavedSettings();

            Dispatcher.BeginInvoke(() => {
                AppTitleText.Text = appName.Name + " " + appName.Version.ToString();
            });

            updater.Interval = new TimeSpan(0, 0, 1);
            updater.Tick += UpdateHandler;
            updater.Start();
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
            isTracking = true;
            positionLoggingCounter = 0;

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
            isTracking = false;

            Dispatcher.BeginInvoke(() => {
                StopTrackingButton.IsEnabled = false;
                StartTrackingButton.IsEnabled = true;
            });

            RGSender.Stop();
            RGSender = null;
        }

        void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (!isTracking)
                return;
            else
                lastKnownPosition = args.Position;
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

        private void UpdateHandler(object sender, EventArgs e)
        {
            if (!App.RunningInBackground && RGSender != null)
                RGSender.UpdateStatus();
            
            if (isTracking && lastKnownPosition != null && positionLoggingCounter++ == 3)
                RGSender.AddPoint(lastKnownPosition);

            if (positionLoggingCounter > 3)
                positionLoggingCounter = 0;
        }
    }
}