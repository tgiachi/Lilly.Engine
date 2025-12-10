using System.Diagnostics;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Physics;
using Lilly.Engine.Interfaces.Physics;
using Lilly.Engine.Interfaces.Services;
using Lilly.Physics.Plugin.Callbacks;
using Lilly.Physics.Plugin.Data;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Primitives;
using Serilog;
using EngineRigidPose = Lilly.Engine.Data.Physics.RigidPose;

namespace Lilly.Physics.Plugin.Services;

public class PhysicWorld3d : IPhysicWorld3d, IDisposable
{
    private readonly World3dPhysicConfig _config;
    private DefaultPoseIntegratorCallbacks _poseIntegratorCallbacks;
    private readonly Dictionary<int, BodyRecord> _bodies = new();

    private readonly Dictionary<IPhysicsGameObject3d, SubscriptionInfo> _subscriptions =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<int, IPhysicsGameObject3d> _bodyOwners = new();
    private readonly List<SyncEntry> _syncEntries = new();
    private readonly Dictionary<PhysicsShape, TypedIndex> _shapeCache = new();

    private int _nextId = 1;

    private readonly IRenderPipeline _renderPipeline;

    private readonly ILogger _logger = Log.ForContext<PhysicWorld3d>();
    public BufferPool Pool { get; }

    public ThreadDispatcher ThreadDispatcher { get; }

    public Simulation Simulation { get; private set; }

    private readonly RenderContext _renderContext;

    public PhysicWorld3d(World3dPhysicConfig config, RenderContext renderContext, IRenderPipeline renderPipeline)
    {
        _config = config;
        _renderContext = renderContext;
        _renderPipeline = renderPipeline;
        Pool = new();
        ThreadDispatcher = new(config.ThreadCount);
        renderContext.Renderer.OnUpdate += Update;

        _logger.Information("Starting Physic World3d with {ThreadCount} threads", config.ThreadCount);

        _renderPipeline.GameObjectAdded += RenderPipelineOnGameObjectAdded;
        _renderPipeline.GameObjectRemoved += RenderPipelineOnGameObjectRemoved;
    }

    public Vector3 Gravity => _config.Gravity;

    private readonly record struct BodyRecord(bool IsStatic, BodyHandle BodyHandle, StaticHandle StaticHandle)
    {
        public static BodyRecord Dynamic(BodyHandle bodyHandle)
            => new(false, bodyHandle, default);

        public static BodyRecord Static(StaticHandle staticHandle)
            => new(true, default, staticHandle);
    }

    private readonly record struct SyncEntry(
        int BodyId,
        BodyHandle BodyHandle,
        Transform3D Transform,
        PhysicsSyncMode SyncMode
    );

    private sealed record SubscriptionInfo(int BodyId, Action Handler);

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

    public IPhysicsBodyHandle CreateDynamic(PhysicsBodyConfig config)
    {
        TypedIndex shapeIndex;
        BodyInertia inertia;

        if (config.Shape is CompoundShape compoundShape)
        {
            (shapeIndex, inertia) = CreateCompoundDynamic(compoundShape, config.Mass);
        }
        else
        {
            shapeIndex = GetOrCreateShape(config.Shape);
            inertia = ComputeInertia(config.Shape, config.Mass);
        }

        var bodyDesc = BodyDescription.CreateDynamic(
            new(config.Pose.Position, config.Pose.Rotation),
            inertia,
            new(shapeIndex, config.SpeculativeMargin),
            new(0.01f)
        );

        var handle = Simulation.Bodies.Add(bodyDesc);
        var id = _nextId++;

        _bodies[id] = BodyRecord.Dynamic(handle);

        return new PhysicsBodyHandle(id);
    }

    public IPhysicsBodyHandle CreateStatic(PhysicsShape shape, EngineRigidPose pose)
    {
        var shapeIndex = shape is CompoundShape compoundShape
                             ? GetOrCreateCompoundShape(compoundShape)
                             : GetOrCreateShape(shape);
        var handle = Simulation.Statics.Add(new(pose.Position, pose.Rotation, shapeIndex));
        var id = _nextId++;

        _bodies[id] = BodyRecord.Static(handle);

        return new PhysicsBodyHandle(id);
    }

    public void Dispose()
    {
        Simulation.Dispose();
        Pool.Clear();

        GC.SuppressFinalize(this);
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

        return new(body.Pose.Position, body.Pose.Orientation);
    }

    public PhysicsWorldStats GetStats()
    {
        var dynamicBodies = _bodies.Count(b => !b.Value.IsStatic);
        var staticBodies = _bodies.Count - dynamicBodies;
        var activeDynamics = Simulation?.Bodies.ActiveSet.Count ?? 0;

        return new(
            dynamicBodies,
            activeDynamics,
            staticBodies,
            _shapeCache.Count,
            _config.ThreadCount,
            Gravity
        );
    }

    public void Remove(IPhysicsBodyHandle handle)
    {
        RemoveInternal(handle);
    }

    public void SetGravity(Vector3 gravity)
    {
        _config.Gravity = gravity;
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

    public Task ShutdownAsync()
        => Task.CompletedTask;

    public async Task StartAsync()
    {
        _logger.Information("Initializing Physic World3d simulation with gravity {Gravity}", _config.Gravity);
        _poseIntegratorCallbacks = new(_config);
        Simulation = Simulation.Create(
            Pool,
            new DefaultNarrowPhaseCallbacks(),
            _poseIntegratorCallbacks,
            new(8, 1)
        );
    }

    public void WakeAllBodies()
    {
        foreach (var pair in _bodies)
        {
            if (pair.Value.IsStatic)
            {
                continue;
            }

            var bodyHandle = pair.Value.BodyHandle;
            var bodyRef = Simulation.Bodies.GetBodyReference(bodyHandle);

            if (bodyRef.Exists)
            {
                bodyRef.Awake = true;
            }
        }
    }

    private void AttachOrReplaceBody(IPhysicsGameObject3d physicsGameObject, bool subscribe)
    {
        var sw = Stopwatch.StartNew();
        var isReplace = false;

        if (_subscriptions.TryGetValue(physicsGameObject, out var existing))
        {
            RemoveInternal(new PhysicsBodyHandle(existing.BodyId));
            isReplace = true;
        }

        var cfg = physicsGameObject.BuildBodyConfig();
        var handle = physicsGameObject.IsStatic
                         ? CreateStatic(cfg.Shape, cfg.Pose)
                         : CreateDynamic(cfg);

        physicsGameObject.OnPhysicsAttached(handle);
        _bodyOwners[handle.Id] = physicsGameObject;

        if (!physicsGameObject.IsStatic)
        {
            RegisterSync(handle, physicsGameObject.PhysicsTransform, physicsGameObject.SyncMode);
        }

        if (subscribe && !_subscriptions.ContainsKey(physicsGameObject))
        {
            var handler = () => HandleShapeDirty(physicsGameObject);
            physicsGameObject.PhysicsShapeDirty += handler;
            _subscriptions[physicsGameObject] = new(handle.Id, handler);
        }
        else if (_subscriptions.TryGetValue(physicsGameObject, out var sub))
        {
            _subscriptions[physicsGameObject] = sub with { BodyId = handle.Id };
        }

        _logger.Debug(
            "AttachOrReplaceBody for {GameObjectName} ({IsReplace}) took {ElapsedMs}ms",
            physicsGameObject.GetType().Name,
            isReplace ? "REPLACE" : "NEW",
            sw.Elapsed.TotalMilliseconds
        );
    }

    private BodyInertia ComputeInertia(PhysicsShape shape, float mass)
    {
        return shape switch
        {
            BoxShape b        => new Box(b.Width, b.Height, b.Depth).ComputeInertia(mass),
            SphereShape s     => new Sphere(s.Radius).ComputeInertia(mass),
            CapsuleShape c    => new Capsule(c.Radius, c.Length).ComputeInertia(mass),
            ConvexHullShape h => CreateConvexHull(h).ComputeInertia(mass),
            MeshShape         => throw new NotSupportedException("Triangle meshes are intended for static bodies in BEPU."),
            CompoundShape     => throw new NotSupportedException("Compound shapes compute inertia during creation."),
            _                 => default
        };
    }

    private Mesh CreateMesh(MeshShape meshShape)
    {
        var start = Stopwatch.GetTimestamp();
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
            tri = new(vertices[i0], vertices[i1], vertices[i2]);
        }

        _logger.Debug(
            "Created triangle mesh with {TriangleCount} triangles in {ElapsedMilliseconds} ms",
            triangleCount,
            Stopwatch.GetElapsedTime(start)
        );

        return new(triangles, new(1f), Pool);
    }

    private ConvexHull CreateConvexHull(ConvexHullShape hullShape)
    {
        var start = Stopwatch.GetTimestamp();
        var points = hullShape.Vertices;

        Pool.Take(points.Count, out Buffer<Vector3> buffer);
        for (var i = 0; i < points.Count; i++)
        {
            buffer[i] = points[i];
        }

        var hull = new ConvexHull(buffer, Pool, out _);

        _logger.Debug(
            "Created convex hull with {VertexCount} vertices in {ElapsedMilliseconds} ms",
            points.Count,
            Stopwatch.GetElapsedTime(start)
        );


        return hull;
    }

    private void AddCompoundChild(CompoundBuilder builder, CompoundShapeChild child)
    {
        var pose = new BepuPhysics.RigidPose(child.LocalPosition, child.LocalOrientation);

        switch (child.Shape)
        {
            case BoxShape box:
                builder.Add(new Box(box.Width, box.Height, box.Depth), pose, child.Weight);
                break;
            case SphereShape sphere:
                builder.Add(new Sphere(sphere.Radius), pose, child.Weight);
                break;
            case CapsuleShape capsule:
                builder.Add(new Capsule(capsule.Radius, capsule.Length), pose, child.Weight);
                break;
            case ConvexHullShape hull:
                builder.Add(CreateConvexHull(hull), pose, child.Weight);
                break;
            default:
                throw new NotSupportedException("Compound shapes only support convex child shapes.");
        }
    }

    private (TypedIndex ShapeIndex, BodyInertia Inertia) CreateCompoundDynamic(
        CompoundShape compoundShape,
        float mass
    )
    {
        using var builder = new CompoundBuilder(Pool, Simulation.Shapes, compoundShape.Children.Count);

        foreach (var child in compoundShape.Children)
        {
            AddCompoundChild(builder, child);
        }

        if (builder.Children.Count == 0)
        {
            var fallback = new Box(1f, 1f, 1f);
            return (Simulation.Shapes.Add(fallback), fallback.ComputeInertia(mass));
        }

        builder.BuildDynamicCompound(out var children, out var inertia);

        var totalWeight = 1f / inertia.InverseMass;
        if (mass > 0f)
        {
            var scale = totalWeight / mass;
            inertia.InverseMass = 1f / mass;
            inertia.InverseInertiaTensor.XX *= scale;
            inertia.InverseInertiaTensor.YX *= scale;
            inertia.InverseInertiaTensor.YY *= scale;
            inertia.InverseInertiaTensor.ZX *= scale;
            inertia.InverseInertiaTensor.ZY *= scale;
            inertia.InverseInertiaTensor.ZZ *= scale;
        }

        var shapeIndex = Simulation.Shapes.Add(new Compound(children));

        return (shapeIndex, inertia);
    }

    private TypedIndex GetOrCreateCompoundShape(CompoundShape compoundShape)
    {
        if (_shapeCache.TryGetValue(compoundShape, out var cached))
        {
            return cached;
        }

        using var builder = new CompoundBuilder(Pool, Simulation.Shapes, compoundShape.Children.Count);

        foreach (var child in compoundShape.Children)
        {
            AddCompoundChild(builder, child);
        }

        if (builder.Children.Count == 0)
        {
            return Simulation.Shapes.Add(new Box(1f, 1f, 1f));
        }

        builder.BuildKinematicCompound(out var children);
        var shapeIndex = Simulation.Shapes.Add(new Compound(children));
        _shapeCache[compoundShape] = shapeIndex;

        return shapeIndex;
    }

    private TypedIndex GetOrCreateShape(PhysicsShape shape)
    {
        if (_shapeCache.TryGetValue(shape, out var cached))
        {
            return cached;
        }

        var index = shape switch
        {
            BoxShape b        => Simulation.Shapes.Add(new Box(b.Width, b.Height, b.Depth)),
            SphereShape s     => Simulation.Shapes.Add(new Sphere(s.Radius)),
            CapsuleShape c    => Simulation.Shapes.Add(new Capsule(c.Radius, c.Length)),
            ConvexHullShape h => Simulation.Shapes.Add(CreateConvexHull(h)),
            MeshShape m       => Simulation.Shapes.Add(CreateMesh(m)),
            CompoundShape c   => GetOrCreateCompoundShape(c),

            _                 => throw new ArgumentOutOfRangeException(nameof(shape), "Unsupported shape type")
        };

        _shapeCache[shape] = index;

        return index;
    }

    private void HandleShapeDirty(IPhysicsGameObject3d physicsGameObject)
    {
        AttachOrReplaceBody(physicsGameObject, false);
    }

    private void RegisterSync(IPhysicsBodyHandle handle, Transform3D transform, PhysicsSyncMode syncMode)
    {
        if (syncMode == PhysicsSyncMode.None)
        {
            return;
        }

        if (!_bodies.TryGetValue(handle.Id, out var record) || record.IsStatic)
        {
            return;
        }

        _syncEntries.Add(new(handle.Id, record.BodyHandle, transform, syncMode));
    }

    private void RemoveInternal(IPhysicsBodyHandle handle)
    {
        if (!_bodies.TryGetValue(handle.Id, out var record))
        {
            return;
        }

        if (_bodyOwners.TryGetValue(handle.Id, out var owner))
        {
            UnregisterSync(handle.Id);

            if (_subscriptions.TryGetValue(owner, out var sub) && sub.BodyId == handle.Id)
            {
                owner.OnPhysicsDetached();
            }
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
        _bodyOwners.Remove(handle.Id);
    }

    private void RenderPipelineOnGameObjectAdded(IGameObject gameObject)
    {
        if (gameObject is not IPhysicsGameObject3d physicsGameObject)
        {
            return;
        }

        var start = Stopwatch.GetTimestamp();
        AttachOrReplaceBody(physicsGameObject, true);

        _logger.Debug(
            "PhysicsGameObject3d of type {GameObjectType} with ID {GameObjectId} was attached to physics world. took {ElapsedMilliseconds} ms",
            gameObject.Name,
            gameObject.Id,
            Stopwatch.GetElapsedTime(start)
        );
    }

    private void RenderPipelineOnGameObjectRemoved(IGameObject gameObject)
    {
        if (gameObject is not IPhysicsGameObject3d physicsGameObject)
        {
            return;
        }

        if (_subscriptions.TryGetValue(physicsGameObject, out var sub))
        {
            physicsGameObject.PhysicsShapeDirty -= sub.Handler;
            _subscriptions.Remove(physicsGameObject);
            RemoveInternal(new PhysicsBodyHandle(sub.BodyId));
        }

        _logger.Debug(
            "PhysicsGameObject3d of type {GameObjectType} with ID {GameObjectId} was removed from physics world.",
            gameObject.Name,
            gameObject.Id
        );
    }

    private void UnregisterSync(int bodyId)
    {
        for (var i = _syncEntries.Count - 1; i >= 0; i--)
        {
            if (_syncEntries[i].BodyId == bodyId)
            {
                _syncEntries.RemoveAt(i);
            }
        }
    }

    private void Update(GameTime gameTime)
    {
        Simulation.Timestep(1 / 60f, ThreadDispatcher);

        // Sync dynamic bodies back to their game object transforms
        for (var i = 0; i < _syncEntries.Count; i++)
        {
            var entry = _syncEntries[i];
            var body = Simulation.Bodies.GetBodyReference(entry.BodyHandle);

            if (!body.Exists)
            {
                continue;
            }

            var pose = body.Pose;
            entry.Transform.Position = pose.Position;

            if (entry.SyncMode == PhysicsSyncMode.FullPose)
            {
                entry.Transform.Rotation = pose.Orientation;
            }
        }
    }
}
