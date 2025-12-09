using System.Numerics;
using Lilly.Engine.Core.Interfaces.Services.Base;
using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Interfaces.Services;

public interface IChunkGeneratorService : IChunkProvider, ILillyService
{
    int Seed { get; set; }

    void AddGeneratorStep(IGeneratorStep generationStep);

    void ClearCache();

    Task GenerateInitialChunksAsync();

    Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position);

    Task<ChunkEntity> GetChunkByWorldPosition(int chunkX, int chunkY, int chunkZ);

    Task<IEnumerable<ChunkEntity>> GetChunksByPositions(IEnumerable<Vector3> positions);

    bool RemoveGeneratorStep(string stepName);

    bool TryGetCachedChunk(Vector3 position, out ChunkEntity? chunk);
}
