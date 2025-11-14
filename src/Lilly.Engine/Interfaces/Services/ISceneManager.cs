using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.Scenes;
using Lilly.Engine.Scenes.Transitions.Base;

namespace Lilly.Engine.Interfaces.Services;

/// <summary>
/// Defines the contract for scene management services.
/// </summary>
public interface ISceneManager : IUpdatable, IDisposable
{
    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    IScene? CurrentScene { get; }

    /// <summary>
    /// Gets a value indicating whether a scene transition is in progress.
    /// </summary>
    bool IsTransitioning { get; }

    /// <summary>
    /// Gets the current scene transition, if any.
    /// </summary>
    Transition? CurrentTransition { get; }

    /// <summary>
    /// Changes to a different scene immediately without a transition.
    /// </summary>
    /// <param name="sceneName">The name of the scene to change to.</param>
    void ChangeScene(string sceneName);

    /// <summary>
    /// Changes to a different scene immediately without a transition.
    /// </summary>
    /// <param name="scene">The scene to change to.</param>
    void ChangeScene(IScene scene);

    /// <summary>
    /// Changes to a different scene with a transition effect.
    /// </summary>
    /// <param name="sceneName">The name of the scene to change to.</param>
    /// <param name="transition">The transition to use.</param>
    void ChangeScene(string sceneName, Transition transition);

    /// <summary>
    /// Changes to a different scene with a transition effect.
    /// </summary>
    /// <param name="scene">The scene to change to.</param>
    /// <param name="transition">The transition to use.</param>
    void ChangeScene(IScene scene, Transition transition);

    /// <summary>
    /// Gets a registered scene by name.
    /// </summary>
    /// <param name="sceneName">The name of the scene.</param>
    /// <returns>The scene if found, otherwise null.</returns>
    IScene? GetScene(string sceneName);

    /// <summary>
    /// Registers a scene with the scene manager.
    /// </summary>
    /// <param name="scene">The scene to register.</param>
    void RegisterScene(IScene scene);

    /// <summary>
    /// Unregisters a scene from the scene manager.
    /// </summary>
    /// <param name="sceneName">The name of the scene to unregister.</param>
    /// <returns>True if the scene was unregistered, false if not found.</returns>
    bool UnregisterScene(string sceneName);

    /// <summary>
    /// Updates the scene manager and any active transitions.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    void Update(GameTime gameTime);
}
