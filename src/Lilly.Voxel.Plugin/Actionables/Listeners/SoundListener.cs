using Lilly.Engine.Interfaces.Services;
using Lilly.Voxel.Plugin.Actionables.Components;
using Lilly.Voxel.Plugin.Actionables.Listeners.Base;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Actionables.Listeners;

public class SoundListener : ComponentQueryListener
{
    private readonly IAudioService _soundService;

    public SoundListener(IAudioService soundService)
    {
        _soundService = soundService;
    }

    public override ActionEventType EventType => ActionEventType.OnUse;

    public override void DispatchAction(ActionEventContext actionEventContext)
    {
        if (actionEventContext.Target?.Components.Get<SoundComponent>() is SoundComponent soundComp)
        {
            _soundService.PlaySoundEffect3D(soundComp.SoundId, actionEventContext.WorldPosition);
        }
    }

    protected override ComponentQuery Query { get; } = new ComponentQuery
    {
        All = [typeof(SoundComponent)]
    };
}
