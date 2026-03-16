namespace ClimateExplorer.Core.DataPreparation;

using System.Text.RegularExpressions;

/// <summary>
/// Parses a simple text expression into a function that transforms a data record value.
///
/// Supported formats (whitespace around operators is optional):
///   Single comparison : x > 25   x == 0   x less-than 0   x >= 1   x less-than-eq 2.2   x != 0
///   Range (AND)       : x >= 1 AND x less-than 10   (use AND instead of ampamp in URL-stored expressions)
///   Negation          : -x   or   negate
///
/// Comparisons return 1.0 when true, 0.0 when false. A null input produces a null output.
/// </summary>
public static class CustomTransformationParser
{
    private static readonly Regex RangePattern = new(
        @"^x\s*([><=!]+)\s*(-?[\d.]+)\s*(&&|AND)\s*x\s*([><=!]+)\s*(-?[\d.]+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SinglePattern = new(
        @"^x\s*([><=!]+)\s*(-?[\d.]+)$",
        RegexOptions.Compiled);

    public static Func<double?, double?> Parse(string expression)
    {
        expression = expression.Trim();

        if (expression is "-x" or "negate")
        {
            return v => v == null ? null : v * -1;
        }

        var rangeMatch = RangePattern.Match(expression);
        if (rangeMatch.Success)
        {
            var op1 = rangeMatch.Groups[1].Value;
            var val1 = double.Parse(rangeMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            var op2 = rangeMatch.Groups[4].Value;
            var val2 = double.Parse(rangeMatch.Groups[5].Value, System.Globalization.CultureInfo.InvariantCulture);
            var pred1 = BuildPredicate(op1, val1);
            var pred2 = BuildPredicate(op2, val2);
            return v => v == null ? null : (pred1(v.Value) && pred2(v.Value) ? 1.0 : 0.0);
        }

        var singleMatch = SinglePattern.Match(expression);
        if (singleMatch.Success)
        {
            var op = singleMatch.Groups[1].Value;
            var val = double.Parse(singleMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            var pred = BuildPredicate(op, val);
            return v => v == null ? null : (pred(v.Value) ? 1.0 : 0.0);
        }

        throw new ArgumentException($"Cannot parse custom transformation: '{expression}'. Expected: 'x > 25', 'x >= 1 AND x < 10', '-x', or 'negate'.");
    }

    private static Func<double, bool> BuildPredicate(string op, double val)
    {
        return op switch
        {
            "==" => v => v == val,
            "!=" => v => v != val,
            ">" => v => v > val,
            ">=" => v => v >= val,
            "<" => v => v < val,
            "<=" => v => v <= val,
            _ => throw new ArgumentException($"Unsupported operator: '{op}'"),
        };
    }
}