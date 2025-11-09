namespace Lilly.Engine.Core.Attributes.Debugger;

/// <summary>Specifies a range for debugger editing.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]

/// <summary>
/// Attribute that specifies a range for debugger editing of numeric fields or properties.
/// </summary>
public class DebuggerRangeAttribute : Attribute
{
    /// <summary>Initializes a new instance of the DebuggerRangeAttribute class.</summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="step">The step value.</param>
    public DebuggerRangeAttribute(double min, double max, double step = 1)
    {
        Min = min;
        Max = max;
        Step = step;
    }

    /// <summary>Gets the minimum value.</summary>
    public double Min { get; }

    /// <summary>Gets the maximum value.</summary>
    public double Max { get; }

    /// <summary>Gets the step value.</summary>
    public double Step { get; }
}
