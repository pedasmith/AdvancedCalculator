using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace BCBasic
{
    class BCBasicMathFunctions
    {
        //
        // Functions in BCBasic that don't exist in C# Math: DtoR, DMStoR, IsNan, Log2, RtoD
        // Added 2017-07-29: fft and invfft
        // Added 2018-09-09: BitAnd BitOr BitNot
        // Handy function 2018-10-21: ApproxEqual
        public static bool ApproxEqual(double a, double b, double sigfig=5.0)
        {
            // Start with making the problem simpler: a is always > b, and both are always positive. 

            bool sameSign = (a <= 0 && b <= 0) || (a >= 0 && b >= 0);
            if (a < b)
            {
                var c = a;
                a = b;
                b = c;
            }
            var delta = a - b;
            var ratio = delta / a; // always compare against the larger.  Ratio is small when the numbers are closer together.
                                   // Note that ratio can never be < 0 because a is always >= b, so it's positive, and so is a.
            var log = ratio > 0 ? Math.Log10(ratio) : Double.NegativeInfinity; // So if I want to compare against 5 sig. figs, just compare log against 5
            var isApproximatelyEqualTo = (sameSign && (log < -sigfig));
            return isApproximatelyEqualTo;
        }

        public static async Task<bool> BitAndAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            var left = (ulong)(await ArgList[0].EvalAsync(context)).AsDouble;
            var right = (ulong)(await ArgList[1].EvalAsync(context)).AsDouble;
            Retval.AsDouble = (double)(left & right);
            return true;
        }

        public static async Task<bool> BitNotAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            var left = (ulong)(await ArgList[0].EvalAsync(context)).AsDouble;
            Retval.AsDouble = (double)(~left);
            return true;
        }

        public static async Task<bool> BitOrAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            var left = (ulong)(await ArgList[0].EvalAsync(context)).AsDouble;
            var right = (ulong)(await ArgList[1].EvalAsync(context)).AsDouble;
            Retval.AsDouble = (double)(left | right);
            return true;
        }

        public static async Task<bool> BitXorAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            var left = (ulong)(await ArgList[0].EvalAsync(context)).AsDouble;
            var right = (ulong)(await ArgList[1].EvalAsync(context)).AsDouble;
            Retval.AsDouble = (double)(left ^ right);
            return true;
        }

        public static async Task<bool> DtoRAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = ((await ArgList[0].EvalAsync(context)).AsDouble * 2 * Math.PI) / 360.0;
            return true;
        }

        public static async Task<bool> DMStoRAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            var d = (await ArgList[0].EvalAsync(context)).AsDouble;
            var m = ArgList.Count < 2 ? 0.0 : (await ArgList[1].EvalAsync(context)).AsDouble;
            var s = ArgList.Count < 3 ? 0.0 : (await ArgList[2].EvalAsync(context)).AsDouble;
            var degrees = d + (m / 60.0) + (s / 3600.0);
            Retval.AsDouble = (degrees * 2 * Math.PI) / 360.0;
            return true;
        }

        // Rule: round
        public static async Task<bool> FactorialAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            double dval = (await ArgList[0].EvalAsync(context)).AsDouble;
            if (Math.Truncate(dval) != dval || dval < 0)
            {
                Retval.AsDouble = Double.NaN;
            }
            else
            {
                double fact = 1.0;
                for (double n = dval; n > 0; n--)
                {
                    fact *= n;
                }
                Retval.AsDouble = fact;
            }
            return true;
        }

        public static async Task<bool> FftAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            BCValueList input = (await ArgList[0].EvalAsync(context)).AsArray;
            var floats = input.AsFloatArray();
            var options = MathNet.Numerics.IntegralTransforms.FourierOptions.Default;
            MathNet.Numerics.IntegralTransforms.Fourier.ForwardReal(floats, floats.Length - 2, options);
            Retval.AsObject = new BCValueList(floats);
            return true;
        }

        public static async Task<bool> InverseFftAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            BCValueList input = (await ArgList[0].EvalAsync(context)).AsArray;
            var floats = input.AsFloatArray();
            var options = MathNet.Numerics.IntegralTransforms.FourierOptions.Default;
            MathNet.Numerics.IntegralTransforms.Fourier.InverseReal(floats, floats.Length - 2, options);
            Retval.AsObject = new BCValueList(floats);
            return true;
        }

        public static async Task<bool> FracAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            double a = (await ArgList[0].EvalAsync(context)).AsDouble;
            Retval.AsDouble = a - Math.Floor(a); // Graham, Knuth & Patashnik 1992 -- note that Frac(1.3) is .3 but Frac(-1.3) is .7.
            return true;
        }


        // Returns true if ANY of the items are NaN. If the list is empty, return false.  If the list only has
        // strings, convert to double (AsDouble) and use those values
        // IsNan ("abc") --> true
        // Not Set values are ALSO NaN value
        public static async Task<bool> IsNaNAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = 0; // false;
            foreach (var arg in ArgList)
            {
                var val = await arg.EvalAsync(context);
                var dval = val.AsDouble;
                if (double.IsNaN(dval) || val.CurrentType == BCValue.ValueType.IsNoValue) // No value == NaN
                {
                    Retval.AsDouble = 1.0;
                    break; // no point in continuing.
                }
            }
            return true;
        }


        public static async Task<bool> Log2Async(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Log((await ArgList[0].EvalAsync(context)).AsDouble, 2.0);
            return true;
        }


        public static async Task<bool> ModAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            double a = (await ArgList[0].EvalAsync(context)).AsDouble;
            double n = (await ArgList[1].EvalAsync(context)).AsDouble;
            Retval.AsDouble = a % n;
            return true;
        }


        public static async Task<bool> RndAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            var cmd = 1.0;
            if (ArgList.Count == 1)
            {
                cmd = (await ArgList[0].EvalAsync(context)).AsDouble;
            }
            if (cmd > 0.0)
            {
                Retval.AsDouble = BCRunContext.StaticRnd.NextDouble();
            }
            else if (cmd == 0.0)
            {
                BCRunContext.StaticRnd = new Random();
                Retval.AsDouble = 0.0;
            }
            else // < 0.0 or nan
            {
                var seed = (int)Math.Abs(cmd);
                BCRunContext.StaticRnd = new Random(seed);
                Retval.AsDouble = seed;
            }
            return true;
        }


        public static async Task<bool> RtoDAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = ((await ArgList[0].EvalAsync(context)).AsDouble * 360) / (2 * Math.PI);
            return true;
        }



        //
        // Duplicate the C# Math.___ functions
        // Except for BigMul, DivRem, IEEERemainder

        public static async Task<bool> AbsAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Abs((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> AcosAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Acos((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> AsinAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Asin((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> AtanAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Atan((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> Atan2Async(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Atan2((await ArgList[0].EvalAsync(context)).AsDouble, (await ArgList[1].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> CeilingAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Ceiling((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> CosAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Cos((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> CoshAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Cosh((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> ExpAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Exp((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> FloorAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Floor((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }


        public static async Task<bool> LogAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (ArgList.Count)
            { 
                case 1:
                    Retval.AsDouble = Math.Log((await ArgList[0].EvalAsync(context)).AsDouble);
                    break;
                case 2:
                    Retval.AsDouble = Math.Log((await ArgList[0].EvalAsync(context)).AsDouble, (await ArgList[1].EvalAsync(context)).AsDouble);
                    break;
            }
            return true;
        }

        public static async Task<bool> Log10Async(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Log10((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> MaxAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            double value = (await ArgList[0].EvalAsync(context)).AsDouble;
            for (int i = 1; i < ArgList.Count; i++)
            {
                double potential = (await ArgList[i].EvalAsync(context)).AsDouble;
                value = Math.Max(value, potential);
            }
            Retval.AsDouble = value;
            return true;
        }

        public static async Task<bool> MinAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            double value = (await ArgList[0].EvalAsync(context)).AsDouble;
            for (int i = 1; i < ArgList.Count; i++)
            {
                double potential = (await ArgList[i].EvalAsync(context)).AsDouble;
                value = Math.Min(value, potential);
            }
            Retval.AsDouble = value;
            return true;
        }



        public static async Task<bool> PowAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Pow((await ArgList[0].EvalAsync(context)).AsDouble, (await ArgList[1].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> RoundAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            var decimalPlaces = (ArgList.Count >= 2) ? (await ArgList[1].EvalAsync(context)).AsDouble : 0.0;
            var mult = Math.Pow(10, decimalPlaces);
            var startingValue = (await ArgList[0].EvalAsync(context)).AsDouble;
            startingValue = startingValue * mult;
            var rounded  = Math.Round(startingValue);
            Retval.AsDouble = rounded / mult;
            return true;
        }

        public static async Task<bool> SigmaAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            double x = (await ArgList[0].EvalAsync(context)).AsDouble;
            double sigma = 1.0 / (1.0 + Math.Pow(Math.E, -x));
            Retval.AsDouble = sigma;
            return true;
        }

        public static async Task<bool> SignAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Sign((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> SinAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Sin((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> SinhAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Sinh((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> SqrtAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Sqrt((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> TanAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Tan((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> TanhAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Tanh((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }

        public static async Task<bool> TruncateAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.AsDouble = Math.Truncate((await ArgList[0].EvalAsync(context)).AsDouble);
            return true;
        }
    }
}