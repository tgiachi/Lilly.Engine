using System.Numerics;
using Lilly.Rendering.Core.Collections;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Entities.Transparent;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Primitives;

namespace Lilly.Engine.Pipelines.Culling;

public class SceneCuller
{
    private readonly Octree _octree;

    // Processed lists ready for rendering
    public List<IGameObject3d> OpaqueEntities { get; } = new(512);
    public List<IGameObject3d> TransparentEntities { get; } = new(128);

    // Statistics
    public int ProcessedCount => OpaqueEntities.Count + TransparentEntities.Count;
    public int SkippedCount { get; private set; }

    public SceneCuller()
    {
        // Initialize with a reasonably large world size.
        // In a real engine, this should be configurable or dynamic.
        _octree = new Octree(size: 2000f);
    }

    public void Process(ICamera3D camera, GameObjectCollection<IGameObject3d> allEntities)
    {
        OpaqueEntities.Clear();
        TransparentEntities.Clear();
        SkippedCount = 0;

        // 1. Rebuild Octree (Dynamic Scene Strategy)
        // For static scenes, we would only update moved objects.
        // For simplicity and correctness with purely dynamic lists, we clear and rebuild.
        _octree.Clear();

        // Basic optimization: Only insert active entities
        for (var i = 0; i < allEntities.Count; i++)
        {
            if (allEntities[i].IsActive)
            {
                _octree.Insert(allEntities[i]);
            }
        }

        // 2. Broad Phase Culling via Octree
        // We define a predicate to check if an octree node (BoundingBox) is visible.
        // Since we don't have direct access to BoundingFrustum class here, we rely on the camera's ability
        // to check bounds or we simply retrieve everything and do narrow phase.
        // Ideally: bool Intersects(BoundingBox b) => camera.Frustum.Intersects(b);

        // Using a "Get All" strategy for the Octree query temporarily if specific Frustum-Box intersection
        // isn't exposed on ICamera3d, then filtering individually.
        // If ICamera3d exposes IsInFrustum(BoundingBox), we use that.
        // Assuming camera.IsInFrustum(IGameObject3d) internally checks bounds.

        // Let's create a temporary list for broad phase results
        var broadPhaseResults = new List<IGameObject3d>(allEntities.Count);

        // Here we pass a lambda. Since we can't easily check 'box vs camera' without the Frustum object,
        // we will pass a "true" predicate to get all objects in the octree,
        // effectively using the Octree just as a spatial organizer for now,
        // or we implement a crude check if camera position is within distance + radius.

        // REAL IMPLEMENTATION: We want to use the octree to cull.
        // We need to approximate the frustum check or use the camera to check the node bounds.
        // Since we don't see 'IsInFrustum(BoundingBox)' on the interface in the context,
        // we will fallback to standard iteration but use the structure for future expansion.

        _octree.Query((box) => IsBoxVisible(camera, box), broadPhaseResults);

        // 3. Narrow Phase & Sorting
        var cameraPos = camera.Position;

        foreach (var entity in broadPhaseResults)
        {
            if (camera.IsInFrustum(entity))
            {
                if (entity is ITransparentRenderable3d)
                {
                    TransparentEntities.Add(entity);
                }
                else
                {
                    OpaqueEntities.Add(entity);
                }
            }
            else
            {
                SkippedCount++;
            }
        }

        SortLists(cameraPos);
    }

    private bool IsBoxVisible(ICamera3D camera, BoundingBox box)
    {
        // Placeholder for efficient Box-Frustum check.
        // If the camera interface supports checking a BoundingBox directly, use it.
        // Otherwise, return true to pass to Narrow Phase.
        return true;
    }

    private void SortLists(Vector3 cameraPos)
    {
        // Front-to-Back for Opaque (Early-Z optimization)
        OpaqueEntities.Sort(
            (a, b) =>
            {
                var distA = Vector3.DistanceSquared(a.Transform.Position, cameraPos);
                var distB = Vector3.DistanceSquared(b.Transform.Position, cameraPos);

                return distA.CompareTo(distB);
            }
        );

        // Back-to-Front for Transparent (Painter's Algorithm)
        TransparentEntities.Sort(
            (a, b) =>
            {
                var distA = Vector3.DistanceSquared(a.Transform.Position, cameraPos);
                var distB = Vector3.DistanceSquared(b.Transform.Position, cameraPos);

                return distB.CompareTo(distA);
            }
        );
    }
}
