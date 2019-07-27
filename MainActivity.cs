using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Gms.Maps;
using Android;
using Android.Gms.Maps.Model;
using Android.Gms.Location;
using Android.Support.V4.App;
using PathFinder.Helpers;
using System;
using PathFinder.Fragments;
using Google.Places;
using System.Collections.Generic;
using Android.Content;

namespace PathFinder
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback
    {
        readonly string[] permissionGroup = { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation };

        TextView placeTextView;
        Button getDirectionButton;
        Button startTripButton;
        ImageView centerMarker;
        RelativeLayout placeLayout;
        ImageButton locationButton;

        GoogleMap map;
        FusedLocationProviderClient locationProviderClient;
        Android.Locations.Location myLastLocation;
        LatLng mypostion;
        LatLng destinationPoint;
        LocationRequest mLocationRequest;
        LocationCallbackHelper mLocationCallback = new LocationCallbackHelper();

        MapHelpers mapHelper = new MapHelpers();
        ProgressDialogueFragment ProgressDialogue;

        // Flags
        bool directionDrawn;
        private bool tripStarted;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            
            RequestPermissions(permissionGroup, 0);
            SupportMapFragment mapFragment = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.map);
            mapFragment.GetMapAsync(this);

            if (!PlacesApi.IsInitialized)
            {
                string key = Resources.GetString(Resource.String.mapkey);
                PlacesApi.Initialize(this, key);
            }

            startTripButton = (Button)FindViewById(Resource.Id.startTripButton);
            placeLayout = (RelativeLayout)FindViewById(Resource.Id.placeLayout);
            centerMarker = (ImageView)FindViewById(Resource.Id.centerMarker);
            placeTextView = (TextView)FindViewById(Resource.Id.placeTextView);
            getDirectionButton = (Button)FindViewById(Resource.Id.getDirectionsButton);
            locationButton = (ImageButton)FindViewById(Resource.Id.locationButton);
            locationButton.Click += LocationButton_Click;
            getDirectionButton.Click += GetDirectionButton_Click;
            startTripButton.Click += StartTripButton_Click;
            placeLayout.Click += PlaceLayout_Click;

            CreateLocationRequest();

        }

        private void LocationButton_Click(object sender, EventArgs e)
        {
            DisplayLocation();
        }

        private void StartTripButton_Click(object sender, EventArgs e)
        {
            if (!tripStarted)
            {
                Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Start Trip");
                alert.SetMessage("Are you sure");
                alert.SetPositiveButton("Start", (thisalert, args) =>
                {
                    locationProviderClient.RequestLocationUpdates(mLocationRequest, mLocationCallback, null);
                    tripStarted = true;
                    startTripButton.Text = "Stop Trip";
                });

                alert.SetNegativeButton("Cancel", (thisalert, args) =>
                {
                    alert.Dispose();
                });

                alert.Show();

            }
            else
            {
                Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Stop Trip");
                alert.SetMessage("Are you sure");
                alert.SetPositiveButton("Stop", (thisalert, args) =>
                {
                    locationProviderClient.RequestLocationUpdates(mLocationRequest, mLocationCallback, null);
                    tripStarted = false;
                    startTripButton.Text = "Start Trip";
                    Resetapp();
                });

                alert.SetNegativeButton("Cancel", (thisalert, args) =>
                {
                    alert.Dispose();
                });

                alert.Show();
            }

        }

        void Resetapp()
        {
            directionDrawn = false;
            map.Clear();
            centerMarker.Visibility = Android.Views.ViewStates.Visible;
            getDirectionButton.Visibility = Android.Views.ViewStates.Visible;
            startTripButton.Visibility = Android.Views.ViewStates.Invisible;
            DisplayLocation();
        }

       

        void CreateLocationRequest()
        {
            mLocationRequest = new LocationRequest();
            mLocationRequest.SetInterval(5);
            mLocationRequest.SetFastestInterval(5);
            mLocationRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
            mLocationRequest.SetSmallestDisplacement(1);
            mLocationCallback.OnLocationFound += MLocationCallback_OnLocationFound;
            if(locationProviderClient == null)
            {
                locationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
            }
        }

        private void MLocationCallback_OnLocationFound(object sender, LocationCallbackHelper.OnLocationCapturedEventArgs e)
        {
            myLastLocation = e.Location;
            // Updates Location when app is normal
            if (!directionDrawn)
            {
                if (myLastLocation != null)
                {
                    mypostion = new LatLng(myLastLocation.Latitude, myLastLocation.Longitude);
                    map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(mypostion, 15));
                }
            }
          

            //  Updates Location When Trip stared
            if (tripStarted)
            {
                if(myLastLocation != null)
                {
                    string key = Resources.GetString(Resource.String.mapkey);
                    mypostion = new LatLng(myLastLocation.Latitude, myLastLocation.Longitude);
                    mapHelper.UpdateLocationToDestination(mypostion, destinationPoint, map, key);
                }
            }
        }

        void StartLocationUpdates()
        {
            locationProviderClient.RequestLocationUpdates(mLocationRequest, mLocationCallback, null);
        }

        void StopLocationUpdates()
        {
            locationProviderClient.RemoveLocationUpdates(mLocationCallback);
        }

        private void PlaceLayout_Click(object sender, EventArgs e)
        {
            // Prevents the place search functionality when trip has started
            if (tripStarted)
            {
                return;
            }

            List<Place.Field> fields = new List<Place.Field>();
            fields.Add(Place.Field.Address);
            fields.Add(Place.Field.Name);
            fields.Add(Place.Field.LatLng);

            Intent intent = new Autocomplete.IntentBuilder(AutocompleteActivityMode.Overlay, fields)
                .SetCountry("NG")
                .Build(this);

            StartActivityForResult(intent, 0);
        }

        private  void GetDirectionButton_Click(object sender, System.EventArgs e)
        {
            GetDirection();
        }

        async void GetDirection()
        {
            getDirectionButton.Visibility = Android.Views.ViewStates.Invisible;
            startTripButton.Visibility = Android.Views.ViewStates.Visible;
            directionDrawn = true;
            centerMarker.Visibility = Android.Views.ViewStates.Invisible;
            string key = Resources.GetString(Resource.String.mapkey);

            ShowProgressDialogue("Getting Directions", false);
            string directionJson = await mapHelper.GetDirectionJsonAsync(mypostion, destinationPoint, key);
            map.Clear();
            mapHelper.DrawPolylineOnMap(directionJson, map);
            CloseProgressDialogue();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if(resultCode == Result.Ok)
            {
                var place = Autocomplete.GetPlaceFromIntent(data);
                placeTextView.Text = place.Name;
                destinationPoint = place.LatLng;
                GetDirection();
            }
            
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if(grantResults.Length < 1)
            {
                return;
            }

            if(grantResults[0] == (int) Android.Content.PM.Permission.Granted)
            {
                DisplayLocation();
            }
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            //Activates map styles

            //var mapStyle = MapStyleOptions.LoadRawResourceStyle(this, Resource.Raw.mapstyle);
            //googleMap.SetMapStyle(mapStyle);

            map = googleMap;
            map.UiSettings.ZoomControlsEnabled = true;
            map.CameraMoveStarted += Map_CameraMoveStarted;
            map.CameraIdle += Map_CameraIdle;

            if (CheckPersmission())
            {
                // DisplayLocation();
                StartLocationUpdates();
            }
           
        }

        public override void OnBackPressed()
        {
           
            if (directionDrawn)
            {
                // Resets the app
                directionDrawn = false;
                map.Clear();
                centerMarker.Visibility = Android.Views.ViewStates.Visible;
                getDirectionButton.Visibility = Android.Views.ViewStates.Visible;
                startTripButton.Visibility = Android.Views.ViewStates.Invisible;
                DisplayLocation();
            }
            else
            {
                base.OnBackPressed();
            }
        }

        private async void Map_CameraIdle(object sender, System.EventArgs e)
        {
            if (directionDrawn)
            {
                return;
            }

            destinationPoint = map.CameraPosition.Target;
            string key = Resources.GetString(Resource.String.mapkey);
            string address = await mapHelper.FindCordinateAddress(destinationPoint, key);

            if (!string.IsNullOrEmpty(address))
            {
                placeTextView.Text = address;
            }
            else
            {
                placeTextView.Text = "Where to?";
            }
           
        }

        private void Map_CameraMoveStarted(object sender, GoogleMap.CameraMoveStartedEventArgs e)
        {
            if (directionDrawn)
            {
                return;
            }
            placeTextView.Text = "Setting new location";
        }

        bool CheckPersmission()
        {
            bool permissionGranted = false;

            if(ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Android.Content.PM.Permission.Granted && 
                ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Android.Content.PM.Permission.Granted)
            {
                permissionGranted = false;
            }
            else
            {
                permissionGranted = true;
            }

            return permissionGranted;
        }

       async void DisplayLocation()
        {
            if(locationProviderClient == null)
            {
                locationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
                
            }

            myLastLocation = await locationProviderClient.GetLastLocationAsync();
            if(myLastLocation != null)
            {
                mypostion = new LatLng(myLastLocation.Latitude, myLastLocation.Longitude);
                map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(mypostion, 15));
            }
        }

        void ShowProgressDialogue(string status, bool cancelable)
        {
            ProgressDialogue = new ProgressDialogueFragment(status);
            ProgressDialogue.Cancelable = cancelable;
            var trans = SupportFragmentManager.BeginTransaction();
            ProgressDialogue.Show(trans, "Progress");
        }

        void CloseProgressDialogue()
        {
            if(ProgressDialogue != null)
            {
                ProgressDialogue.Dismiss();
                ProgressDialogue = null;
            }
        }
    }
}