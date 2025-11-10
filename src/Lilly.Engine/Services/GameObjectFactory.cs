using DryIoc;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Pooling;
using Lilly.Engine.Rendering.Core.Data.Internal;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Microsoft.Extensions.ObjectPool;
using MoonSharp.Interpreter;
using Serilog;

namespace Lilly.Engine.Services;

public class GameObjectFactory : IGameObjectFactory
{
    private readonly ILogger _logger = Log.ForContext<GameObjectFactory>();

    private uint _nextObjectId = 1;

    private readonly Dictionary<Type, ObjectPool<IGameObject>> _objectPools = new();

    private readonly List<GameObjectRegistration> _registrations;

    private readonly Dictionary<Type, bool> _useObjectPooling = new();

    private readonly IContainer _container;

    private readonly IScriptEngineService _scriptEngineService;

    public GameObjectFactory(
        List<GameObjectRegistration> registrations,
        IContainer container,
        IScriptEngineService scriptEngineService
    )
    {
        _registrations = registrations;
        _container = container;
        _scriptEngineService = scriptEngineService;

        foreach (var registration in _registrations)
        {
            _useObjectPooling[registration.Type] = registration.UseObjectPooling;

        }

        BuildDynamicScriptModule();
    }

    /// <summary>
    /// Creates a new instance of the specified game object type.
    /// </summary>
    /// <typeparam name="TGameObject">The type of game object to create.</typeparam>
    /// <returns>A new game object instance.</returns>
    public TGameObject CreateGameObject<TGameObject>() where TGameObject : class, IGameObject
    {
        var type = typeof(TGameObject);

        if (!_container.IsRegistered<TGameObject>())
        {
            throw new InvalidOperationException($"Game object of type {type.FullName} is not registered.");
        }

        // Check if object pooling is enabled for this type
        if (_useObjectPooling.TryGetValue(type, out var usePooling) && usePooling)
        {
            return GetOrCreateFromPool<TGameObject>();
        }

        // Create without pooling
        return CreateNewInstance<TGameObject>();
    }

    /// <summary>
    /// Creates a new instance of the specified game object type.
    /// </summary>
    /// <param name="type">The type of game object to create.</param>
    /// <returns>A new game object instance.</returns>
    public IGameObject CreateGameObject(Type type)
    {
        if (!_container.IsRegistered(type))
        {
            throw new InvalidOperationException($"Game object of type {type.FullName} is not registered.");
        }

        // Check if object pooling is enabled for this type
        if (_useObjectPooling.TryGetValue(type, out var usePooling) && usePooling)
        {
            var method = typeof(GameObjectFactory).GetMethod(nameof(GetOrCreateFromPool))!
                                                  .MakeGenericMethod(type);

            return (IGameObject)method.Invoke(this, null)!;
        }

        // Create without pooling
        var createMethod = typeof(GameObjectFactory).GetMethod(nameof(CreateNewInstance))!
                                                    .MakeGenericMethod(type);

        return (IGameObject)createMethod.Invoke(this, null)!;
    }

    private TGameObject GetOrCreateFromPool<TGameObject>() where TGameObject : class, IGameObject
    {
        var type = typeof(TGameObject);

        // Create pool if it doesn't exist
        if (!_objectPools.TryGetValue(type, out var pool))
        {
            pool = new DefaultObjectPool<IGameObject>(
                new GameObjectPooledPolicy(_container, type),
                maximumRetained: 100
            );
            _objectPools[type] = pool;

            _logger.Debug("Created object pool for {GameObjectType} with maximum retention of 100", type.FullName);
        }

        var instance = (TGameObject)pool.Get();
        instance.Id = _nextObjectId++;
        instance.Name = type.Name + "_" + instance.Id;
        _logger.Debug(
            "Retrieved game object of type {GameObjectType} from pool with ID {GameObjectId}",
            type.FullName,
            instance.Id
        );

        return instance;
    }

    private TGameObject CreateNewInstance<TGameObject>() where TGameObject : class, IGameObject
    {
        var instance = _container.Resolve<TGameObject>();
        instance.Id = _nextObjectId++;
        instance.Name = typeof(TGameObject).Name + "_" + instance.Id;
        _logger.Debug(
            "Created game object of type {GameObjectType} with ID {GameObjectId}",
            typeof(TGameObject).FullName,
            instance.Id
        );

        return instance;
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

            _logger.Debug(
                "Registering script function {FunctionName} for GameObject type {GameObjectType}",
                functionName,
                gameObjectType.Name
            );

            _scriptEngineService.AddManualModuleFunction<object[], object>(
                moduleName,
                functionName,
                _ => CreateGameObject(gameObjectType)
            );

            UserData.RegisterType(gameObjectType);
        }
    }

    /// <summary>
    /// Returns a game object to the pool if pooling is enabled for its type.
    /// </summary>
    /// <param name="gameObject">The game object to return to the pool.</param>
    public void ReturnToPool(IGameObject gameObject)
    {
        var type = gameObject.GetType();

        if (_useObjectPooling.TryGetValue(type, out var usePooling) && usePooling)
        {
            if (_objectPools.TryGetValue(type, out var pool))
            {
                pool.Return(gameObject);
                _logger.Debug(
                    "Returned game object of type {GameObjectType} with ID {GameObjectId} to pool",
                    type.FullName,
                    gameObject.Id
                );
            }
        }
        else
        {
            _logger.Debug(
                "Game object of type {GameObjectType} with ID {GameObjectId} does not use pooling, allowing GC",
                type.FullName,
                gameObject.Id
            );
        }
    }
}
