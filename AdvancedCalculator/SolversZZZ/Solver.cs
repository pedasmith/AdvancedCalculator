using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using System.Diagnostics;


namespace EquationSolver
{
    public class Solver
    {
        public Solver()
        {
            Equations = new List<Equation>();
        }

        public class Equation
        {
            public static string ListToString(List<string> list)
            {
                String Retval = "";
                foreach (string str in list)
                {
                    if (Retval != "") Retval += ",";
                    Retval += str;
                }
                return Retval;
            }
            public delegate double EquationFunctionDouble();
            public delegate string EquationFunctionString();

            public Equation(string variable, List<string> rhs, EquationFunctionDouble function)
            {
                EqName = variable + "<--" + ListToString(rhs);
                Variable = variable;
                RHSNames = rhs;
                FunctionDouble = function;
            }
            public Equation(string variable, string rhs, EquationFunctionDouble function)
            {
                EqName = variable + "<--" + variable;
                Variable = variable;
                RHSNames = new List<string>() { rhs };
                FunctionDouble = function;
            }
            public Equation(string variable, List<string> rhs, EquationFunctionString function)
            {
                EqName = variable + "<--" + ListToString(rhs);
                Variable = variable;
                RHSNames = rhs;
                FunctionString = function;
            }
            public Equation(string variable, string rhs, EquationFunctionString function)
            {
                EqName = variable + "<--" + variable;
                Variable = variable;
                RHSNames = new List<string>() { rhs };
                FunctionString = function;
            }


            public string EqName { get; set; }
            public string Variable { get; set; }
            public List<string> RHSNames { get; set; }
            public EquationFunctionDouble FunctionDouble;
            public EquationFunctionString FunctionString;


            public bool DependsOn(string Name)
            {
                bool Retval = RHSNames.Contains(Name);
                return Retval;
            }
        }
        public List<Equation> Equations { get; set; }

        
        class EquivLists
        {
            private int MaxValue = 0;
            private Dictionary<String, int> Map = new Dictionary<string,int>();
            public void Add(string VariableA, string VariableB)
            {
                if (Map.ContainsKey(VariableA))
                {
                    int ValA = Map[VariableA];
                    if (Map.ContainsKey(VariableB))
                    {
                        // Need to update everyone with B's old value
                        int ValB = Map[VariableB];
                        List<String> ToBeChanged = new List<String>();
                        foreach (string Key in Map.Keys)
                        {
                            if (Map[Key] == ValB)
                            {
                                //Map[Key] = ValA;
                                ToBeChanged.Add(Key);
                            }
                        }
                        foreach (string Key in ToBeChanged)
                        {
                            Map[Key] = ValB;
                        }
                    }
                    else
                    {
                        Map[VariableB] = ValA;
                    }
                }
                else if (Map.ContainsKey(VariableB))
                {
                    int ValB = Map[VariableB];
                    Map[VariableA] = ValB; // We know A was not already in the Map.
                }
                else
                {
                    Map[VariableA] = MaxValue;
                    Map[VariableB] = MaxValue;
                    MaxValue++;
                }
            }
#if NEVER_EVER_DEFINED
            private void ZZZAdd (string VariableA, string VariableB)
            {
                List<String> ListA = ListWith(VariableA);
                List<String> ListB = ListWith(VariableB);
                if (ListA == null && ListB == null)
                {
                    List<String> NewList = new List<String> { VariableA, VariableB };
                    Data.Add(NewList);
                }
                else if (ListA != null && ListB != null)
                {
                    // Same list or different list?
                    if (ListA == ListB)
                    {
                        // This pair already exists; do not have to do anything.
                    }
                    else
                    {
                        // These two lists must be concatinated.  Put everything into ListA.
                        foreach (string Name in ListB)
                        {
                            if (ListA.Contains(Name))
                            {
                                // This is an error -- every value must be in only one list.
                                Trace("ERROR: lists are not correct for {0}", Name);
                            }
                            else
                            {
                                ListA.Add(Name);
                            }
                        }
                    }
                }
                else if (ListA != null)
                {
                    // VariableA is in a list; VariableB should be in the same list
                    ListA.Add(VariableB);
                }
                else
                {
                    ListB.Add(VariableA);
                }
            }
#endif
            private int NCall = 0;
            private int NCheck = 0;
            private int NMatch = 0;
            public bool AreEquiv(string VariableA, string VariableB)
            {
                bool Retval = false;
                NCall++;
                if (Map.ContainsKey(VariableA) && Map.ContainsKey(VariableB))
                {
                    NCheck++;
                    if (Map[VariableA] == Map[VariableB])
                    {
                        NMatch++;
                        Retval = true;
                    }
                }
                return Retval;
            }

            public int GetCachedEquiv(string Name)
            {
                int Retval = Map.ContainsKey(Name) ? Map[Name] : -1;
                return Retval;
            }
            public bool AreEquiv(int CachedEquiv, string VariableB)
            {
                bool Retval = (CachedEquiv == -1) ? false : (GetCachedEquiv(VariableB) == CachedEquiv);
                return Retval;
            }
#if NEVER_EVER_DEFINED
            private bool ZZZAreEquiv(string VariableA, string VariableB)
            {
                List<String> ListA = ListWith(VariableA);
                List<String> ListB = ListWith(VariableB);
                bool Retval = ListA == ListB && ListA != null;
                return Retval;
            }
#endif
            List<List<String>> Data = new List<List<String>>();

            private List<String> ListWith(string Variable)
            {
                foreach (var List in Data)
                {
                    if (List.Contains(Variable))
                    {
                        return List;
                    }
                }
                return null;
            }
        }

        private void Trace(string format, params object[] args)
        {
            //Tracing is off on released builds
            //string str = String.Format(format, args);
            //Debug.WriteLine(str);
            //AdvancedCalculator.Log.WriteWithTime(str + "\r\n");
        }
        EquivLists VariableEquivLists = new EquivLists();
        public void InitEquivLists ()
        {
            foreach (var eq in Equations)
            {
                if (eq.RHSNames.Count != 1)
                {
                    continue;
                }
                // a <-- b (and I assume that later on b <-- a)
                VariableEquivLists.Add(eq.Variable, eq.RHSNames[0]);
            }
        }
        public int GetCachedEquiv(String VariableA)
        {
            return VariableEquivLists.GetCachedEquiv(VariableA);
        }
        public bool AreEquiv(int CachedEquiv, string VariableB)
        {
            bool Retval = VariableEquivLists.AreEquiv(CachedEquiv, VariableB);
            return Retval;
        }
        public bool AreEquiv(String VariableA, string VariableB)
        {
            bool Retval = VariableEquivLists.AreEquiv(VariableA, VariableB);
            return Retval;
        }
#if NEVER_EVER_DEFINED
        // Two values are equivilent is one is set strictly by the other
        // e.g. packetsize and packetsizeinbits
        public bool AreEquiv(string a, string b)
        {
            bool Retval = OnlySetBy(a, b) || OnlySetBy(b, a);
            return Retval;
        }
        public bool OnlySetBy(string a, string b)
        {
            // Is variable 'a' only ever set by variable b with no other variables?
            foreach (var eq in Equations)
            {
                if (eq.Variable != a) continue;
                if (eq.RHSNames.Count != 1) return false;
                if (eq.RHSNames[0] != b) return false;
            }
            return true;
        }
#endif
        public int IsRecent(string a)
        {
            int Retval = RecentlySet.IndexOf (a);
            return Retval;
        }
        // returns either 1 or 2
        //public static int MaxPriority = 2;
        public int Priority(string a)
        {
            int Retval = IsRecent(a);
            int CachedEquiv = GetCachedEquiv(a);
            for (int i=Retval+1; i<RecentlySet.Count; i++)
            {
                string recent = RecentlySet[i];
                if (AreEquiv(CachedEquiv, recent)) Retval = i;
            }
            return Retval;
        }

        public List<String> RecentlySet = new List<String>();
        private bool inSolve = false;
        public void SetRecent(string Changed)
        {
            if (Changed == "Principle")
            {
                        Changed = "Principle";
            }
            RecentlySet.Remove(Changed);
            RecentlySet.Add(Changed); // Most recent is at the end.
            Trace("Solve: Adding {0} to RecentlySet {1}", Changed, Equation.ListToString(RecentlySet));
        }

        public void SetByName(string Changed, double NewVal)
        {
#if DESKTOP
            this.GetType().GetProperty(Changed).SetValue(this, NewVal, null);
#else
            this.GetType().GetRuntimeProperty(Changed).SetValue(this, NewVal);
#endif
        }

        public double GetByNameDouble(string Changed)
        {
#if DESKTOP
            return (double)this.GetType().GetProperty(Changed).GetValue(this, null);
#else
            return (double)this.GetType().GetRuntimeProperty(Changed).GetValue(this);
#endif
        }

        public void SetByName(string Changed, string NewVal)
        {
#if DESKTOP
            this.GetType().GetProperty(Changed).SetValue(this, NewVal, null);
#else
            InsertPropertyCache(Changed);
            PropertyCache[Changed].SetValue(this, NewVal);
#endif
        }

        private Dictionary<String, PropertyInfo> PropertyCache = new Dictionary<string, PropertyInfo>();
        private void InsertPropertyCache(String Changed)
        {
            if (!PropertyCache.ContainsKey(Changed))
            {
#if DESKTOP
                PropertyCache[Changed] = this.GetType().GetProperty(Changed);
#else
                PropertyCache[Changed] = this.GetType().GetRuntimeProperty(Changed);
#endif
            }
        }

        public string GetByNameString(string Changed)
        {
#if DESKTOP
            return (string)this.GetType().GetProperty(Changed).GetValue(this, null);
#else
            InsertPropertyCache(Changed);
            return (string)PropertyCache[Changed].GetValue(this);
#endif
        }

        public bool NameIsDouble(string Name)
        {
            foreach (Equation eq in Equations)
            {
                if (eq.Variable == Name)
                {
                    bool RetVal = eq.FunctionDouble != null;
                    return RetVal;
                }
            }

            foreach (Equation eq in Equations)
            {
                if (eq.RHSNames.Contains(Name) && eq.FunctionDouble != null)
                {
                    return true;
                }
            }
            return false;
        }

        // Lists the currently known-to-be-wrong values
        public List<string> Unsolved()
        {
            List<string> Retval = new List<string>();
            foreach (var eq in Equations)
            {
                if (eq.FunctionDouble != null)
                {
                    double NewVal = eq.FunctionDouble();
                    if (Double.IsNaN(NewVal))
                    {
                        // Possibly should do something....
                        Trace("Unsolved: {0} returns NaN", eq.EqName);
                    }
                    else
                    {
                        //double OldVal = GetByNameDouble(eq.Variable);
                        //double DeltaRange = Math.Abs(NewVal == 0 ? OldVal : (OldVal - NewVal) / NewVal);
                        //if (Double.IsNaN(DeltaRange) || DeltaRange >= 0.0001)
                        double OldVal = GetByNameDouble(eq.Variable);
                        if (OutsideDelta (NewVal, OldVal))
                        {
                            Trace ("Unsolved: Outside Delta: eq {3}={0} was {1} should be {2}", eq.EqName, OldVal, NewVal, eq.Variable);
                            // They are different; do something
                            Retval.Add(eq.Variable);
                        }
                    }
                }
            }
            return Retval;
        }

        public int Solve(string Changed)
        {
            if (inSolve)
            {
                return -1;
            }
            Trace ("Solve: solve for {0}", Changed);
            int Retval = 0;
            for (int i = 0; i < 5; i++)
            {
                Trace ("Solve: pass {0} out of up to 5", i);
                Retval = SolveOnePass(Changed);
                List<string> incorrect = Unsolved();
                if (incorrect.Count == 0)
                {
                    Trace("Solve: nothing is incorrect; return {0}", Retval);
                    return Retval;
                }
                Trace("Solve: {0}: unsolved list is {1}", Changed, Equation.ListToString(incorrect));

                // Pick the lowest priority (>= 0) value to remove
                int lowestPriority = Int32.MaxValue;
                foreach (var item in incorrect)
                {
                    int priority = Priority(item);
                    if (priority >= 0 && priority < lowestPriority) lowestPriority = priority;
                }
                if (lowestPriority >= 0 && lowestPriority < RecentlySet.Count)
                {
                    Trace("Solve: {0} need more calculations; lowest priority is {1}", Changed, RecentlySet[lowestPriority]);
                    RecentlySet.RemoveAt(lowestPriority);
                }

            }
            Trace("Solve: END solve for {0}", Changed);
            Trace("");
            return Retval;
        }

        public double SolveFor(string Modify, string Match, double MinStart, double MaxStart)
        {
            // Find the right equation
            double initValue = GetByNameDouble(Modify);
            Equation solveEq = null;
            foreach (var eq in Equations)
            {
                if (eq.Variable == Match && eq.FunctionDouble != null)
                {
                    solveEq = eq;
                    break;
                }
            }
            if (solveEq == null)
            {
                return initValue;
            }
            double targetVal = this.GetByNameDouble(Match);
            double lowVal = double.NaN;
            double highVal = double.NaN;
            solveEq.FunctionDouble();

            double trialVal;
            double resultVal;

            bool oldInSolve = inSolve;
            inSolve = true;

            trialVal = MinStart;
            this.SetByName(Modify, trialVal);
            resultVal = solveEq.FunctionDouble();
            if (resultVal < targetVal) lowVal = trialVal; else highVal = trialVal;

            trialVal = MaxStart;
            this.SetByName(Modify, trialVal);
            resultVal = solveEq.FunctionDouble();
            if (resultVal < targetVal) lowVal = trialVal; else highVal = trialVal;

            if (Double.IsNaN (lowVal) || Double.IsNaN(highVal))
            {
                //Trace("SolveFor: {3}:  Unable to solve for {0}-->{4}; lowval {1} highVal {2}", Modify, lowVal, highVal, Modify, targetVal);
                this.SetByName(Modify, initValue);
                inSolve = false;
                return initValue;
            }

            for (int i = 0; i < 70; i++)
            {
                trialVal = (lowVal + highVal) / 2.0;
                this.SetByName(Modify, trialVal);
                resultVal = solveEq.FunctionDouble();
                if (resultVal < targetVal) lowVal = trialVal; else highVal = trialVal;
                //Trace("SolveFor: {4}: Index {0}  trialVal {3} resultVal is {1} targetVal {2}", i, resultVal, targetVal, trialVal, Modify);
            }
            //Trace("SolveFor: {0}: trialVal {1} resultVal is {2} targetVal {3}", Modify, trialVal, resultVal, targetVal);
            this.SetByName(Modify, initValue);
            inSolve = oldInSolve;
            return trialVal;
        }


        private int SolveOnePass(string Changed)
        {
            int Retval = 0;
            if (inSolve)
            {
                //Trace("Solver: return right away (in solver)");
                return -1;
            }
            inSolve = true;
            try
            {
                SetRecent(Changed);

                List<string> Updated = new List<string>() { Changed }; // don't update anything more than once
                List<string> Handled = new List<string>();
                List<string> ToBeHandled = new List<string>() { Changed };

                while (ToBeHandled.Count > 0)
                {
                    string BeingHandled = ToBeHandled[0];
                    ToBeHandled.Remove(BeingHandled);
                    Handled.Add(BeingHandled);
                    string type;
                    type = "NotRecentlySet";
                    for (int p = -1; p < RecentlySet.Count; p++)
                    {
                        type = (p == -1) ? "NotRecentlySet" : "Recent(" + RecentlySet[p] + ")";
                        int CachedEquiv = p == -1 ? -2 : GetCachedEquiv(RecentlySet[p]);
                        Trace("{0}: handling equations for variable {1}, cachedEquiv {2}", type, BeingHandled, CachedEquiv);
                        foreach (Equation eq in Equations)
                        {
                            if (p == -1)
                            {
                                int priority = Priority(eq.Variable);
                                if (p != priority) continue;
                            }
                            else
                            {
                                bool equiv = AreEquiv(CachedEquiv, eq.Variable);
                                // expensive: Trace("equiv: variable {0}={1} cachedEquiv {2}; true=continue false=break", eq.Variable, GetCachedEquiv(eq.Variable), equiv);
                                if (!equiv) continue;
                            }

                            if (eq.DependsOn(BeingHandled) && !Updated.Contains(eq.Variable))
                            {
                                Retval++;
                                bool changed = SolveEquation(eq, type, BeingHandled);
                                Updated.Add(eq.Variable);
                                if (changed)
                                {
                                    ToBeHandled.Add(eq.Variable);
                                }
                            }
                        }
                    }
                }
            }
            catch (FormatException)
            {
                throw;
            }
            finally
            {
                inSolve = false;
                Trace("Solver: returning {0}", Retval);
            }

            // Why the waiting?  Because the UI elements will be lazily updated,
            // and when they change, they will cause a callback to be triggered.
            //
            /*
            var timer = new Windows.UI.Xaml.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 90);
            timer.Tick += (s, ea) =>
            {
                inSolve = false;
                timer.Stop();
            };
            timer.Start();
            //inSolve = false;
            Trace("Solver: returning {0}", Retval);
             */
            return Retval;
        }

        private double Delta(double NewVal, double OldVal)
        {
            double Retval =  Math.Abs(NewVal == 0 ? OldVal : (OldVal - NewVal) / NewVal);
            return Retval;
        }
        protected bool OutsideDelta(double NewVal, double OldVal)
        {
            double delta = Delta(NewVal, OldVal);
            bool Retval = (Double.IsNaN(delta) || delta >= 0.0001);
            return Retval;
        }

        // Solves a single equation and saves the answer into the right property.
        private bool SolveEquation (Equation eq, string type, string BeingHandled)
        {
            bool Retval = false; // didn't actually make a change
            bool isDouble = eq.FunctionDouble != null;
            if (isDouble)
            {
                double NewVal = eq.FunctionDouble();
                if (Double.IsNaN(NewVal))
                {
                    Trace("{0}: Is Nan; no changes: {1}: Set {2} to {3} - handling {4}", type, eq.EqName, eq.Variable, NewVal, BeingHandled);
                }
                else
                {
                    //double OldVal = GetByNameDouble (eq.Variable);
                    //double DeltaRange = Delta(NewVal, OldVal);
                    double OldVal = GetByNameDouble(eq.Variable);
                    if (OutsideDelta (NewVal, OldVal))
                    {
                        Trace("{0}: {1}: Set {2} to {3} from {4} -  handling {5}", type, eq.EqName, eq.Variable, NewVal, OldVal, BeingHandled);
                        SetByName(eq.Variable, NewVal);
                        Retval = true;
                    }
                }
            }
            else // must be string
            {
                string NewVal = eq.FunctionString();
                string OldVal = GetByNameString(eq.Variable);
                if (NewVal != OldVal)
                {
                    Trace("{0}: {1}: Set {2} to {3} from {4} - handling {4}", type, eq.EqName, eq.Variable, NewVal, OldVal, BeingHandled);
                    SetByName(eq.Variable, NewVal);
                    Retval = true;
                }
            }
            return Retval;
        }
    } // end of class


}
