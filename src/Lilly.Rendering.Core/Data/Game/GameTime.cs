using System.Runtime.InteropServices;

namespace Lilly.Rendering.Core.Data.Game;

[StructLayout(LayoutKind.Auto)]
public struct GameTime
{
    public double Elapsed { get; set; }
    public double Total { get; set; }

    public void Update(double elapsed)
    {
        Elapsed = elapsed;
        Total += elapsed;
    }

    public TimeSpan ElapsedTime => TimeSpan.FromSeconds(Elapsed);

    public TimeSpan TotalTime => TimeSpan.FromSeconds(Total);

    public override string ToString()
    {
        return $"Elapsed: {Elapsed}, Total: {Total}";
    }
}
