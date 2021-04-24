using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BellTest
{
    class FormulaParser
    {
        enum Condition_ { RANDOMTRUE, GREATER, LESS, GREATEREQUAL, LESSEQUAL, EQUAL };
        public double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        Condition_ condition;

        bool Evaluate(string formula, int angle)
        {
            // remove spaces, to lower case
            formula = Regex.Replace(formula, @"\s+", "");
            formula = formula.Trim().ToLower();
            formula.Replace("angle", ConvertToRadians(angle).ToString());
            // move operators like "cos" behind their operands
            // re-arrange text according to braces; most inner braces first
            // determine type of condition and create LEFT_SIDE_EXPRESSION and RIGHT_SIDE_EXPRESSION (RANDOMTRUE only LEFT_SIDE_EXPRESSION)
            while (formula.Length > 0)
            {
                if (formula.StartsWith("cos("))
                {

                }
            }

            return false;
        }
    }
}
