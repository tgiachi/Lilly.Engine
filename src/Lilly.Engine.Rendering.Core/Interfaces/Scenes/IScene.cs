namespace Lilly.Engine.Rendering.Core.Interfaces.Scenes;

/// <summary>
/// Defines the contract for a scene in the SquidVox engine.
/// A scene represents a distinct state or screen in the application (e.g., menu, gameplay, settings).
/// </summary>
public interface IScene
{
    /// <summary>
    /// Gets the name of the scene.
    /// </summary>
    string Name { get; }

    delegate void SceneActivatedHandler(IScene scene);

    event SceneActivatedHandler SceneActivated;

    /// <summary>
    /// Called when the scene is loaded and becomes active.
    /// Use this to initialize scene-specific resources.
    /// </summary>
    void Load();

    /// <summary>
    /// Called when the scene is unloaded and becomes inactive.
    /// Use this to cleanup scene-specific resources.
    /// </summary>
    void Unload();
}
