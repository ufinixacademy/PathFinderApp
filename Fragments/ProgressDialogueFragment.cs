using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace PathFinder.Fragments
{
    public class ProgressDialogueFragment : Android.Support.V4.App.DialogFragment
    {
       
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        string status;
        public ProgressDialogueFragment(string thisStatus)
        {
            status = thisStatus;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
          
            View view = inflater.Inflate(Resource.Layout.progress, container, false);
            TextView statustext = (TextView)view.FindViewById(Resource.Id.progressStatus);
            statustext.Text = status;
            return view;
        }
    }
}