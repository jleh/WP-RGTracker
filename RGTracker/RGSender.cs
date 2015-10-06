
using System;
using System.IO.IsolatedStorage;
using System.Net;
using Windows.Devices.Geolocation;


public class RGSender
{

    const int MAX_LOCATION_ACCURACY = 25;

    private int runnerId;
    private String runnerName;
    private String serverAddress;
    private String password;
    private RGTracker.MainPage mainPage;

    private int successful = 0;
    private int errors = 0;
    private int discarded = 0;

    public RGSender(RGTracker.MainPage mainPage)
    {
        this.runnerName = (String)IsolatedStorageSettings.ApplicationSettings["RunnerName"];
        this.runnerId = (int)IsolatedStorageSettings.ApplicationSettings["RunnerID"];
        this.serverAddress = (String)IsolatedStorageSettings.ApplicationSettings["ServerAddress"];
        this.password = (String)IsolatedStorageSettings.ApplicationSettings["Password"];

        this.mainPage = mainPage;
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
            SendToServer(geoposition.Coordinate); // TODO: Remove
        }
    }

    public void SendToServer(Geocoordinate coordinate)
    {
        DateTimeOffset time = coordinate.Timestamp;
        Int32 timestamp = (Int32)time.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        Int32 lat = Convert.ToInt32(Math.Floor(coordinate.Latitude * 1000000));
        Int32 lon = Convert.ToInt32(Math.Floor(coordinate.Longitude * 1000000));

        String AdditionalData = "act=s&n=" + runnerName + "&c=" + runnerId + "&p=" + password;
        String RGData = timestamp + "," + lat + "," + lon;
        String URL = serverAddress + "?" + AdditionalData + "&d=" + RGData;

        WebClient webClient = new WebClient();
        webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(RequestCompleted);
        webClient.DownloadStringAsync(new System.Uri(URL));
    }

    private void RequestCompleted(object sender, DownloadStringCompletedEventArgs e)
    {
        String statusText;

        if (e.Error == null)
        {
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
