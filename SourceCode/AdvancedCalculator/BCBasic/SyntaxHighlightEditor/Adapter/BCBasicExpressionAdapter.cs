using BCBasic;
using System.Collections.Generic;

namespace Edit
{
    // Converts the Language ParsedResult into BCBasic FullStatement
    public class BCBasicExpressionAdapter
    {
        public IList<IExpression> ConvertList(IList<ParsedExpression> list)
        {
            var Retval = new List<IExpression>();
            foreach (var item in list)
            {
                Retval.Add(Convert(item));
            }
            return Retval;
        }
        /*
    All of the implementations of an IExpression

    Status	Code	File	Line	Column	Project
	class NumericFunction : IExpression	C:\bin\My Small Utils\Shipwreck\AdvancedCalculator\BCBasic\BCBasic.Shared\BCBasic.cs	2255	11	BluetoothCalculatorUniversalVS2017
	class NumericConstant : IExpression	C:\bin\My Small Utils\Shipwreck\AdvancedCalculator\BCBasic\BCBasic.Shared\BCBasic.cs	1851	11	BluetoothCalculatorUniversalVS2017
	class VariableArrayValue : IExpression	C:\bin\My Small Utils\Shipwreck\AdvancedCalculator\BCBasic\BCBasic.Shared\BCBasic.cs	1971	11	BluetoothCalculatorUniversalVS2017
	class VariableValue : IExpression // also supports PI and RND	C:\bin\My Small Utils\Shipwreck\AdvancedCalculator\BCBasic\BCBasic.Shared\BCBasic.cs	1916	11	BluetoothCalculatorUniversalVS2017
	class NumericSingleExpression : IExpression	C:\bin\My Small Utils\Shipwreck\AdvancedCalculator\BCBasic\BCBasic.Shared\BCBasic.cs	2193	11	BluetoothCalculatorUniversalVS2017
	class StringConstant : IExpression	C:\bin\My Small Utils\Shipwreck\AdvancedCalculator\BCBasic\BCBasic.Shared\BCBasic.cs	1900	11	BluetoothCalculatorUniversalVS2017
	public class InfixExpression : IExpression	C:\bin\My Small Utils\Shipwreck\AdvancedCalculator\BCBasic\BCBasic.Shared\BCBasic.cs	2007	18	BluetoothCalculatorUniversalVS2017
	class ObjectConstant : IExpression	C:\bin\My Small Utils\Shipwreck\AdvancedCalculator\BCBasic\BCBasic.Shared\BCBasic.cs	1883	11	BluetoothCalculatorUniversalVS2017
	class NumericInput : IExpression	C:\bin\My Small Utils\Shipwreck\AdvancedCalculator\BCBasic\BCBasic.Shared\BCBasic.cs	2222	11	BluetoothCalculatorUniversalVS2017

         * 
         */
        public IExpression Convert (ParsedExpression value)
        {
            if (value == null) return null;
            else if (value is ParsedExpressionArray)
            {
                var expArray = value as ParsedExpressionArray;
                // As of 2018-06-06, the list must be exactly 1 item long.
                // This requirement is handled by the VariableArrayValue constructor.
                var indexList = new List<IExpression>();
                foreach (var index in expArray.Indexes.List)
                {
                    indexList.Add(Convert(index));
                }
                return new VariableArrayValue(expArray.Identifier.Text, indexList);
            }
            else if (value is ParsedExpressionBinary)
            {
                var expBinary = value as ParsedExpressionBinary;
                var left = Convert (expBinary.Left);
                var op = expBinary.Op.Text;
                var right = Convert(expBinary.Right);
                return new InfixExpression(left, op, right);
            }
            else if (value is ParsedExpressionColorName)
            {
                var expColor = value as ParsedExpressionColorName;
                var color = expColor.ColorName.Text;
                return new StringConstant(color);
            }
            else if (value is ParsedExpressionFunction)
            {
                var expFunction = value as ParsedExpressionFunction;
                var name = expFunction.Identifier.Text;
                var fnc = new NumericFunction(name);
                foreach (var item in expFunction.Indexes.List)
                {
                    fnc.ArgList.Add(Convert(item));
                }
                return fnc;
            }
            else if (value is ParsedExpressionIdentifier)
            {
                var expIdentifier = value as ParsedExpressionIdentifier;
                var name = expIdentifier.Identifier.Text;
                return new VariableValue(name);
            }
            else if (value is ParsedExpressionInput)
            {
                var expInput = value as ParsedExpressionInput;
                var prompt = Convert(expInput.Prompt);
                var defaultValue = Convert(expInput.Default);
                return new NumericInput(prompt, defaultValue);
            }
            else if (value is ParsedExpressionLiteralNumeric)
            {
                var expLiteralNumeric = value as ParsedExpressionLiteralNumeric;
                if (expLiteralNumeric.Literal.Text == "∞")
                {
                    return new NumericConstant(expLiteralNumeric.Literal.Text);
                }
                return new NumericConstant(expLiteralNumeric.Literal.Value);
            }
            else if (value is ParsedExpressionLiteralString)
            {
                var expLiteralString= value as ParsedExpressionLiteralString;
                return new StringConstant(expLiteralString.Literal.StringValue);
            }
            else if (value is ParsedExpressionParenthesis)
            {
                var expParen = value as ParsedExpressionParenthesis;
                return Convert(expParen.Expression);
            }
            else if (value is ParsedExpressionPrefix)
            {
                var expPrefix = value as ParsedExpressionPrefix;
                var op = expPrefix.Op.Text;
                var right = Convert(expPrefix.Right);
                if (expPrefix.Op.OperatorType == Lexer.OperatorType.Sinclair)
                {
                    var f = new NumericFunction(op);
                    f.ArgList.Add(right);
                    return f;
                }
                else
                {
                    return new NumericSingleExpression(op, right);
                }
            }
            else if (value is ParsedExpressionSuffix)
            {
                var expSuffix = value as ParsedExpressionSuffix;
                var left = Convert(expSuffix.Left);
                double power = 2.0; // POWER -> @"(²|³|⁴)";
                switch (expSuffix.Op.Text)
                {
                    case "²": power = 2.0; break;
                    case "³": power = 3.0; break;
                    case "⁴": power = 4.0; break;
                }
                return new BCBasic.InfixExpression(left, "**", new BCBasic.NumericConstant(power));
            }
            System.Diagnostics.Debug.WriteLine($"ERROR: ExpressionAdapter: Unknown expression {value.Cmd}");

            return null;
        }
    }
}