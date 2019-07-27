﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Location;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace PathFinder.Helpers
{
    public class LocationCallbackHelper : LocationCallback
    {
        public event EventHandler<OnLocationCapturedEventArgs> OnLocationFound;
        public class OnLocationCapturedEventArgs : EventArgs
        {
            public Android.Locations.Location Location { get; set; }
        }

        public override void OnLocationResult(LocationResult result)
        {
           if(result.Locations.Count != 0)
            {
                OnLocationFound?.Invoke(this, new OnLocationCapturedEventArgs { Location = result.Locations[0] });
            }
        }

        public override void OnLocationAvailability(LocationAvailability locationAvailability)
        {
          
        }

    }
}