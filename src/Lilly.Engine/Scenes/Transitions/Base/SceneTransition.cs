using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.Scenes;
using Lilly.Engine.Types;

namespace Lilly.Engine.Scenes.Transitions.Base;



public abstract class Transition : IDisposable
{
    private readonly float _halfDuration;
    private float _currentSeconds;

    protected Transition(float duration)
    {
        Duration = duration;
        _halfDuration = Duration / 2f;
    }

    public SceneTransitionState State { get; private set; } = SceneTransitionState.Out;
    public float Duration { get; }
    public float Value => Math.Clamp(_currentSeconds / _halfDuration, 0f, 1f);

    public IScene? FromScene { get; protected set; }
    public IScene? ToScene { get; protected set; }

    public event EventHandler StateChanged;
    public event EventHandler Completed;

    public abstract void Dispose();

    public abstract void Render(GameTime gameTime);

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
                throw new ArgumentOutOfRangeException();
        }
    }
}
