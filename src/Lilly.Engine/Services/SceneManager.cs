using DryIoc;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Internal;
using Lilly.Engine.Interfaces.Scenes;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Scenes.Transitions.Base;
using Lilly.Engine.Types;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using Serilog;

namespace Lilly.Engine.Services;

/// <summary>
/// Implements the scene management service.
/// Manages scene registration, transitions, and the current active scene.
/// </summary>
public class SceneManager : ISceneManager, IGameObject
{
    private readonly ILogger _logger = Log.ForContext<SceneManager>();
    private readonly Dictionary<string, IScene> _registeredScenes = new();
    private readonly List<SceneObjectRegistration> _registeredSceneObjects;
    private readonly IInputManagerService _inputManagerService;
    private readonly IRenderPipeline? _renderPipeline;
    private IScene? _nextScene;

    public IGameObject? Parent { get; set; }
    public IEnumerable<IGameObject> Children => Enumerable.Empty<IGameObject>();
    public uint Id { get; set; }
    public string Name { get; set; } = "SceneManager";
    public uint ZIndex { get; set; }
    public bool IsActive { get; set; } = true;

    public SceneManager(
        IInputManagerService inputManagerService,
        IContainer container,
        IRenderPipeline? renderPipeline,
        List<SceneObjectRegistration>? registeredSceneObjects = null
    )
    {
        _inputManagerService = inputManagerService;
        _registeredSceneObjects = registeredSceneObjects;
        _renderPipeline = renderPipeline;

        _logger.Information("Initializing Scene Manager");
        if (registeredSceneObjects != null)
        {
            foreach (var sceneObject in _registeredSceneObjects)
            {
                var scene = container.Resolve<IScene>(sceneObject.SceneType);
                RegisterScene(scene);
            }
        }
    }

    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public IScene? CurrentScene { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a scene transition is in progress.
    /// </summary>
    public bool IsTransitioning { get; private set; }

    /// <summary>
    /// Gets the current scene transition, if any.
    /// </summary>
    public Transition? CurrentTransition { get; private set; }

    /// <summary>
    /// Changes to a different scene immediately without a transition.
    /// </summary>
    /// <param name="sceneName">The name of the scene to change to.</param>
    public void ChangeScene(string sceneName)
    {
        var scene = GetScene(sceneName);

        if (scene != null)
        {
            ChangeScene(scene);
        }
    }

    /// <summary>
    /// Changes to a different scene immediately without a transition.
    /// </summary>
    /// <param name="scene">The scene to change to.</param>
    public void ChangeScene(IScene scene)
    {
        if (IsTransitioning)
        {
            _logger.Warning("Cannot change scene while a transition is in progress");

            return;
        }

        _logger.Information("Changing scene from {From} to {To}", CurrentScene?.Name ?? "none", scene.Name);

        // Unload current scene
        CurrentScene?.Unload();

        // Set new scene and load it
        CurrentScene = scene;
        CurrentScene.Load();
    }

    /// <summary>
    /// Changes to a different scene with a transition effect.
    /// </summary>
    /// <param name="sceneName">The name of the scene to change to.</param>
    /// <param name="transition">The transition to use.</param>
    public void ChangeScene(string sceneName, Transition transition)
    {
        var scene = GetScene(sceneName);

        if (scene != null)
        {
            ChangeScene(scene, transition);
        }
    }

    /// <summary>
    /// Changes to a different scene with a transition effect.
    /// </summary>
    /// <param name="scene">The scene to change to.</param>
    /// <param name="transition">The transition to use.</param>
    public void ChangeScene(IScene scene, Transition transition)
    {
        if (IsTransitioning)
        {
            _logger.Warning("Cannot change scene while a transition is in progress");

            return;
        }

        _logger.Information(
            "Changing scene from {From} to {To} with transition",
            CurrentScene?.Name ?? "none",
            scene.Name
        );

        IsTransitioning = true;
        CurrentTransition = transition;

        // Store the new scene to be loaded when transition completes
        _nextScene = scene;

        // Start the transition (don't load the new scene yet)
        transition.Start(CurrentScene, scene);

        // Add transition to render pipeline
        if (_renderPipeline != null && transition is TransitionGameObject transitionGameObject)
        {
            _renderPipeline.AddGameObject(transitionGameObject);
        }

        // Subscribe to transition events
        transition.StateChanged += OnTransitionStateChanged;
        transition.Completed += OnTransitionCompleted;
    }

    /// <summary>
    /// Disposes of the scene manager and all registered scenes.
    /// </summary>
    public void Dispose()
    {
        _logger.Debug("Disposing scene manager");

        // Dispose current transition if active
        if (CurrentTransition != null)
        {
            CurrentTransition.Completed -= OnTransitionCompleted;
            CurrentTransition.Dispose();
            CurrentTransition = null;
        }

        // Unload current scene
        CurrentScene?.Unload();

        // Clear registered scenes
        _registeredScenes.Clear();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets a registered scene by name.
    /// </summary>
    /// <param name="sceneName">The name of the scene.</param>
    /// <returns>The scene if found, otherwise null.</returns>
    public IScene? GetScene(string sceneName)
    {
        if (_registeredScenes.TryGetValue(sceneName, out var scene))
        {
            return scene;
        }

        _logger.Warning("Scene {Name} not found", sceneName);

        return null;
    }

    /// <summary>
    /// Registers a scene with the scene manager.
    /// </summary>
    /// <param name="scene">The scene to register.</param>
    public void RegisterScene(IScene scene)
    {
        if (_registeredScenes.ContainsKey(scene.Name))
        {
            _logger.Warning("Scene {Name} is already registered, replacing it", scene.Name);
        }

        _registeredScenes[scene.Name] = scene;
        _logger.Debug("Registered scene {Name}", scene.Name);
    }


    /// <summary>
    /// Unregisters a scene from the scene manager.
    /// </summary>
    /// <param name="sceneName">The name of the scene to unregister.</param>
    /// <returns>True if the scene was unregistered, false if not found.</returns>
    public bool UnregisterScene(string sceneName)
    {
        if (_registeredScenes.Remove(sceneName))
        {
            _logger.Debug("Unregistered scene {Name}", sceneName);

            return true;
        }

        _logger.Warning("Scene {Name} not found for unregistration", sceneName);

        return false;
    }

    public void Update(GameTime gameTime)
    {
        CurrentTransition?.Update(gameTime);
    }

    /// <summary>
    /// Called when a scene transition completes.
    /// </summary>
    private void OnTransitionCompleted(object? sender, EventArgs e)
    {
        if (CurrentTransition == null)
        {
            return;
        }

        _logger.Debug("Transition completed");

        // Unsubscribe from transition event
        CurrentTransition.Completed -= OnTransitionCompleted;

        // Unsubscribe from state change event
        CurrentTransition.StateChanged -= OnTransitionStateChanged;

        // Remove transition from render pipeline
        if (_renderPipeline != null && CurrentTransition is TransitionGameObject transitionGameObject)
        {
            _renderPipeline.RemoveGameObject(transitionGameObject);
        }

        // Set the new scene as current (it was already loaded when state changed to In)
        CurrentScene = _nextScene;
        _nextScene = null;

        // Old scene was already unloaded during state change, just update input context
        if (CurrentScene != null)
        {
            _inputManagerService.CurrentContext = CurrentScene.Name;
        }

        // Dispose the transition
        CurrentTransition.Dispose();
        CurrentTransition = null;

        IsTransitioning = false;
    }

    /// <summary>
    /// Called when a scene transition changes state (Out -> In).
    /// This loads the new scene and unloads the old one so it appears during the second half of the transition.
    /// </summary>
    private void OnTransitionStateChanged(object? sender, EventArgs e)
    {
        if (CurrentTransition == null)
        {
            return;
        }

        // Load the new scene and unload the old one when transitioning from Out to In
        if (CurrentTransition is Transition transition && transition.State == SceneTransitionState.In && _nextScene != null)
        {
            _logger.Debug("Loading new scene and unloading old scene during transition (Out -> In state change)");

            // Unload the old scene first
            if (CurrentScene != null)
            {
                CurrentScene.Unload();
            }

            // Load the new scene
            _nextScene.Load();
        }
    }
}
