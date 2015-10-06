
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Net;
using System.Windows.Threading;
using Windows.Devices.Geolocation;


public class RGSender
{

    const int MAX_LOCATION_ACCURACY = 25;
    const int SEND_INTERVAL_SECONDS = 7;

    private int runnerId;
    private String runnerName;
    private String serverAddress;
    private String password;
    private RGTracker.MainPage mainPage;

    private int successful = 0;
    private int errors = 0;
    private int discarded = 0;

    private int sendOffset = 0;

    private DispatcherTimer sendTimer;
    private List<Geoposition> coordinateList = new List<Geoposition>();

    public RGSender(RGTracker.MainPage mainPage)
    {
        this.runnerName = (String)IsolatedStorageSettings.ApplicationSettings["RunnerName"];
        this.runnerId = (int)IsolatedStorageSettings.ApplicationSettings["RunnerID"];
        this.serverAddress = (String)IsolatedStorageSettings.ApplicationSettings["ServerAddress"];
        this.password = (String)IsolatedStorageSettings.ApplicationSettings["Password"];

        this.mainPage = mainPage;

        sendTimer = new DispatcherTimer();
        sendTimer.Tick += new EventHandler(DoSend);
        sendTimer.Interval = new TimeSpan(0, 0, SEND_INTERVAL_SECONDS);
        sendTimer.Start();
    }

    public void Stop()
    {
        sendTimer.Stop();
    }

    public void AddPoint(Geoposition geoposition)
    {
        String lat = geoposition.Coordinate.Latitude.ToString("0.000000");
        String lon = geoposition.Coordinate.Longitude.ToString("0.000000");
        String accuracy = geoposition.Coordinate.Accuracy.ToString("0") + " m";

        if (geoposition.Coordinate.Accuracy >= MAX_LOCATION_ACCURACY) // Discard inaccurate position
        {
            discarded++;
            mainPage.UpdateStatusDisplay(successful, errors, discarded, "");
            mainPage.UpdateCoordinateField("Accuracy too low " + accuracy + " position data discarded.");
        }
        else
        {
            mainPage.UpdateCoordinateField(lat + " " + lon + " " + accuracy);
            coordinateList.Add(geoposition);
        }
    }

    private void DoSend(object sender, EventArgs e)
    {
        if (coordinateList.Count == 0)
            return;

        String URL = GetURLWithBasicParameters();
        Geoposition[] coordinates = coordinateList.ToArray();

        int timestampFirst = GetPointTimestamp(coordinates[0]);
        int latFirst = GetLat(coordinates[0]);
        int lonFirst = GetLon(coordinates[0]);

        String firstPointData = timestampFirst + "," + latFirst + "," + lonFirst + ",10x"; // Satellites "fixed" to 10
        URL += firstPointData;

        for (int i = 1; i < coordinates.Length; i++)
        {
            int timestamp = GetPointTimestamp(coordinates[i]) - timestampFirst;
            int lat = GetLat(coordinates[i]) - latFirst;
            int lon = GetLon(coordinates[i]) - lonFirst;

            URL += timestamp + "," + lat + "," + lon + ",10x";
        }

        sendOffset = coordinates.Length;

        SendToServer(URL);
    }

    private String GetURLWithBasicParameters()
    {
        return serverAddress + "?act=s&n=" + runnerName + "&c=" + runnerId + "&p=" + password + "&d=";
    }

    private int GetPointTimestamp(Geoposition geoposition)
    {
        return (int)geoposition.Coordinate.Timestamp.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    }

    private int GetLat(Geoposition geoposition)
    {
        return Convert.ToInt32(Math.Floor(geoposition.Coordinate.Latitude * 1000000)); ;
    }

    private int GetLon(Geoposition geoposition)
    {
        return Convert.ToInt32(Math.Floor(geoposition.Coordinate.Longitude * 1000000));
    }

    public void SendToServer(String URL)
    {
        WebClient webClient = new WebClient();
        webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(RequestCompleted);
        webClient.DownloadStringAsync(new System.Uri(URL));
    }

    private void RequestCompleted(object sender, DownloadStringCompletedEventArgs e)
    {
        String statusText;

        if (e.Error == null)
        {
            coordinateList.RemoveRange(0, sendOffset);
            sendOffset = 0;
            successful++;
            statusText = e.Result;
        }
        else
        {
            errors++;
            statusText = e.Error.Message;
        }

        mainPage.UpdateStatusDisplay(successful, errors, discarded, statusText);
    }

    
}
