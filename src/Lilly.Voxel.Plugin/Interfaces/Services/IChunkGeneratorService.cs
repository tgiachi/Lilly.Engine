using System.Numerics;
using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Interfaces.Services;

public interface IChunkGeneratorService : IChunkProvider
{
    int Seed { get; set; }

    Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position);

    bool TryGetCachedChunk(Vector3 position, out ChunkEntity? chunk);

    Task StartAsync(CancellationToken cancellationToken = default);

    Task<ChunkEntity> GetChunkByWorldPosition(int chunkX, int chunkY, int chunkZ);

    Task<IEnumerable<ChunkEntity>> GetChunksByPositions(IEnumerable<Vector3> positions);

    Task GenerateInitialChunksAsync();

    void AddGeneratorStep(IGeneratorStep generationStep);

    void ClearCache();

    bool RemoveGeneratorStep(string stepName);

}
