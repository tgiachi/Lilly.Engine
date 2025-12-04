using Lilly.Voxel.Plugin.Interfaces.Actionables;

namespace Lilly.Voxel.Plugin.Json.Entities;

public class BlockComponentsJson
{
    public IActionableComponent[] OnUse { get; set; } = [];
    public IActionableComponent[] OnBroke { get; set; } = [];
    public IActionableComponent[] Default { get; set; } = [];
}
