using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public class RTLString : IObjectValue
    {
        public string PreferredName
        {
            get { return "String"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("Escape,Parse,Pos,Replace,ToLower,ToUpper,ToString");
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

        private static double Range (double minvalue, double value, double maxvalue)
        {
            if (value < minvalue) return minvalue;
            if (value > maxvalue) return maxvalue;
            return value;
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Escape":
                    {
                        if (!BCObjectUtilities.CheckArgs(2, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var escape = (await ArgList[0].EvalAsync(context)).AsString;
                        int narg = 2;
                        if (escape == "color") narg = 4;
                        if (!BCObjectUtilities.CheckArgs(narg, narg, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        switch (escape)
                        {
                            case "color":
                                {
                                    var r = (await ArgList[1].EvalAsync(context)).AsDouble;
                                    var g = (await ArgList[2].EvalAsync(context)).AsDouble;
                                    var b = (await ArgList[3].EvalAsync(context)).AsDouble;
                                    r = Range(0, r, 255);
                                    g = Range(0, g, 255);
                                    b = Range(0, b, 255);
                                    var str = String.Format("#{0:X2}{1:X2}{2:X2}", (byte)r, (byte)g, (byte)b);
                                    Retval.AsString = str;
                                    return RunResult.RunStatus.OK;
                                }
                            case "csv":
                                {
                                    var obj = (await ArgList[1].EvalAsync(context));
                                    if (obj.IsArray)
                                    {
                                        Retval.AsString = RTLCsvRfc4180.Encode(obj.AsArray);
                                    }
                                    else
                                    {
                                        Retval.AsString = RTLCsvRfc4180.Encode(obj.AsString);
                                    }
                                    return RunResult.RunStatus.OK;
                                }
                            case "json":
                                {
                                    var obj = await ArgList[1].EvalAsync(context);
                                    if (obj.IsArray)
                                    {
                                        var sb = new StringBuilder();
                                        sb.Append("{\n");
                                        bool isFirst = true;
                                        foreach (var row in obj.AsArray.data)
                                        {
                                            if (row.IsArray && row.AsArray.data.Count >= 2)
                                            {
                                                if (!isFirst) sb.Append(",\n");
                                                isFirst = false;
                                                var itemName = StringUtils.EncodeJson(row.AsArray.data[0].AsString);
                                                var value = row.AsArray.data[1];
                                                string itemValue;
                                                switch (value.CurrentType)
                                                {
                                                    case BCValue.ValueType.IsDouble:
                                                        itemValue = value.AsDouble.ToString();
                                                        break;
                                                    case BCValue.ValueType.IsObject:
                                                        if (value.AsObject is RTLDateTime)
                                                        {
                                                            itemValue = (value.AsObject as RTLDateTime).GetValue("Iso8601").AsString;
                                                        }
                                                        else
                                                        {
                                                            itemValue = value.AsString;
                                                        }
                                                        itemValue = StringUtils.EncodeJson(itemValue);
                                                        break;
                                                    default:
                                                        itemValue = StringUtils.EncodeJson(value.AsString);
                                                        break;
                                                }
                                                sb.Append($"{itemName}:{itemValue}");
                                            }
                                        }
                                        sb.Append("\n}");
                                        Retval.AsString = sb.ToString();
                                    }
                                    else
                                    {
                                        var result = StringUtils.EncodeJson(obj.AsString);
                                        Retval.AsString = result;
                                    }
                                    return RunResult.RunStatus.OK;
                                }
                        }
                    }
                    return RunResult.RunStatus.ErrorStop;
                case "Parse":
                    {
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var escape = (await ArgList[0].EvalAsync(context)).AsString;
                        var str = (await ArgList[1].EvalAsync(context)).AsString;
                        BCValueList resultList;
                        switch (escape)
                        {
                            case "csv":
                                resultList = RTLCsvRfc4180.Parse(str);
                                Retval.AsObject = resultList;
                                return RunResult.RunStatus.OK;

                            case "json":
                                resultList = StringUtils.ParseJson(str);
                                Retval.AsObject = resultList;
                                return RunResult.RunStatus.OK;
                        }
                    }
                    return RunResult.RunStatus.ErrorStop;
                case "Pos":
                    {
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        await BCBasicStringFunctions.POSAsync(context, ArgList, Retval);
                        return RunResult.RunStatus.OK;
                    }
                case "Replace":
                    {
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        await BCBasicStringFunctions.ReplaceAsync(context, ArgList, Retval);
                        return RunResult.RunStatus.OK;
                    }
                case "ToLower":
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var str = (await ArgList[0].EvalAsync(context)).AsString;
                        Retval.AsString = str.ToLower();
                        return RunResult.RunStatus.OK;
                    }
                case "ToUpper":
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var str = (await ArgList[0].EvalAsync(context)).AsString;
                        Retval.AsString = str.ToUpper();
                        return RunResult.RunStatus.OK;
                    }
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


    class StringUtils
    {
        public static string EncodeJson(string data)
        {
            var val = Windows.Data.Json.JsonValue.CreateStringValue(data);
            var retval = val.Stringify();
            return retval;
        }

        private static BCValue JsonValueToValue(IJsonValue subvalue)
        {
            switch (subvalue.ValueType)
            {
                case JsonValueType.Array:
                    return new BCValue(JsonValueToValueList(subvalue.GetArray()));
                case JsonValueType.Number:
                    return new BCValue(subvalue.GetNumber());
                case JsonValueType.String:
                    return new BCValue(subvalue.GetString());
            }
            return BCValue.MakeError(700, $"Cant convert json type {subvalue.ValueType} ");
        }

        private static BCValueList JsonValueToValueList (IJsonValue value)
        {
            var Retval = new BCValueList();
            switch (value.ValueType)
            {
                case JsonValueType.Array:
                    var arr = value.GetArray();
                    foreach (var item in arr)
                    {
                        var bcvalue = JsonValueToValue(item);
                        Retval.data.Add(bcvalue);
                    }
                    break;
                case JsonValueType.Object:
                    var obj = value.GetObject();
                    foreach (var subvaluePair in obj)
                    {
                        var name = subvaluePair.Key;
                        var subvalue = subvaluePair.Value;
                        var bcvalue = JsonValueToValue(subvalue);
                        Retval.SetProperty(name, bcvalue);
                    }
                    break;
            }
            return Retval;
        }
        public static BCValueList ParseJson(string data)
        {
            JsonValue jsonValue;
            var val = Windows.Data.Json.JsonValue.TryParse (data, out jsonValue);
            var Retval = val ? JsonValueToValueList(jsonValue) : null;
            return Retval;
        }
    }
}
