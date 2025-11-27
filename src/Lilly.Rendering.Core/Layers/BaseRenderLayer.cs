using System.Diagnostics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Collections;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Pipeline;
using Lilly.Rendering.Core.Types;

namespace Lilly.Rendering.Core.Layers;

public class BaseRenderLayer<TGameObject> : IRenderLayer where TGameObject : class, IGameObject
{
    public string Name { get; }
    public RenderPriority Priority { get; }

    public bool IsActive { get; set; }

    public int ProcessedEntityCount { get; protected set; }
    public int SkippedEntityCount { get; protected set; }

    public int TotalEntityCount => Entities.Count;

    public double RenderTimeMilliseconds { get; private set; }

    public double UpdateTimeMilliseconds { get; private set; }

    private static readonly double TickToMilliseconds = 1000d / Stopwatch.Frequency;

    private double _renderStartTime;
    private double _updateStartTime;

    protected GameObjectCollection<TGameObject> Entities { get; } = [];

    public BaseRenderLayer(string name, RenderPriority priority)
    {
        Name = name;
        Priority = priority;
    }

    public virtual void Initialize() { }
    public virtual void Update(GameTime gameTime) { }
    public virtual void Render(GameTime gameTime) { }

    public virtual bool CanAdd<TEntity>(TEntity entity)
    {
        return typeof(TGameObject).IsAssignableFrom(entity.GetType());
    }

    public virtual void AddEntity<TEntity>(TEntity entity) where TEntity : IGameObject
    {
        if (entity is TGameObject typedEntity)
        {
            Entities.Add(typedEntity);
        }
    }

    public void RemoveEntity<TEntity>(TEntity entity) where TEntity : IGameObject
    {
        if (entity is TGameObject typedEntity)
        {
            Entities.Remove(typedEntity);
        }
    }

    protected void StartRenderTimer()
    {
        _renderStartTime = Stopwatch.GetTimestamp();
    }

    protected void EndRenderTimer()
    {
        if (_renderStartTime == 0)
        {
            RenderTimeMilliseconds = 0;
            return;
        }

        RenderTimeMilliseconds = (Stopwatch.GetTimestamp() - _renderStartTime) * TickToMilliseconds;
    }

    protected void StartUpdateTimer()
    {
        _updateStartTime = Stopwatch.GetTimestamp();
    }

    protected void EndUpdateTimer()
    {
        if (_updateStartTime == 0)
        {
            UpdateTimeMilliseconds = 0;
            return;
        }

        UpdateTimeMilliseconds = (Stopwatch.GetTimestamp() - _updateStartTime) * TickToMilliseconds;
    }
}
