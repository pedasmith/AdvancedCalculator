using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BCBasic
{
    public class TestRunner
    {
        public class TestResult
        {
            public class SingleTestResult
            {
                public bool IsPass { get; set; }
                public string Result { get; set; }
            }
            private List<SingleTestResult> AllResults = new List<SingleTestResult>();

            // PASS: ✔ U+2714 Heavy Check Mark (Dingbats)
            // ERROR: ✘ U+2718 Heavy Ballot X (Dingbats)
            public void AddTestError(string text)
            {
                if (!text.EndsWith("\n"))
                {
                    text += "\nAND THE ERROR STRING HAS AN INVALID FORMAT!\n";
                }
                NTest++;
                NError += 1;
                AllResults.Add(new SingleTestResult() { IsPass = false, Result = text });
                //SB.Append(text);
            }

            public void AddTestPass(string text)
            {
                if (!text.EndsWith("\n"))
                {
                    text += "\nAND THE ERROR STRING HAS AN INVALID FORMAT!\n";
                }
                NTest++;
                AllResults.Add(new SingleTestResult() { IsPass = true, Result = text });
                //SB.Append(text);
            }
            public int NError { get; internal set; }
            public int NTest { get; internal set; }
            public override string ToString()
            {
                StringBuilder SB = new StringBuilder();
                AllResults.Sort((a, b) => { return a.IsPass.CompareTo(b.IsPass); });
                foreach (var result in AllResults)
                {
                    SB.Append(result.Result);
                }
                string text;
                if (NTest == 0)
                {
                    text = $"✘: ERROR: No tests were run";
                }
                else if (NError == 0)
                {
                    text = $"✔: Ran {NTest} tests\n{SB.ToString()}";
                }
                else
                {
                    text = $"{NError} ERRORS when running tests\n{SB.ToString()}";
                }
                return text;
            }
            public string Title()
            {
                if (NTest == 0)
                {
                    return $"✘: ERROR: No tests were run";
                }
                else if (NError == 0)
                {
                    return $"✔: {NTest} tests run";
                }
                return $"✘: ERROR: {NError} errors in {NTest} tests";
            }
        }

        public TestResult CurrTestResult { get; internal set; } = null;
        public string FunctionProgramAndPackageName { get; set; } = "";

        public void AddTestError(string text)
        {
            if (CurrTestResult == null) return;
            CurrTestResult.AddTestError(text);
        }

        public void AddTestPass(string text)
        {
            if (CurrTestResult == null) return;
            CurrTestResult.AddTestPass(text);
        }

        private async Task RunProgramTest(TestResult result, BCGlobalConnections externals, BCPackage package, BCProgram program)
        {
            //var functionCode = context.ExternalConnections.Package.GetProgram(name);
            CurrTestResult = result;
            List<String> potentialTestFunctions = null;
            var testFunctionsRun = new List<string>();
            BCBasicProgram compiledProgram = null;
            try
            {
                var compileResults = await BCBasicProgram.CompileAsync(externals, externals.DoConsole, program, program.Code); // will get a new context.
                if (compileResults.Errors.Count > 0)
                {
                    AddTestError($"✘: ERROR: unable to compile {program.Name} in {package.Name}\n    First error: {compileResults.Errors[0].ToString()}\n");
                    return;
                }
                compiledProgram = compileResults.Program;
                compiledProgram.SetupDataStatements(); // Make sure any READ and DATA statements work

                compiledProgram.BCContext.ProgramRunContext.CurrTestRunner = this;
                potentialTestFunctions = (compiledProgram.BCContext.Functions.Keys).Where(e => e.StartsWith("TEST")).ToList();
            }
            catch (Exception e)
            {
                AddTestError($"✘: ERROR: Exception {e.Message} Unable to set up program {program.Name} in {package.Name}\n");
                return;
            }

            foreach (var functionName in potentialTestFunctions)
            {
                string name = $"{functionName} in program {program.Name} and {package.Name}";
                FunctionProgramAndPackageName = name;

                var context = compiledProgram.BCContext;

                var function = context.Functions[functionName];
                context.ProgramRunContext.ResetReadIndex();
                context.ProgramRunContext.Tracing.ClearTraceCounts();
                context.ProgramRunContext.Tracing.Context = context;

                var startNTest = result.NTest;
                var testValue = await function.EvalAsync(context, new List<BCBasic.IExpression>()); // Test takes no parameters
                var endNTest = result.NTest;

                if (testValue.CurrentType == BCValue.ValueType.IsDouble)
                {
                    var d = testValue.AsDouble;
                    if (Double.IsNaN(d))
                    {
                        AddTestError($"✘: ERROR IN TEST: returned NaN: {name}\n");
                    }
                    else if (d < 0)
                    {
                        AddTestError($"✘: ERROR IN TEST: returned negative number {d}: {name}\n");
                    }
                    else if (d > 0)
                    {
                        AddTestError($"✘: {d} ERRORS: {name}\n");
                    }
                    else
                    {
                        AddTestPass($"✔: Success with {name}\n");
                    }
                }
                else if (testValue.CurrentType == BCValue.ValueType.IsNoValue && endNTest > startNTest)
                {
                    // The TEST function didn't return the number of successes but 
                    // it must have called the ASSERT function.  That counts as running
                    // tests OK.
                    ;
                }
                else
                {
                    AddTestError($"✘: ERROR IN TEST: returned non-numeric value: {name}\n");
                }
            }
            compiledProgram.BCContext.ProgramRunContext.CurrTestRunner = null;
            CurrTestResult = null;
        }


        public async Task<TestResult> RunLibraryTestsAsync(BCGlobalConnections externals, BCLibrary library, CancellationToken ct)
        {
            var result = new TestResult();
            var startingCursor = Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor;
            Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);
            foreach (var package in library.Packages)
            {
                var startNTest = result.NTest;
                foreach (var program in package.Programs)
                {
                    await RunProgramTest(result, externals, package, program);
                }
                var endNTest = result.NTest;
                if (endNTest == startNTest)
                {
                    // This package has no tests; that's automatically wrong.
                    result.AddTestError($"✘: ERROR: Package {package.Name} has no tests\n");
                }
            }
            Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = startingCursor;
            return result;
        }


        public async Task<TestResult> RunPackageTestsAsync(BCGlobalConnections externals, BCLibrary library, CancellationToken ct, BCPackage package)
        {
            var result = new TestResult();

            foreach (var program in package.Programs)
            {
                await RunProgramTest(result, externals, package, program);
            }
            return result;
        }


        public async Task<TestResult> RunProgramTestsAsync (BCGlobalConnections externals, BCLibrary library, CancellationToken ct, BCPackage package, BCProgram program)
        {
            var result = new TestResult();

            await RunProgramTest(result, externals, package, program);
            return result;
        }
    }
}