using System.Collections.Generic;
using System.Numerics;
using Lilly.Engine.Vertexts;
using Lilly.Rendering.Core.Primitives;
using TrippyGL;

namespace Lilly.Engine.Data.Assets;

public sealed class ModelMeshData : IDisposable
{
    public VertexBuffer<VertexPositionNormalTex> VertexBuffer { get; }
    public uint IndexCount { get; }
    public int MaterialIndex { get; }
    public string? TextureKey { get; }
    public BoundingBox Bounds { get; }

    public ModelMeshData(
        VertexBuffer<VertexPositionNormalTex> vertexBuffer,
        uint indexCount,
        int materialIndex,
        BoundingBox bounds,
        string? textureKey
    )
    {
        VertexBuffer = vertexBuffer;
        IndexCount = indexCount;
        MaterialIndex = materialIndex;
        Bounds = bounds;
        TextureKey = textureKey;
    }

    public void Dispose()
    {
        VertexBuffer.Dispose();
    }
}

public readonly struct ModelInstance
{
    public int MeshIndex { get; }
    public Matrix4x4 Transform { get; }

    public ModelInstance(int meshIndex, Matrix4x4 transform)
    {
        MeshIndex = meshIndex;
        Transform = transform;
    }
}

public sealed class ModelAsset : IDisposable
{
    public IReadOnlyList<ModelMeshData> Meshes { get; }
    public IReadOnlyList<ModelInstance> Instances { get; }
    public BoundingBox Bounds { get; }

    public ModelAsset(IReadOnlyList<ModelMeshData> meshes, IReadOnlyList<ModelInstance> instances, BoundingBox bounds)
    {
        Meshes = meshes;
        Instances = instances;
        Bounds = bounds;
    }

    public void Dispose()
    {
        foreach (var mesh in Meshes)
        {
            mesh.Dispose();
        }
    }
}
