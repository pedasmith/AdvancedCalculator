using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;

// The media team is simply incompetant.  They have a "simple" media access
// layer that can't be used without undocumented bizarro C# code.
[ComImport]
[Guid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
unsafe interface IMemoryBufferByteAccess
{
    void GetBuffer(out byte* buffer, out uint capacity);
}
namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public class RTLSensorMicrophone : IObjectValue
    {
        private string Name = "(microphone)";
        private int ActionCount { get; set; } = 0;

        public string PreferredName
        {
            get { return "Microphone"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("Start,Stop,ToString"); 
                case "Name":
                    return new BCValue(Name);
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void SetValue(string name, BCValue value)
        {
            ; // Can't ever set a constant.
        }

        public IList<string> GetNames()
        {
            return new List<string>() { "Methods" };
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
            Stop();
        }

        Task Running;
        CancellationTokenSource cts = null;
        AudioGraph audioGraph = null;
        AudioFrameOutputNode frameOutputNode = null;
        BCRunContext Context;
        string FunctionName = "";

        private async Task<string> Setup()
        {
            cts = new CancellationTokenSource();


            AudioGraphSettings settings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Speech);

            CreateAudioGraphResult createResult = await AudioGraph.CreateAsync(settings);
            if (createResult.Status != AudioGraphCreationStatus.Success)
            {
                var error = $"Microphone: AudioGraph creation error: {createResult.Status.ToString()}";
                RTLSystemX.AddError(error);
                return "Error: " + error;
            }
            audioGraph = createResult.Graph;

            audioGraph.EncodingProperties.Bitrate = 8000 * 32;
            audioGraph.EncodingProperties.BitsPerSample = 32;
            audioGraph.EncodingProperties.ChannelCount = 1;
            audioGraph.EncodingProperties.SampleRate = 8000;
            audioGraph.EncodingProperties.Subtype = "Float";

            var mics = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Media.Devices.MediaDevice.GetAudioCaptureSelector());
            DeviceInformation selectedMic = null;
            foreach (var mic in mics)
            {
                var type = mic.Kind;
                selectedMic = mic;
            }
            if (selectedMic == null)
            {
                RTLSystemX.AddError($"No microphone found");
            }
            else
            {
                // No actually an error: RTLSystemX.AddError($"Mic list {mics.Count} name {selectedMic.Name}");
            }
            Name = selectedMic.Name;

            //var encoding = new AudioEncodingProperties();
            CreateAudioDeviceInputNodeResult micResult = await audioGraph.CreateDeviceInputNodeAsync(Windows.Media.Capture.MediaCategory.Speech, audioGraph.EncodingProperties, selectedMic);

            if (micResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                var error = $"AudioGraph mic input result: {micResult.Status.ToString()}";
                RTLSystemX.AddError(error);
                return "Error: " + error;
            }

            //var deviceInputNode = micResult.DeviceInputNode;

            frameOutputNode = audioGraph.CreateFrameOutputNode();
            audioGraph.QuantumStarted += AudioGraph_QuantumStarted;
            micResult.DeviceInputNode.AddOutgoingConnection(frameOutputNode);

            return null;
        }

        private void AudioGraph_QuantumStarted(AudioGraph sender, object args)
        {
            // Got some data.  Get the actual "stuff" and call the BASIC function
            AudioFrame frame = frameOutputNode.GetFrame();
            var vl = ProcessFrameOutput(frame);
            if (vl != null)
            {
                var arglist = new List<IExpression>() { new ObjectConstant (vl) };
                Context.ProgramRunContext.AddCallback(new CallbackData(Context, FunctionName, arglist));
            }
        }


        unsafe private BCValueList ProcessFrameOutput(AudioFrame frame)
        {
            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);
                if (capacityInBytes > 0)
                {
                    var capacityInFloats = capacityInBytes / 4;
                    float* dataInFloat = (float*)dataInBytes;
                    var floats = new float[capacityInFloats];
                    Marshal.Copy((IntPtr)dataInFloat, floats, 0, (int)capacityInFloats);
                    int nNotZero = 0;
                    foreach (var item in floats)
                    {
                        if (item != 0) nNotZero++;
                    }
                    if (nNotZero > 0)
                    {
                        ;
                    }
                    // Write to the BASIC function
                    var vl = new BCValueList(floats);
                    return vl;
                }
            }
            return null;
        }

        private async Task<string> Start(string fname, BCRunContext context)
        {
            var ct = cts.Token;

            audioGraph.Start();
            while (!ct.IsCancellationRequested)
            {
                // The microphone calback happens regardless of what happens here.
                await Task.Delay(50);
            }

            audioGraph.Stop();
            return null;
        }

        public async Task DoStartAsync (BCRunContext context, String fname, BCValue Retval)
        {
            cts?.Cancel(); // Cancel the old one.
            var result = await Setup();
            if (result == null)
            {
                FunctionName = fname;
                Context = context;
                Running = Start(fname, context);
            }
            else
            {
                Retval.SetError(1, result);
            }
        }
        private void Stop()
        {
            if (cts == null)
            {
                RTLSystemX.AddError("Microphone is already stopped");
            }
            cts?.Cancel();
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Start":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var fname = (await ArgList[0].EvalAsync(context)).AsString;
                    await DoStartAsync(context, fname, Retval);
                    return RunResult.RunStatus.OK;
                case "Stop":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Stop();
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
