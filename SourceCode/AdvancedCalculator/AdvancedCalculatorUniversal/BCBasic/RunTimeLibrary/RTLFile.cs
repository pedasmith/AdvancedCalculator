using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public class RTLFile : IObjectValue
    {
        enum OpenType {  None, Append, Read, Write };
        private StorageFile CurrFile { get; set; } = null;
        private OpenType CurrFileOpenType { get; set; } = OpenType.None;

        private int ActionCount { get; set; } = 0;

        public string PreferredName
        {
            get { return "File"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    switch (CurrFileOpenType)
                    {
                        case OpenType.None:
                            return new BCValue("AppendPicker,ReadPicker,WritePicker,ToString");
                        case OpenType.Append:
                            return new BCValue("AppendLine,AppendText,Size,ToString");
                        case OpenType.Write:
                            return new BCValue("WriteLine,WriteText,Size,ToString");
                        case OpenType.Read:
                            return new BCValue("ReadAll,ReadLines,Size,ToString");
                    }
                    return new BCValue("ToString");
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
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "AppendLine":
                case "AppendText":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (CurrFile == null || CurrFileOpenType != OpenType.Append)
                    {
                        Retval.SetError(801, "File was not opened for appending");
                    }
                    else
                    {
                        var str = (await ArgList[0].EvalAsync(context)).AsString;
                        if (name == "AppendLine") str += "\r\n";
                        try
                        {
                            await FileIO.AppendTextAsync(CurrFile, str);
                            Retval.AsString = ""; //Note: what's the best return value here?
                        }
                        catch (Exception e)
                        {
                            Retval.SetError(e.HResult, e.Message);
                        }

                        ActionCount++;
                    }
                    return RunResult.RunStatus.OK;
                case "AppendPicker":
                    {
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        // Can't use the FileOpenPicker because it won't make new files
                        //var f = new FileOpenPicker(); 
                        var f = new FileSavePicker();
                        f.CommitButtonText = "Append";

                        var fileExplanation = (await ArgList[0].EvalAsync(context)).AsString;
                        var fileExt = (await ArgList[1].EvalAsync(context)).AsString;
                        f.FileTypeChoices.Add(fileExplanation, new List<string>() { fileExt });
                        //f.FileTypeFilter.Add(fileExt); // Expecting e.g. .txt


                        var fileName = (await ArgList[2].EvalAsync(context)).AsString;
                        f.SuggestedFileName = fileName;

                        var result = await f.PickSaveFileAsync();
                        if (result == null)
                        {
                            Retval.SetError(802, "No file was selected for append");
                        }
                        else
                        {
                            Retval.AsObject = new RTLFile() { CurrFile = result, CurrFileOpenType = OpenType.Append };
                        }
                    }
                    return RunResult.RunStatus.OK;
                case "ReadAll":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (CurrFile == null || CurrFileOpenType != OpenType.Read)
                    {
                        Retval.SetError(803, "File was not opened for reading");
                    }
                    else
                    {
                        var text = await FileIO.ReadTextAsync(CurrFile);
                        Retval.AsString = text;
                        ActionCount++;
                    }
                    return RunResult.RunStatus.OK;
                case "ReadLines":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (CurrFile == null || CurrFileOpenType != OpenType.Read)
                    {
                        Retval.SetError(804, "File was not opened for reading");
                    }
                    else
                    {
                        var lines = await FileIO.ReadLinesAsync(CurrFile);
                        var array = new BCValueList();
                        foreach (var line in lines)
                        {
                            array.data.Add(new BCValue(line));
                        }
                        Retval.AsObject = array; // automatically sets the IsArray
                        ActionCount++;
                    }
                    return RunResult.RunStatus.OK;
                case "ReadPicker":
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var f = new FileOpenPicker();

                        var ext = await ArgList[0].EvalAsync(context);
                        if (ext.IsArray)
                        { 
                            var extArray = ext.AsArray;
                            foreach (var item in extArray.data)
                            {
                                f.FileTypeFilter.Add(item.AsString); // Expecting e.g. .txt
                            }
                        }
                        else
                        {
                            f.FileTypeFilter.Add(ext.AsString); // Expecting e.g. .txt
                        }

                        var result = await f.PickSingleFileAsync();
                        if (result == null)
                        {
                            Retval.SetError(805, "No file was selected for read");
                        }
                        else
                        {
                            Retval.AsObject = new RTLFile() { CurrFile = result, CurrFileOpenType = OpenType.Read };
                        }
                    }
                    return RunResult.RunStatus.OK;

                case "Size":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (CurrFile == null)
                    {
                        Retval.SetError(806, "File was not opened");
                    }
                    else
                    {
                        var results = await CurrFile.Properties.RetrievePropertiesAsync(new List<string>() { "System.Size" });
                        if (results.ContainsKey("System.Size"))
                        {
                            var size = results["System.Size"];
                            Retval.AsDouble = (Double)(UInt64)size;
                        }
                        else
                        {
                            Retval.SetError(810, "Unable to get size");
                        }
                    }
                    return RunResult.RunStatus.OK;


                case "WriteLine":
                case "WriteText":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (CurrFile == null || CurrFileOpenType != OpenType.Write)
                    {
                        Retval.SetError(806, "File was not opened for writing");
                    }
                    else
                    {
                        var str = (await ArgList[0].EvalAsync(context)).AsString;
                        if (name == "WriteLine") str += "\r\n";
                        try
                        {
                            if (ActionCount == 0)
                            {
                                await FileIO.WriteTextAsync(CurrFile, str);
                            }
                            else
                            {
                                await FileIO.AppendTextAsync(CurrFile, str);
                            }
                            ActionCount++;
                            Retval.AsString = ""; //Note: what's the best return value here?
                        }
                        catch (Exception e)
                        {
                            Retval.SetError(e.HResult, e.Message);
                        }
                    }
                    return RunResult.RunStatus.OK;
                case "WritePicker":
                    {
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var f = new FileSavePicker();

                        var fileExplanation = (await ArgList[0].EvalAsync(context)).AsString;
                        var fileExt = (await ArgList[1].EvalAsync(context)).AsString;
                        f.FileTypeChoices.Add(fileExplanation, new List<string>() { fileExt });

                        var fileName = (await ArgList[2].EvalAsync(context)).AsString;
                        f.SuggestedFileName = fileName;

                        var result = await f.PickSaveFileAsync();
                        if (result == null)
                        {
                            Retval.SetError(807, "No file was selected for write");
                        }
                        else
                        {
                            // Clear out the current file?
                            Retval.AsObject = new RTLFile() { CurrFile = result, CurrFileOpenType = OpenType.Write };
                        }
                    }
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
