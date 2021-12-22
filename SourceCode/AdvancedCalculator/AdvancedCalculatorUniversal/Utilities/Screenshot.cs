using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Utilities
{
    public class Screenshot
    {
        public static async Task TakeScreenshotAsync(FrameworkElement el, double widthInInches, double heightInInches, StorageFolder folder = null, string filename = null)
        {
            if (folder == null) folder = ApplicationData.Current.LocalFolder;
            if (filename == null || filename == "") filename = "screenshot.png";
            var renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(el); //, width, height);

            var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

            var file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                var pw = renderTargetBitmap.PixelWidth;
                var ph = renderTargetBitmap.PixelHeight;

                double dpiw = pw / widthInInches;
                double dpih = ph / heightInInches;
                if (double.IsNaN(heightInInches) && double.IsNaN(widthInInches))
                {
                    dpiw = 96; // 96 dpi is a nice default.
                    dpih = 96;
                }
                else if (double.IsNaN(heightInInches))
                {
                    dpih = dpiw;
                }
                else if (double.IsNaN(widthInInches))
                {
                    dpiw = dpih;
                }
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    (uint)pw, (uint)ph, dpiw, dpih,
                    pixelBuffer.ToArray());

                await encoder.FlushAsync();
                //RenderedImage.Source = renderTargetBitmap;
            }
        }
    }
}
