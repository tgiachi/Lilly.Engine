using System.Collections.Generic;
using System.Numerics;
using TrippyGL;
using Lilly.Engine.Vertexts;

namespace Lilly.Engine.Data.Assets;

public sealed class ModelMeshData : IDisposable
{
    public VertexBuffer<VertexPositionNormalTex> VertexBuffer { get; }
    public uint IndexCount { get; }
    public int MaterialIndex { get; }

    public ModelMeshData(VertexBuffer<VertexPositionNormalTex> vertexBuffer, uint indexCount, int materialIndex)
    {
        VertexBuffer = vertexBuffer;
        IndexCount = indexCount;
        MaterialIndex = materialIndex;
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

    public ModelAsset(IReadOnlyList<ModelMeshData> meshes, IReadOnlyList<ModelInstance> instances)
    {
        Meshes = meshes;
        Instances = instances;
    }

    public void Dispose()
    {
        foreach (var mesh in Meshes)
        {
            mesh.Dispose();
        }
    }
}
