using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceRecogEvalClient.Droid
{
    [Activity(Label = "VoiceEval", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            _ = EnsureAppPermission(Manifest.Permission.RecordAudio, Manifest.Permission.ModifyAudioSettings);
        }

        public async Task EnsureAppPermission(params string[] perms)
        {
            List<string> neededPerms;
            do
            {
                neededPerms = new List<string>();
                foreach (var perm in perms)
                {
                    var permissionLevel = base.PackageManager.CheckPermission(perm, base.PackageName);
                    if (permissionLevel != Permission.Granted)
                    {
                        neededPerms.Add(perm);
                    }
                }
                if (neededPerms.Any())
                {
                    var waitTask = OnRequestPermissionsResultHandle.WaitOneAsync();
                    RequestPermissions(neededPerms.ToArray(), 1);
                    await waitTask;
                }
            }
            while (neededPerms.Any());
        }

        AutoResetEvent OnRequestPermissionsResultHandle = new AutoResetEvent(false);

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            OnRequestPermissionsResultHandle.Set();
        }
    }
}