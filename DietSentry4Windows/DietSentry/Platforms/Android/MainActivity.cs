using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui.ApplicationModel;

namespace DietSentry
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int FolderPickerRequestCode = 41027;
        private static TaskCompletionSource<Android.Net.Uri?>? _folderPickerTcs;

        public static Task<Android.Net.Uri?> PickFolderAsync()
        {
            var activity = Platform.CurrentActivity;
            if (activity == null)
            {
                return Task.FromResult<Android.Net.Uri?>(null);
            }

            var tcs = new TaskCompletionSource<Android.Net.Uri?>();
            var existing = _folderPickerTcs;
            _folderPickerTcs = tcs;
            existing?.TrySetResult(null);

            var intent = new Intent(Intent.ActionOpenDocumentTree);
            intent.AddFlags(
                ActivityFlags.GrantReadUriPermission |
                ActivityFlags.GrantWriteUriPermission |
                ActivityFlags.GrantPersistableUriPermission |
                ActivityFlags.GrantPrefixUriPermission);

#pragma warning disable CA1422
            activity.StartActivityForResult(intent, FolderPickerRequestCode);
#pragma warning restore CA1422
            return tcs.Task;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode != FolderPickerRequestCode)
            {
                return;
            }

            var tcs = _folderPickerTcs;
            _folderPickerTcs = null;
            if (tcs == null)
            {
                return;
            }

            if (resultCode != Result.Ok || data?.Data == null)
            {
                tcs.TrySetResult(null);
                return;
            }

            var uri = data.Data;
            try
            {
                var flags = data.Flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                ContentResolver?.TakePersistableUriPermission(uri, flags);
            }
            catch (Exception)
            {
                // Ignore failures and continue using the returned Uri.
            }

            tcs.TrySetResult(uri);
        }
    }
}
