namespace Lilly.Engine.Core.Utils;

public static class MathUtils
{
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
