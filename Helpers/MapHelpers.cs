using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Gms.Maps.Utils;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;
using Newtonsoft.Json;
using ufinix.Directions;
using ufinix.Helpers;

namespace PathFinder.Helpers
{
    public class MapHelpers
    {
        Marker currentPositionMarker;
       bool isRequestingDirection;

        public async Task<string> FindCordinateAddress(LatLng position, string mapkey)
        {
            string url = "https://maps.googleapis.com/maps/api/geocode/json?latlng=" + position.Latitude.ToString() + "," + position.Longitude.ToString() + "&key=" + mapkey;
            string placeAdress = "";
            var handler = new HttpClientHandler();
            HttpClient httpClient = new HttpClient(handler);
            string result = await httpClient.GetStringAsync(url);

            if (!string.IsNullOrEmpty(result))
            {
                var geoCodeData = JsonConvert.DeserializeObject<GeocodingParser>(result);
                if (geoCodeData.status.Contains("OK"))
                {
                    placeAdress = geoCodeData.results[0].formatted_address;
                }
            }

            return placeAdress;
        }

        public async Task<string> GetDirectionJsonAsync(LatLng location, LatLng destination, string mapkey)
        {
            // https://maps.googleapis.com/maps/api/directions/json?origin=4.79317,7.0019&destination=4.69317,7.1019&mode=driving&key=AIzaSyCeNsnHKHXDSguq29pUYKPBFg5rZtU28aQ

            // Origin of route
            string str_origin = "origin=" + location.Latitude.ToString() + "," + location.Longitude.ToString();

            // Destination of route
            string str_destination = "destination=" + destination.Latitude.ToString() + "," + destination.Longitude.ToString();

            // Mode
            string mode = "mode=driving";

            // Building the parameters to the webservice
            string parameters = str_origin + "&" + str_destination + "&" + mode + "&key=" + mapkey;

            // Output Format
            string output = "json";

            string url = "https://maps.googleapis.com/maps/api/directions/" + output + "?" + parameters;

            var handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);
            string jsonString = await client.GetStringAsync(url);

            return jsonString;
        }

        public void DrawPolylineOnMap(string json, GoogleMap mainMap)
        {
            Android.Gms.Maps.Model.Polyline mPolyLine;

            var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);

            string durationString = directionData.routes[0].legs[0].duration.text;
            string distanceString = directionData.routes[0].legs[0].distance.text;

            var polylineCode = directionData.routes[0].overview_polyline.points;
            var line = PolyUtil.Decode(polylineCode);

            LatLng firstpoint = line[0];
            LatLng lastpoint = line[line.Count - 1];
            ArrayList routeList = new ArrayList();

            int locationCount = 0;
            foreach(LatLng item in line)
            {
                routeList.Add(item);
                locationCount++;

                Console.WriteLine("Poistion " + locationCount.ToString() + " = " + item.Latitude + "," + item.Longitude.ToString());
            }

            PolylineOptions polylineOptions = new PolylineOptions()
                .AddAll(routeList)
                .InvokeWidth(10)
                .InvokeColor(Color.Teal)
                .Geodesic(true)
                .InvokeJointType(JointType.Round);

            mPolyLine = mainMap.AddPolyline(polylineOptions);

            // Location Marker
            MarkerOptions locationMarkerOption = new MarkerOptions();
            locationMarkerOption.SetPosition(firstpoint);
            locationMarkerOption.SetTitle("My Location");
            locationMarkerOption.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));
            Marker locationmarker = mainMap.AddMarker(locationMarkerOption);

            // Destination Marker
            MarkerOptions destinationMarkerOption = new MarkerOptions();
            destinationMarkerOption.SetPosition(lastpoint);
            destinationMarkerOption.SetTitle("Destination");
            destinationMarkerOption.SetSnippet(durationString + ", " + distanceString);
            destinationMarkerOption.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed));
            Marker destinationMarker = mainMap.AddMarker(destinationMarkerOption);

            // Current Location Marker
            MarkerOptions positionMakerOption = new MarkerOptions();
            positionMakerOption.SetPosition(firstpoint);
            positionMakerOption.SetTitle("Current Location");
            positionMakerOption.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.positionmarker));
            positionMakerOption.Visible(false);
            currentPositionMarker = mainMap.AddMarker(positionMakerOption);

            // Location Circle
            CircleOptions locationCircleOption = new CircleOptions();
            locationCircleOption.InvokeCenter(firstpoint);
            locationCircleOption.InvokeRadius(30);
            locationCircleOption.InvokeStrokeColor(Color.Teal);
            locationCircleOption.InvokeFillColor(Color.Teal);
            mainMap.AddCircle(locationCircleOption);

            // Destination Circle
            CircleOptions destiationCircleOption = new CircleOptions();
            destiationCircleOption.InvokeCenter(lastpoint);
            destiationCircleOption.InvokeRadius(30);
            destiationCircleOption.InvokeStrokeColor(Color.Teal);
            destiationCircleOption.InvokeFillColor(Color.Teal);
            mainMap.AddCircle(destiationCircleOption);

            LatLng southwest = new LatLng(directionData.routes[0].bounds.southwest.lat, directionData.routes[0].bounds.southwest.lng);
            LatLng northeast = new LatLng(directionData.routes[0].bounds.northeast.lat, directionData.routes[0].bounds.northeast.lng);

            LatLngBounds tripBounds = new LatLngBounds(southwest, northeast);
            mainMap.AnimateCamera(CameraUpdateFactory.NewLatLngBounds(tripBounds, 150));
            mainMap.UiSettings.ZoomControlsEnabled = true;
            destinationMarker.ShowInfoWindow();

        }

        public async void UpdateLocationToDestination(LatLng currentPosition, LatLng destination, GoogleMap map, string key)
        {
            currentPositionMarker.Visible = true;
            currentPositionMarker.Position = currentPosition;
            map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(currentPosition, 15));

            if (!isRequestingDirection)
            {
                isRequestingDirection = true;
                string json = await GetDirectionJsonAsync(currentPosition, destination, key);
                var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);
                string duration = directionData.routes[0].legs[0].duration.text;
                string distance = directionData.routes[0].legs[0].distance.text;

                currentPositionMarker.Title = "Current Location";
                currentPositionMarker.Snippet = "Your Destination is " + duration + " , " + distance + " Away";
                currentPositionMarker.ShowInfoWindow();
                isRequestingDirection = false;
            }
           
        }

    }
}