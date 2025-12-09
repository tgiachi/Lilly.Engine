using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Scenes;
using Lilly.Engine.Scenes.Transitions.Interfaces;
using Lilly.Engine.Types;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;

namespace Lilly.Engine.Scenes.Transitions.Base;

/// <summary>
/// Base class for scene transitions with timing and state management.
/// </summary>
public abstract class Transition : IDisposable
{
    private readonly float _halfDuration;
    private float _currentSeconds;

    protected Transition(float duration, ITransitionEffect effect)
    {
        Duration = duration;
        Effect = effect;
        _halfDuration = Duration / 2f;
    }

    public SceneTransitionState State { get; private set; } = SceneTransitionState.Out;
    public float Duration { get; }
    public float Value => Math.Clamp(_currentSeconds / _halfDuration, 0f, 1f);

    /// <summary>
    /// Gets the transition effect that renders the visual effect.
    /// </summary>
    public ITransitionEffect Effect { get; }

    public IScene? FromScene { get; protected set; }
    public IScene? ToScene { get; protected set; }

    public event EventHandler StateChanged;
    public event EventHandler Completed;

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Renders the transition using the sprite batcher.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <param name="spriteBatcher">The sprite batcher for rendering.</param>
    public void Render(GameTime gameTime, ILillySpriteBatcher spriteBatcher)
    {
        // Delegate rendering to the effect
        Effect.Render(gameTime, Value, spriteBatcher);
    }

    /// <summary>
    /// Starts the transition between two scenes.
    /// </summary>
    /// <param name="fromScene">The scene to transition from (null for initial scene).</param>
    /// <param name="toScene">The scene to transition to.</param>
    public virtual void Start(IScene? fromScene, IScene toScene)
    {
        FromScene = fromScene;
        ToScene = toScene;
    }

    public void Update(GameTime gameTime)
    {
        var elapsedSeconds = gameTime.GetElapsedSeconds();

        switch (State)
        {
            case SceneTransitionState.Out:
                _currentSeconds += elapsedSeconds;

                if (_currentSeconds >= _halfDuration)
                {
                    State = SceneTransitionState.In;
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }

                break;
            case SceneTransitionState.In:
                _currentSeconds -= elapsedSeconds;

                if (_currentSeconds <= 0.0f)
                {
                    Completed?.Invoke(this, EventArgs.Empty);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException("Invalid transition state");
        }
    }
}
