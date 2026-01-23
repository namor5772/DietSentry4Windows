using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace DietSentry
{
    public static class HelpSheetLayout
    {
        public static async Task ApplyMaxHeightAsync(Grid overlay, Border sheet, double maxHeightRatio)
        {
            if (overlay == null || sheet == null)
            {
                return;
            }

            if (maxHeightRatio <= 0)
            {
                return;
            }

            if (overlay.Height <= 0)
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                void Handler(object? sender, EventArgs e)
                {
                    if (overlay.Height > 0)
                    {
                        overlay.SizeChanged -= Handler;
                        tcs.TrySetResult(true);
                    }
                }

                overlay.SizeChanged += Handler;
                await Task.Yield();
                if (overlay.Height <= 0)
                {
                    await Task.WhenAny(tcs.Task, Task.Delay(60));
                }
            }

            var maxHeight = overlay.Height;
            if (maxHeight <= 0)
            {
                var windowPage = overlay.Window?.Page;
                if (windowPage != null)
                {
                    maxHeight = windowPage.Height;
                }
            }

            if (maxHeight > 0)
            {
                sheet.MaximumHeightRequest = maxHeight * maxHeightRatio;
            }
        }
    }
}
