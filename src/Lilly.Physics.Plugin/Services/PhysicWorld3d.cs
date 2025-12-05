using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;
using System.Linq;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Physics;
using Lilly.Engine.Interfaces.Physics;
using Lilly.Engine.Interfaces.Services;
using Lilly.Physics.Plugin.Callbacks;
using Lilly.Physics.Plugin.Data;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using Serilog;
using BepuRigidPose = BepuPhysics.RigidPose;
using EngineRigidPose = Lilly.Engine.Data.Physics.RigidPose;

namespace Lilly.Physics.Plugin.Services;

public class PhysicWorld3d : IPhysicWorld3d, IDisposable
{
    private readonly World3dPhysicConfig _config;
    private readonly Dictionary<int, BodyRecord> _bodies = new();
    private readonly Dictionary<PhysicsShape, TypedIndex> _shapeCache = new();
    private readonly Dictionary<uint, IPhysicsBodyHandle> _gameObjectsMap = new();

    private int _nextId = 1;

    private readonly IRenderPipeline _renderPipeline;

    private readonly ILogger _logger = Log.ForContext<PhysicWorld3d>();
    public BufferPool Pool { get; }

    public ThreadDispatcher ThreadDispatcher { get; }

    public Simulation Simulation { get; private set; }

    public PhysicWorld3d(World3dPhysicConfig config, RenderContext renderContext, IRenderPipeline renderPipeline)
    {
        _config = config;
        _renderPipeline = renderPipeline;
        Pool = new BufferPool();
        ThreadDispatcher = new ThreadDispatcher(config.ThreadCount);
        renderContext.Renderer.OnUpdate += Update;

        _logger.Information("Starting Physic World3d with {ThreadCount} threads", config.ThreadCount);

        _renderPipeline.GameObjectAdded += RenderPipelineOnGameObjectAdded;
        _renderPipeline.GameObjectRemoved += RenderPipelineOnGameObjectRemoved;
    }

    private void RenderPipelineOnGameObjectRemoved(IGameObject gameObject)
    {
        if (gameObject is IPhysicsGameObject3d physicsGameObject)
        {
            if (!_gameObjectsMap.TryGetValue(gameObject.Id, out var bodyId))
            {
                return;
            }
            Remove(bodyId);

            _logger.Debug(
                "PhysicsGameObject3d of type {GameObjectType} with ID {GameObjectId} was removed from physics world.",
                gameObject.Name,
                gameObject.Id
            );
        }
    }

    private void RenderPipelineOnGameObjectAdded(IGameObject gameObject)
    {
        if (gameObject is IPhysicsGameObject3d physicsGameObject)
        {
            var cfg = physicsGameObject.BuildBodyConfig();
            var handle = physicsGameObject.IsStatic
                             ? CreateStatic(cfg.Shape, cfg.Pose)
                             : CreateDynamic(cfg);
            physicsGameObject.OnPhysicsAttached(handle);

            _gameObjectsMap[gameObject.Id] = handle;

            _logger.Debug(
                "PhysicsGameObject3d of type {GameObjectType} with ID {GameObjectId} was attached to physics world.",
                gameObject.Name,
                gameObject.Id
            );
        }
    }

    private void Update(GameTime gameTime)
    {
        Simulation.Timestep(1 / 60f, ThreadDispatcher);
    }

    public void Dispose()
    {
        Simulation.Dispose();
        Pool.Clear();

        GC.SuppressFinalize(this);
    }

    public async Task StartAsync()
    {
        _logger.Information("Initializing Physic World3d simulation with gravity {Gravity}", _config.Gravity);
        Simulation = Simulation.Create(
            Pool,
            new DefaultNarrowPhaseCallbacks(),
            new DefaultPoseIntegratorCallbacks(_config.Gravity),
            new SolveDescription(8, 1)
        );
    }

    public Task ShutdownAsync()
    {
        return Task.CompletedTask;
    }

    public IPhysicsBodyHandle CreateDynamic(PhysicsBodyConfig config)
    {
        var shapeIndex = GetOrCreateShape(config.Shape);
        var inertia = ComputeInertia(config.Shape, config.Mass);

        var bodyDesc = BodyDescription.CreateDynamic(
            new BepuRigidPose(config.Pose.Position, config.Pose.Rotation),
            inertia,
            new CollidableDescription(shapeIndex, config.SpeculativeMargin),
            new BodyActivityDescription(0.01f)
        );

        var handle = Simulation.Bodies.Add(bodyDesc);
        var id = _nextId++;

        _bodies[id] = BodyRecord.Dynamic(handle);

        return new PhysicsBodyHandle(id);
    }

    public IPhysicsBodyHandle CreateStatic(PhysicsShape shape, EngineRigidPose pose)
    {
        var shapeIndex = GetOrCreateShape(shape);
        var handle = Simulation.Statics.Add(new StaticDescription(pose.Position, pose.Rotation, shapeIndex));
        var id = _nextId++;

        _bodies[id] = BodyRecord.Static(handle);

        return new PhysicsBodyHandle(id);
    }

    public void Remove(IPhysicsBodyHandle handle)
    {
        if (!_bodies.TryGetValue(handle.Id, out var record))
        {
            return;
        }

        if (record.IsStatic)
        {
            Simulation.Statics.Remove(record.StaticHandle);
        }
        else
        {
            Simulation.Bodies.Remove(record.BodyHandle);
        }

        _bodies.Remove(handle.Id);
    }

    public void SetPose(IPhysicsBodyHandle handle, EngineRigidPose pose)
    {
        if (!_bodies.TryGetValue(handle.Id, out var record))
        {
            return;
        }

        if (record.IsStatic)
        {
            var staticRef = Simulation.Statics.GetStaticReference(record.StaticHandle);
            staticRef.Pose = new(pose.Position, pose.Rotation);
        }
        else
        {
            var body = Simulation.Bodies.GetBodyReference(record.BodyHandle);
            body.Pose.Position = pose.Position;
            body.Pose.Orientation = pose.Rotation;
            body.Awake = true;
        }
    }

    public EngineRigidPose GetPose(IPhysicsBodyHandle handle)
    {
        if (!_bodies.TryGetValue(handle.Id, out var record))
        {
            return new(Vector3.Zero, Quaternion.Identity);
        }

        if (record.IsStatic)
        {
            var staticRef = Simulation.Statics.GetStaticReference(record.StaticHandle);

            return new(staticRef.Pose.Position, staticRef.Pose.Orientation);
        }

        var body = Simulation.Bodies.GetBodyReference(record.BodyHandle);

        return new EngineRigidPose(body.Pose.Position, body.Pose.Orientation);
    }

    public void SetVelocity(IPhysicsBodyHandle handle, Vector3 linear, Vector3 angular)
    {
        if (!_bodies.TryGetValue(handle.Id, out var record) || record.IsStatic)
        {
            return;
        }

        var body = Simulation.Bodies.GetBodyReference(record.BodyHandle);
        body.Velocity.Linear = linear;
        body.Velocity.Angular = angular;
        body.Awake = true;
    }

    public void ApplyImpulse(IPhysicsBodyHandle handle, Vector3 impulse, Vector3 offset)
    {
        if (!_bodies.TryGetValue(handle.Id, out var record) || record.IsStatic)
        {
            return;
        }

        var body = Simulation.Bodies.GetBodyReference(record.BodyHandle);
        body.Awake = true;
        body.ApplyLinearImpulse(impulse);

        if (offset != Vector3.Zero)
        {
            body.ApplyImpulse(offset, impulse);
        }
    }

    private TypedIndex GetOrCreateShape(PhysicsShape shape)
    {
        if (_shapeCache.TryGetValue(shape, out var cached))
        {
            return cached;
        }

        TypedIndex index = shape switch
        {
            BoxShape b     => Simulation.Shapes.Add(new BepuPhysics.Collidables.Box(b.Width, b.Height, b.Depth)),
            SphereShape s  => Simulation.Shapes.Add(new BepuPhysics.Collidables.Sphere(s.Radius)),
            CapsuleShape c => Simulation.Shapes.Add(new BepuPhysics.Collidables.Capsule(c.Radius, c.Length)),
            MeshShape m    => Simulation.Shapes.Add(CreateMesh(m)),
            _              => throw new ArgumentOutOfRangeException(nameof(shape), "Unsupported shape type")
        };

        _shapeCache[shape] = index;

        return index;
    }

    private BodyInertia ComputeInertia(PhysicsShape shape, float mass)
    {
        return shape switch
        {
            BoxShape b     => new Box(b.Width, b.Height, b.Depth).ComputeInertia(mass),
            SphereShape s  => new Sphere(s.Radius).ComputeInertia(mass),
            CapsuleShape c => new Capsule(c.Radius, c.Length).ComputeInertia(mass),
            MeshShape      => throw new NotSupportedException("Triangle meshes are intended for static bodies in BEPU."),
            _              => default
        };
    }

    private Mesh CreateMesh(MeshShape meshShape)
    {
        var indices = meshShape.Indices;
        var vertices = meshShape.Vertices;

        if (indices.Count % 3 != 0)
        {
            throw new ArgumentException("Triangle mesh indices must be divisible by 3.", nameof(meshShape));
        }

        var triangleCount = indices.Count / 3;
        Pool.Take<Triangle>(triangleCount, out var triangles);

        for (var i = 0; i < triangleCount; i++)
        {
            var i0 = indices[i * 3];
            var i1 = indices[i * 3 + 1];
            var i2 = indices[i * 3 + 2];

            ref var tri = ref triangles[i];
            tri = new Triangle(vertices[i0], vertices[i1], vertices[i2]);
        }

        return new Mesh(triangles, new Vector3(1f), Pool);
    }

    private readonly record struct BodyRecord(bool IsStatic, BodyHandle BodyHandle, StaticHandle StaticHandle)
    {
        public static BodyRecord Dynamic(BodyHandle bodyHandle)
            => new(false, bodyHandle, default);

        public static BodyRecord Static(StaticHandle staticHandle)
            => new(true, default, staticHandle);
    }
}
