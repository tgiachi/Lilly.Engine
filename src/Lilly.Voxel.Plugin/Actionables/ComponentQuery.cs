using Lilly.Voxel.Plugin.Collections;

namespace Lilly.Voxel.Plugin.Actionables;

/// <summary>
/// Component query with All/Any/None semantics (AND/OR/NOT) for filtering instances.
/// </summary>
/// <param name="All">All of these component types must be present.</param>
/// <param name="Any">At least one of these component types must be present (if provided).</param>
/// <param name="None">None of these component types must be present.</param>
public sealed record ComponentQuery(
    IReadOnlyList<Type>? All = null,
    IReadOnlyList<Type>? Any = null,
    IReadOnlyList<Type>? None = null
)
{
    public bool Matches(ComponentBag bag)
    {
        if (All is { Count: > 0 } && All.Any(t => !bag.Has(t)))
        {
            return false;
        }

        if (Any is { Count: > 0 } && !Any.Any(bag.Has))
        {
            return false;
        }

        if (None is { Count: > 0 } && None.Any(bag.Has))
        {
            return false;
        }

        return true;
    }
}
