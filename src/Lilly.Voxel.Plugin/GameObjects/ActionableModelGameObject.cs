using System.Numerics;
using Lilly.Engine.GameObjects.ThreeD;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Primitives;
using Lilly.Voxel.Plugin.Collections;
using Lilly.Voxel.Plugin.Interfaces.Actionables;
using Lilly.Voxel.Plugin.Interfaces.Services;

namespace Lilly.Voxel.Plugin.GameObjects;

public class ActionableModelGameObject : ModelGameObject, IRaycastableActionableTarget, IDisposable
{
    private readonly IActionableService _actionableService;
    private bool _registered;

    public ComponentBag Components { get; } = new();
    public Guid TargetId { get; } = Guid.NewGuid();

    public ActionableModelGameObject(
        IRenderPipeline gameObjectManager,
        IAssetManager assetManager,
        IActionableService actionableService
    ) : base(gameObjectManager, assetManager)
    {
        _actionableService = actionableService;
    }

    public new void Initialize()
    {
        base.Initialize();

        if (!_registered)
        {
            _actionableService.RegisterRaycastTarget(this);
            _registered = true;
        }
    }

    public bool Raycast(Ray ray, float maxDistance, out float distance, out Vector3 hitPoint)
    {
        var bbox = BoundingBox;

        if (RayIntersectsBox(ray, bbox, out distance) && distance <= maxDistance)
        {
            hitPoint = ray.GetPoint(distance);

            return true;
        }

        distance = 0f;
        hitPoint = default;

        return false;
    }

    public void Dispose()
    {
        if (_registered)
        {
            _actionableService.UnregisterRaycastTarget(this);
            _registered = false;
        }

        GC.SuppressFinalize(this);
    }

    private static bool RayIntersectsBox(Ray ray, BoundingBox box, out float distance)
    {
        distance = 0f;

        var tMin = (box.Min.X - ray.Origin.X) / (ray.Direction.X == 0f ? float.Epsilon : ray.Direction.X);
        var tMax = (box.Max.X - ray.Origin.X) / (ray.Direction.X == 0f ? float.Epsilon : ray.Direction.X);

        if (tMin > tMax)
        {
            (tMin, tMax) = (tMax, tMin);
        }

        var tyMin = (box.Min.Y - ray.Origin.Y) / (ray.Direction.Y == 0f ? float.Epsilon : ray.Direction.Y);
        var tyMax = (box.Max.Y - ray.Origin.Y) / (ray.Direction.Y == 0f ? float.Epsilon : ray.Direction.Y);

        if (tyMin > tyMax)
        {
            (tyMin, tyMax) = (tyMax, tyMin);
        }

        if ((tMin > tyMax) || (tyMin > tMax))
        {
            return false;
        }

        if (tyMin > tMin)
        {
            tMin = tyMin;
        }

        if (tyMax < tMax)
        {
            tMax = tyMax;
        }

        var tzMin = (box.Min.Z - ray.Origin.Z) / (ray.Direction.Z == 0f ? float.Epsilon : ray.Direction.Z);
        var tzMax = (box.Max.Z - ray.Origin.Z) / (ray.Direction.Z == 0f ? float.Epsilon : ray.Direction.Z);

        if (tzMin > tzMax)
        {
            (tzMin, tzMax) = (tzMax, tzMin);
        }

        if ((tMin > tzMax) || (tzMin > tMax))
        {
            return false;
        }

        if (tzMin > tMin)
        {
            tMin = tzMin;
        }

        if (tzMax < tMax)
        {
            tMax = tzMax;
        }

        if (tMax < 0f)
        {
            return false;
        }

        distance = tMin < 0f ? tMax : tMin;

        return distance >= 0f;
    }
}
