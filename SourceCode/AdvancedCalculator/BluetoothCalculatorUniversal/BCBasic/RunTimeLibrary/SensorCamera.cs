using BCBasic;
using BCBasic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Geolocation;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public class RTLSensorCamera : IObjectValue
    {
        public RTLSensorCamera (string cameraType)
        {
            switch (cameraType)
            {
                case "Front": PreferredCameraPanel = Windows.Devices.Enumeration.Panel.Front; break;
                case "Back": PreferredCameraPanel = Windows.Devices.Enumeration.Panel.Back; break;
                    // NOTE: support the Face camera, too!
            }
        }
        Windows.Devices.Enumeration.Panel PreferredCameraPanel = Windows.Devices.Enumeration.Panel.Unknown;
        List<CameraAnalysis> AnalysisList = new List<CameraAnalysis>();
        Task PreviewTask = null;

        public string PreferredName { get { return "Camera"; } }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("Start,Stop,ToString");
                case "Name":
                    return new BCValue(PreferredName);
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        Task StartPreviewTask = null;
        CaptureElement CaptureElementToRemove = null;
        public void SetValue(string name, BCValue value)
        {
            switch (name)
            {
                case "Image":
                case "View": // View is the old term!
                    var graphics = value.AsObject;
                    CaptureElement captureElement = null;
                    if (graphics is GraphicsPrimitiveCamera)
                    {
                        captureElement = (value.AsObject as GraphicsPrimitiveCamera).fe as CaptureElement;
                    }
                    else if (graphics is GraphicsPrimitiveImage)
                    {
                        var image = graphics as GraphicsPrimitiveImage;
                        captureElement = new CaptureElement();
                        image.SetNewShape(captureElement);
                    }
                    if (captureElement != null)
                    {
                        if (sensor == null)
                        {
                            CaptureElementToSet = captureElement;
                            // We will set the captureElement.Source when
                            // the Start() method is called.
                        }
                        else
                        {
                            CaptureElementToRemove = captureElement;
                            captureElement.Source = sensor;
                            StartPreviewTask = sensor.StartPreviewAsync().AsTask();
                        }
                    }

                    break;

            }
        }
        CaptureElement CaptureElementToSet = null;

        public IList<string> GetNames()
        {
            return new List<string>() { "Methods" };
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
            var t = StopAsync();
        }

        public MediaCapture sensor { get; internal set; } = null;
        CancellationTokenSource cts = null;

        private static int Range (int minvalue, int value, int maxvalue)
        {
            if (value < minvalue) return minvalue;
            if (value > maxvalue) return maxvalue;
            return value;
        }

        int cancelRequested = 0;
        int cancelDetected = 0;
        private async Task StopAsync()
        {
            cancelRequested++;
            cts?.Cancel();
            if (StartPreviewTask != null && CaptureElementToRemove != null)
            {

                await CaptureElementToRemove.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    if (sensor != null && CaptureElementToRemove != null)
                    {
                        // All the steps from https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/simple-camera-preview-access
                        await sensor.StopPreviewAsync();
                        CaptureElementToRemove.Source = null;
                        sensor.Dispose();
                        sensor = null;
                    }
                });
            }
        }

        InterpolationLibrary defaultAnalysis = new InterpolationLibrary(0, 0, 255, 255);
        // dispklay a single frame
        private async Task DoFrameAsync(CancellationToken ct, CameraAnalysis analysis, VideoEncodingProperties previewProperties)
        {
            var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);

            double totalR = 0.0;
            double totalG = 0.0;
            double totalB = 0.0;
            double npixel = analysis.AnalysisW * analysis.AnalysisH;

            var rinterpolation = analysis.GetInterpolationLibrary(AnalysisType.R, defaultAnalysis);
            var ginterpolation = analysis.GetInterpolationLibrary(AnalysisType.G, defaultAnalysis);
            var binterpolation = analysis.GetInterpolationLibrary(AnalysisType.B, defaultAnalysis);

            using (VideoFrame sourceFrame = await sensor.GetPreviewFrameAsync(videoFrame))
            {
                SoftwareBitmap destSBmp = new SoftwareBitmap(BitmapPixelFormat.Bgra8, analysis.AnalysisW, analysis.AnalysisH);
                SoftwareBitmap sourceSBmp = sourceFrame.SoftwareBitmap;
                using (var sourceLockedBuffer = sourceSBmp.LockBuffer(BitmapBufferAccessMode.ReadWrite))
                {
                    using (var sourceReference = sourceLockedBuffer.CreateReference())
                    {
                        using (var destLockedBuffer = destSBmp.LockBuffer(BitmapBufferAccessMode.ReadWrite))
                        {
                            using (var destReference = destLockedBuffer.CreateReference())
                            {
                                unsafe
                                {
                                    byte* source;
                                    byte* dest;
                                    uint sourceCapacity;
                                    uint destCapacity;
                                    ((IMemoryBufferByteAccess)sourceReference).GetBuffer(out source, out sourceCapacity);
                                    ((IMemoryBufferByteAccess)destReference).GetBuffer(out dest, out destCapacity);

                                    double viewportw = previewProperties.Width * 2.0 * analysis.AnalysisRadius;
                                    double viewporth = previewProperties.Height * 2.0 * analysis.AnalysisRadius;

                                    double cx = (double)previewProperties.Width * analysis.AnalysisCX;
                                    double cy = (double)previewProperties.Height * analysis.AnalysisCY;

                                    double viewport_xoffset = cx - (viewportw / 2.0); 
                                    double viewport_yoffset = cy - (viewporth / 2.0);

                                    for (double y = 0; y< destSBmp.PixelHeight; y++)
                                    {
                                        for (double x=0; x< destSBmp.PixelWidth; x++)
                                        {
                                            double viewporty = (y * viewporth) / (double)destSBmp.PixelHeight;
                                            double viewportx = (x * viewportw) / (double)destSBmp.PixelWidth;

                                            int sourcey = (int)(viewporty + viewport_yoffset);
                                            sourcey = Range(0, sourcey, (int)previewProperties.Height);
                                            int sourcex = (int)(viewportx + viewport_xoffset);
                                            sourcex = Range(0, sourcex, (int)previewProperties.Width);

                                            long sourcei = 4 * ((long)(sourcey * previewProperties.Width + sourcex));
                                            if (sourcei > sourceCapacity-4) continue;
                                            long desti = 4 * (long)(y * destSBmp.PixelWidth + x);
                                            if (desti > destCapacity - 4) continue;

                                            const int B = 0;
                                            const int G = 1;
                                            const int R = 2;
                                            const int A = 3;

                                            double rrr = rinterpolation.Interpolate(source[sourcei + R]);
                                            double ggg = ginterpolation.Interpolate(source[sourcei + G]);
                                            double bbb = binterpolation.Interpolate(source[sourcei + B]);

                                            totalR += rrr;
                                            totalG += ggg;
                                            totalB += bbb;

                                            dest[desti + B] = (byte)bbb;
                                            dest[desti + G] = (byte)ggg;
                                            dest[desti + R] = (byte)rrr;
                                            dest[desti + A] = source[sourcei + A];
                                        }
                                    } // foreach pixel
                                } // unsafe memory handling
                            }
                        }
                    } // end using memory
                } // end using buffer

                if (analysis.Image != null)
                {
                    await analysis.Image.Image.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () =>
                        {
                            var wb = new WriteableBitmap(destSBmp.PixelWidth, destSBmp.PixelHeight);
                            destSBmp.CopyToBuffer(wb.PixelBuffer);
                            analysis.Image.Image.Source = wb;
                        });
                }
                analysis.CallFunction(totalR / npixel, totalG / npixel, totalB / npixel);
                // Make the callback, too
            } // end usng frame

        }

        public async Task StartAsync()
        {
            await StopAsync();
            cts = new CancellationTokenSource();
            if (sensor == null)
            {
                sensor = new MediaCapture();
                var init = new MediaCaptureInitializationSettings();
                var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                DeviceInformation selectedDevice = null;
                foreach (var device in devices)
                {
                    if (selectedDevice == null && device.IsEnabled) selectedDevice = device;
                    var devicePanel = device.EnclosureLocation?.Panel ?? Windows.Devices.Enumeration.Panel.Unknown;
                    if (PreferredCameraPanel == Windows.Devices.Enumeration.Panel.Unknown || PreferredCameraPanel == devicePanel)
                    {
                        selectedDevice = device;
                    }
                }
                if (selectedDevice != null)
                {
                    init.VideoDeviceId = selectedDevice.Id;
                }
                await sensor.InitializeAsync(init);

                var previewProperties = sensor.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                // Create a video frame in the desired format for the preview frame

                PreviewTask = new Task(async () =>
                {
                    var ct = cts.Token;
                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(10, ct);
                            foreach (var item in AnalysisList)
                            {
                                await DoFrameAsync(ct, item, previewProperties);
                            } // end of loop
                        }
                        catch (TaskCanceledException)
                        {
                            ;
                        }
                        catch (Exception)
                        {
                            ; // do nothing.  Keep going.
                        }
                    }
                    if (ct.IsCancellationRequested) cancelDetected++;
                });
                PreviewTask.Start();
                if (CaptureElementToSet != null)
                {
                    CaptureElementToSet.Source = sensor;
                }
            }
            if (sensor == null)
            {
                RTLSystemX.AddError($"Camera: this system has no camera");
            }
            else
            {
            }
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Analyze":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    // Example: camera.Analyze ()
                    var a = new CameraAnalysis();
                    a.Context = context;
                    AnalysisList.Add(a);
                    Retval.AsObject = a;
                    return RunResult.RunStatus.OK;
                case "Start":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    await StartAsync();
                    return RunResult.RunStatus.OK;
                case "Stop":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    await StopAsync();
                    return RunResult.RunStatus.OK;
                case "ToString":
                    Retval.AsString = $"Object {PreferredName}";
                    return RunResult.RunStatus.OK;
                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }


        public void Dispose()
        {
        }
    }
}
