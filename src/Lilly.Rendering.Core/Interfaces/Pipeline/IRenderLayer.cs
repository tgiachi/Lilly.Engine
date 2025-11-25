using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Types;

namespace Lilly.Rendering.Core.Interfaces.Pipeline;

/// <summary>
/// Represents a render layer in the rendering pipeline.
/// </summary>
public interface IRenderLayer
{
    /// <summary>
    /// Gets the name of the render layer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the render priority of the layer.
    /// </summary>
    RenderPriority Priority { get; }

    /// <summary>
    /// Initializes the render layer.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Updates the render layer with the given game time.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    void Update(GameTime gameTime);

    /// <summary>
    /// Renders the layer with the given game time.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    void Render(GameTime gameTime);

    /// <summary>
    /// Determines whether the specified entity type can be added to this layer.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns>True if the entity can be added; otherwise, false.</returns>
    bool CanAdd<TEntity>();

    /// <summary>
    /// Adds the specified entity to the render layer.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="entity">The entity to add.</param>
    void AddEntity<TEntity>(TEntity entity) where TEntity : IGameObject;

    /// <summary>
    /// Removes the specified entity from the render layer.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="entity">The entity to remove.</param>
    void RemoveEntity<TEntity>(TEntity entity) where TEntity : IGameObject;
}
