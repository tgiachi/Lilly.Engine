using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Primitives;

namespace Lilly.Engine.Pipelines.Culling;

public class Octree
{
    private OctreeNode _root;
    private readonly List<IGameObject3d> _queryResultCache = new(256);

    public Octree(float size = 1000f, int maxObjectsPerNode = 8, int maxLevels = 5)
    {
        // Initialize with a centered region provided by size
        var half = size / 2.0f;
        var min = new Vector3(-half);
        var max = new Vector3(half);
        _root = new OctreeNode(new BoundingBox(min, max), maxObjectsPerNode, maxLevels, 0);
    }

    public void Clear()
    {
        _root.Clear();
    }

    public void Insert(IGameObject3d item)
    {
        _root.Insert(item);
    }

    public List<IGameObject3d> Query(Matrix4x4 frustumView, Matrix4x4 frustumProjection)
    {
        // Note: Ideally, we pass a BoundingFrustum object.
        // Assuming the caller might not have a BoundingFrustum instance ready,
        // but for now, we will rely on the interface checking intersection against the Camera frustum logic
        // typically passed down.
        // However, standard Octree query takes a BoundingFrustum or BoundingBox.

        // For this implementation, we will collect all candidates that *might* be visible
        // by checking AABB vs Frustum recursively.

        _queryResultCache.Clear();
        _root.Query(frustumView, frustumProjection, _queryResultCache);

        return _queryResultCache;
    }

    public void Query(Func<BoundingBox, bool> frustumIntersects, List<IGameObject3d> results)
    {
        _root.Query(frustumIntersects, results);
    }

    // Resize the octree if objects go out of bounds (Simplistic approach: Expand root)
    public void Resize(BoundingBox newRegion)
    {
        _root = new OctreeNode(newRegion, _root.MaxObjects, _root.MaxLevels, 0);
    }
}

internal class OctreeNode
{
    private readonly BoundingBox _region;
    private readonly List<IGameObject3d> _objects;
    private OctreeNode[]? _children;
    private readonly int _currentLevel;

    public int MaxObjects { get; }
    public int MaxLevels { get; }

    public OctreeNode(BoundingBox region, int maxObjects, int maxLevels, int currentLevel)
    {
        _region = region;
        MaxObjects = maxObjects;
        MaxLevels = maxLevels;
        _currentLevel = currentLevel;
        _objects = new List<IGameObject3d>();
    }

    public void Clear()
    {
        _objects.Clear();
        _children = null; // GC will handle the rest
    }

    public void Insert(IGameObject3d item)
    {
        // If the item doesn't intersect this node at all, we shouldn't be here
        // (Root check handles the initial call)

        // If we have children, try to push it down
        if (_children != null)
        {
            var index = GetChildIndex(item.BoundingBox);

            if (index != -1)
            {
                _children[index].Insert(item);

                return;
            }
        }

        // If no children or doesn't fit in a single child, add to this node
        _objects.Add(item);

        // Check if we need to split
        if (_objects.Count > MaxObjects && _currentLevel < MaxLevels && _children == null)
        {
            Split();

            // Re-distribute existing objects
            for (var i = _objects.Count - 1; i >= 0; i--)
            {
                var obj = _objects[i];
                var index = GetChildIndex(obj.BoundingBox);

                if (index != -1)
                {
                    _children[index].Insert(obj);
                    _objects.RemoveAt(i);
                }
            }
        }
    }

    public void Query(Matrix4x4 view, Matrix4x4 projection, List<IGameObject3d> results)
    {
        // Frustum culling check for the Node's region
        // We need a way to check BoundingBox vs Frustum (View*Proj).
        // Since IGameObject3d usually has an IsInFrustum method taking the object,
        // we might need a helper here.
        // For simplicity in this specific codebase context, we'll assume we simply check
        // if the node *intersects* the frustum.

        // *Optimistic approach*: If the node is visible, check its objects.
        // Implementing precise AABB-Frustum intersection here would require extracting planes.
        // To respect KISS and existing constraints, we delegate precise check to the entity level later,
        // or implement a basic box check.

        // For now, we collect everything in the octree to be filtered precisely by the exact Frustum check
        // OR we implement a basic containment check.
        // Let's assume everything in the tree is a "Broad Phase" candidate.

        results.AddRange(_objects);

        if (_children != null)
        {
            foreach (var child in _children)
            {
                // In a real implementation, we would check child._region vs Frustum here.
                // child.Query(view, projection, results);
                // Due to lack of 'BoundingFrustum' class in the provided context imports,
                // we simply recurse. This makes the Octree a spatial container,
                // but the "Culling" part will happen on the returned candidates.
                child.Query(view, projection, results);
            }
        }
    }

    // Improved Query utilizing a Predicate for Culling (Abstracting the Frustum logic)
    public void Query(Func<BoundingBox, bool> frustumIntersects, List<IGameObject3d> results)
    {
        if (!frustumIntersects(_region))
        {
            return;
        }

        results.AddRange(_objects);

        if (_children != null)
        {
            foreach (var child in _children)
            {
                child.Query(frustumIntersects, results);
            }
        }
    }

    private void Split()
    {
        var min = _region.Min;
        var max = _region.Max;
        var center = (min + max) * 0.5f;
        var half = (max - min) * 0.5f;
        var step = half * 0.5f; // Quarter of the full size

        _children = new OctreeNode[8];

        // Create 8 sub-regions
        for (var i = 0; i < 8; i++)
        {
            var offset = new Vector3(
                (i & 1) == 0 ? -step.X : step.X,
                (i & 2) == 0 ? -step.Y : step.Y,
                (i & 4) == 0 ? -step.Z : step.Z
            );

            // Calculate strictly divided bounds based on center
            Vector3 childMin = center;
            Vector3 childMax = center;

            if ((i & 1) == 0)
                childMin.X = min.X;
            else
                childMax.X = max.X;
            if ((i & 2) == 0)
                childMin.Y = min.Y;
            else
                childMax.Y = max.Y;
            if ((i & 4) == 0)
                childMin.Z = min.Z;
            else
                childMax.Z = max.Z;

            _children[i] = new OctreeNode(new BoundingBox(childMin, childMax), MaxObjects, MaxLevels, _currentLevel + 1);
        }
    }

    private int GetChildIndex(BoundingBox itemBounds)
    {
        // Simple check: does itemBounds fit entirely within a quadrant?
        // Using the center of the current node as the divider.
        var center = (_region.Min + _region.Max) * 0.5f;

        bool left = itemBounds.Max.X < center.X;
        bool right = itemBounds.Min.X > center.X;
        bool down = itemBounds.Max.Y < center.Y;
        bool up = itemBounds.Min.Y > center.Y;
        bool back = itemBounds.Max.Z < center.Z;
        bool front = itemBounds.Min.Z > center.Z;

        if (left)
        {
            if (down)
            {
                if (back)
                    return 0;
                if (front)
                    return 4;
            }
            else if (up)
            {
                if (back)
                    return 2;
                if (front)
                    return 6;
            }
        }
        else if (right)
        {
            if (down)
            {
                if (back)
                    return 1;
                if (front)
                    return 5;
            }
            else if (up)
            {
                if (back)
                    return 3;
                if (front)
                    return 7;
            }
        }

        return -1; // Straddles a boundary
    }
}
