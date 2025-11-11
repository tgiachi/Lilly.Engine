using Silk.NET.Input;
using Silk.NET.Input.Extensions;

namespace Lilly.Engine.Rendering.Core.Extensions;

public static class InputExtensions
{
    public static IEnumerable<Key> GetPressedKeys(this KeyboardState state)
    {
        foreach (var key in Enum.GetValues<Key>())
        {
            if (state.IsKeyPressed(key))
            {
                yield return key;
            }
        }
    }
}
