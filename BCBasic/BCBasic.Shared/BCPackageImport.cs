using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using System.Linq;

namespace BCBasic
{
    public class BCPackageImport
    {
        public static async Task<bool> InitFromFile(StorageFile file, BCPackage newPackage)
        {
            var packageOk = false;
            var txt = await FileIO.ReadTextAsync(file);
            if (txt.StartsWith("{"))
            {
                packageOk = BCPackageImport.InitFromJson(newPackage, file.Name, txt);
            }
            else
            {
                // Must be an MD format
                //var lines = txt.Split(new char[] { '\r', '\n' });
                var lines = await FileIO.ReadLinesAsync(file);
                packageOk = BCPackageImport.InitFromMd(newPackage, file.Name, lines);
            }
            return packageOk;
        }

        enum MdState { ReadingPackagePreComment, ReadingPackageComment, ReadingProgramComment, ReadingProgramCode, ReadingProgramPostComment };
        public static bool InitFromMd (BCPackage package, string filename, IList<string> lines)
        {
            bool Retval = true;
            var state = MdState.ReadingPackagePreComment;
            var sb = new StringBuilder();
            BCProgram program = null;
            foreach (var line in lines)
            {
                switch (state)
                {
                    case MdState.ReadingPackagePreComment:
                        if (line.StartsWith("## "))
                        {
                            package.PackagePreComment = sb.ToString();
                            sb.Clear();
                            var name = line.Substring(3).Trim();
                            package.Name = name;
                            package.Filename = filename;
                            state = MdState.ReadingPackageComment;
                        }
                        else
                        {
                            if (line.Trim() != "" || sb.Length > 0)
                            {
                                sb.Append(line);
                                sb.Append("\r\n");
                            }
                        }
                        break;
                    case MdState.ReadingPackageComment:
                        if (line.StartsWith("### "))
                        {
                            package.Description = sb.ToString();
                            sb.Clear();
                            var name = line.Substring(4).Trim();
                            program = new BCProgram();
                            program.Name = name;
                            state = MdState.ReadingProgramComment;
                        }
                        else
                        {
                            if (line.Trim() != "" || sb.Length > 0)
                            {
                                sb.Append(line);
                                sb.Append("\r\n");
                            }
                        }
                        break;
                    case MdState.ReadingProgramComment:
                        if (line.StartsWith("```"))
                        {
                            program.Description = sb.ToString();
                            sb.Clear();
                            state = MdState.ReadingProgramCode;
                        }
                        else if (line.StartsWith("**Default Key**:"))
                        {
                            // the Key value is special to BC BASIC
                            var key = line.Substring(16);
                            program.KeyName = key.Trim();
                        }
                        else
                        {
                            if (line.Trim() != "" || sb.Length > 0)
                            {
                                sb.Append(line);
                                sb.Append("\r\n");
                            }
                        }
                        break;
                    case MdState.ReadingProgramCode:
                        if (line.StartsWith("```"))
                        {
                            program.Code = sb.ToString();
                            sb.Clear();
                            state = MdState.ReadingProgramPostComment;
                        }
                        else
                        {
                            if (line.Trim() != "" || sb.Length > 0)
                            {
                                sb.Append(line);
                                sb.Append("\r\n");
                            }
                        }
                        break;
                    case MdState.ReadingProgramPostComment:
                        if (line.StartsWith("### "))
                        {
                            program.ProgramPostComment = sb.ToString();
                            sb.Clear();
                            package.Programs.Add(program);

                            var name = line.Substring(4).Trim();
                            program = new BCProgram();
                            program.Name = name;
                            state = MdState.ReadingProgramComment;
                        }
                        else
                        {
                            if (line.Trim() != "" || sb.Length > 0)
                            {
                                sb.Append(line);
                                sb.Append("\r\n");
                            }
                        }
                        break;
                }
            }

            //
            // Now finish up!
            //
            switch (state)
            {
                case MdState.ReadingPackageComment:
                    // Legit but weird state -- there's a package comment,
                    // but no actual code.
                    package.Description = sb.ToString();
                    sb.Clear(); 
                    break;
                case MdState.ReadingPackagePreComment:
                    // Legit but very weird state.  The package doesn't
                    // even have a name?
                    package.PackagePreComment = sb.ToString();
                    sb.Clear();
                    break;
                case MdState.ReadingProgramCode:
                    // Not legit state.  Code should always be terminated.
                    // Do what we can!
                    program.Code = sb.ToString();
                    sb.Clear();
                    break;
                case MdState.ReadingProgramComment:
                    // Not legit state.  Code should always be present
                    // even if there's not much
                    program.Description = sb.ToString();
                    break;
                case MdState.ReadingProgramPostComment:
                    // Super common. often the sb will be totally blank.
                    program.ProgramPostComment = sb.ToString();
                    sb.Clear();
                    package.Programs.Add(program);
                    break;
            }
            return Retval;
        }
        public static bool InitFromJson(BCPackage package, string filename, string contents) // FromJson; the opposite of ToJson
        {
            bool Retval = true; // initialize as parse=ok
            try
            {
                JsonObject json = JsonObject.Parse(contents);
                package.Name = json.GetNamedString("Name", filename);
                package.Description = json.GetNamedString("Description", "");

                List<BCProgram> readInPrograms = new List<BCProgram>();

                var programs = json.GetNamedArray("Programs", null);
                if (programs == null)
                {
                    Retval = false;
                    package.Description = "The package did not contain any programs";
                    Retval = false;
                }
                else
                {
                    int nPackageFailure = 0;
                    string packageFailureDescription = "";
                    for (uint i = 0; i < programs.Count; i++)
                    {
                        var f = programs.GetObjectAt(i);
                        if (f != null)
                        {
                            var pname = f.GetNamedString("Name", "");
                            var pdescription = f.GetNamedString("Description", "");
                            var pkeyname = f.GetNamedString("KeyName", pname); // default is the Name
                            var fcode = f.GetNamedString("Code", "");
                            var codelines = f.GetNamedArray("CodeLines", null);
                            if (codelines != null)
                            {
                                var sb = new StringBuilder();
                                foreach (var item in codelines)
                                {
                                    if (item.ValueType == JsonValueType.String)
                                    {
                                        sb.Append(item.GetString());
                                        sb.Append("\r");
                                    }
                                }
                                var newFcode = sb.ToString().Trim();
                                fcode = newFcode;
                            }
                            if (pname != "" && pdescription != "" && fcode != "")
                            {
                                var program = new BCProgram() { Name = pname, Description = pdescription, KeyName = pkeyname, Code = fcode };
                                readInPrograms.Add(program);
                            }
                            else
                            {
                                bool failed = (pname == "" || pdescription == "" || fcode == "");
                                if (pname == "") pname = string.Format("Program {0}", i);
                                if (pdescription == "") pdescription = "Unable to read description";
                                if (fcode == "") fcode = string.Format("Unable to read code; JSON is {0}", f.Stringify());

                                var program = new BCProgram() { Name = pname, Description = pdescription, Code = fcode };
                                readInPrograms.Add(program);

                                if (failed)
                                {
                                    packageFailureDescription += String.Format("Error reading program {0}.  Name={1} Description={2} Code={3}\n\n", i, pname, pdescription, fcode.Substring(0, Math.Min(fcode.Length, 40)));
                                    nPackageFailure++;
                                }
                            }
                        }

                    }

                    //
                    // Sort the Programs list
                    //
                    package.Programs.Clear();
                    var sortedPrograms = readInPrograms.OrderBy(x => x.Name);
                    foreach (var p in sortedPrograms)
                    {
                        package.Programs.Add(p);
                    }


                    if (nPackageFailure > 0)
                    {
                        Retval = false;
                        package.Description = packageFailureDescription;
                    }
                }
                package.MustSave = false;
            }
            catch (Exception ex)
            {
                InitFailedPackage(package, filename, ex.Message);
                Retval = false;
            }
            return Retval;
        }

        public static void InitFailedPackage(BCPackage package, string filename, string message)
        {
            package.Programs.Clear();
            package.Name = "UNABLE TO LOAD";
            package.Description = string.Format("ERROR: unable to parse input file {0}.  Error {1}", filename, message);
        }
    }
}

