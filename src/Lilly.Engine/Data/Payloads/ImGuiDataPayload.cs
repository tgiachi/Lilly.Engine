namespace Lilly.Engine.Data.Payloads;

public readonly struct ImGuiDataPayload
{
    public string Name { get; init; }

    public Action DrawAction { get; init; }


    public ImGuiDataPayload(string name, Action drawAction)
    {
        Name = name;
        DrawAction = drawAction;
    }

}
