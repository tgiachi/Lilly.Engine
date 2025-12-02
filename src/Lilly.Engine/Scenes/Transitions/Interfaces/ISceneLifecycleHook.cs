using Lilly.Engine.Interfaces.Scenes;

namespace Lilly.Engine.Scenes.Transitions.Interfaces;

/// <summary>
/// Defines the contract for scene lifecycle hooks that are called during scene load/unload operations.
/// </summary>
public interface ISceneLifecycleHook
{
    /// <summary>
    /// Gets the priority of this hook. Higher priority hooks are called first.
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// Called after a scene is loaded.
    /// </summary>
    /// <param name="scene">The scene that was loaded.</param>
    void OnAfterLoad(IScene scene) { }

    /// <summary>
    /// Called before a scene is unloaded.
    /// </summary>
    /// <param name="scene">The scene being unloaded.</param>
    void OnBeforeUnload(IScene scene) { }
}
