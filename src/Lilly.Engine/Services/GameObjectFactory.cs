using DryIoc;
using Lilly.Engine.Core.Extensions.Strings;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Data.Internal;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using MoonSharp.Interpreter;
using Serilog;
using Serilog.Events;

namespace Lilly.Engine.Services;

/// <summary>
/// Factory class responsible for creating and managing game objects.
/// It handles registration, instantiation, and integration with the script engine.
/// </summary>
public class GameObjectFactory : IGameObjectFactory
{
    private readonly ILogger _logger = Log.ForContext<GameObjectFactory>();

    private long _nextObjectId = 1;

    private readonly List<GameObjectRegistration> _registrations;

    private readonly IContainer _container;

    private readonly IScriptEngineService _scriptEngineService;

    public GameObjectFactory(
        IContainer container,
        IScriptEngineService scriptEngineService,
        List<GameObjectRegistration> registrations
    )
    {
        _registrations = registrations;
        _container = container;
        _scriptEngineService = scriptEngineService;

        BuildDynamicScriptModule();
    }

    /// <summary>
    /// Creates a new instance of the specified game object type.
    /// </summary>
    /// <typeparam name="TGameObject">The type of game object to create.</typeparam>
    /// <returns>A new game object instance.</returns>
    public TGameObject Create<TGameObject>() where TGameObject : class, IGameObject
    {
        var type = typeof(TGameObject);

        if (!_container.IsRegistered<TGameObject>())
        {
            throw new InvalidOperationException($"Game object of type {type.FullName} is not registered.");
        }

        return (TGameObject)Create(typeof(TGameObject));
    }

    /// <summary>
    /// Creates a new instance of the specified game object type.
    /// </summary>
    /// <param name="type">The type of game object to create.</param>
    /// <returns>A new game object instance.</returns>
    public IGameObject Create(Type type)
    {
        if (!_container.IsRegistered(type))
        {
            throw new InvalidOperationException($"Game object of type {type.FullName} is not registered.");
        }

        var instance = _container.Resolve<IGameObject>(type);
        AttachRenderContextIfNeeded(instance);
        var objectId = GenerateObjectId();
        instance.Id = objectId;
        instance.Name = GenerateGameObjectName(type, objectId);

        _logger.Debug(
            "Created game object of type {GameObjectType} with ID {GameObjectId}",
            instance.Name,
            objectId
        );

        return instance;
    }

    public void Destroy<TGameObject>(TGameObject gameObject) where TGameObject : class, IGameObject
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        _logger.Debug(
            "Destroying game object of type {GameObjectType} with ID {GameObjectId}",
            gameObject.Name,
            gameObject.Id
        );

        if (gameObject is IDisposable disposable)
        {
            disposable.Dispose();
        }

        DecrementObjectId();
    }

    /// <summary>
    /// Determines whether a game object type is registered in the factory.
    /// </summary>
    /// <typeparam name="TGameObject">The game object type to check.</typeparam>
    /// <returns>True if the game object type is registered; otherwise, false.</returns>
    public bool IsRegistered<TGameObject>() where TGameObject : class, IGameObject
        => _container.IsRegistered<TGameObject>();

    /// <summary>
    /// Determines whether a game object type is registered in the factory.
    /// </summary>
    /// <param name="type">The game object type to check.</param>
    /// <returns>True if the game object type is registered; otherwise, false.</returns>
    public bool IsRegistered(Type type)
        => type == null ? throw new ArgumentNullException(nameof(type)) : _container.IsRegistered(type);

    private void AttachRenderContextIfNeeded(IGameObject instance)
    {
        if (instance is not Base2dGameObject base2d)
        {
            return;
        }

        if (_container.IsRegistered<RenderContext>())
        {
            var renderContext = _container.Resolve<RenderContext>();
            base2d.UseRenderContext(renderContext);
        }
    }

    private void BuildDynamicScriptModule()
    {
        const string moduleName = "gameObjects";

        foreach (var gameObjectRegistration in _registrations)
        {
            var gameObjectType = gameObjectRegistration.Type;
            var baseName = gameObjectType.Name;

            if (baseName.EndsWith("GameObject", StringComparison.Ordinal))
            {
                baseName = baseName[..^"GameObject".Length];
            }

            var functionName = $"new_{_scriptEngineService.ToScriptEngineFunctionName(baseName)}";

            if (_logger.IsEnabled(LogEventLevel.Debug))
            {
                _logger.Debug(
                    "Registering script function {FunctionName} for GameObject type {GameObjectType}",
                    functionName,
                    gameObjectType.Name
                );
            }

            _scriptEngineService.AddManualModuleFunction<object[], object>(
                moduleName,
                functionName,
                _ => Create(gameObjectType)
            );

            UserData.RegisterType(gameObjectType);
        }
    }

    /// <summary>
    /// Decrements the object ID counter.
    /// </summary>
    private void DecrementObjectId()
        => Interlocked.Decrement(ref _nextObjectId);

    /// <summary>
    /// Generates a unique name for a game object based on its type and ID.
    /// </summary>
    private static string GenerateGameObjectName(Type type, uint id)
        => $"{type.Name}_{id}".ToSnakeCase();

    /// <summary>
    /// Generates a unique object ID in a thread-safe manner.
    /// </summary>
    private uint GenerateObjectId()
        => (uint)Interlocked.Increment(ref _nextObjectId);
}
