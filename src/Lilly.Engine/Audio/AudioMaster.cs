using Silk.NET.OpenAL;

namespace Lilly.Engine.Audio;

public class AudioMaster : IDisposable
{
    private static AudioMaster? instance;
    private readonly ALContext alc;
    public AL Al { get; }
    private readonly unsafe Context* context;
    private readonly unsafe Device* device;
    private bool disposed;

    private unsafe AudioMaster()
    {
        alc = ALContext.GetApi();
        Al = AL.GetApi();
        device = alc.OpenDevice("");

        if (device == null)
        {
            throw new InvalidOperationException("Could not create device");
        }

        context = alc.CreateContext(device, null);
        MakeContextCurrent();
        GetError();
    }

    public unsafe void Dispose()
    {
        if (!disposed)
        {
            alc.DestroyContext(context);
            alc.CloseDevice(device);

            Al.Dispose();
            alc.Dispose();
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public void GetError()
    {
        var err = Al.GetError();

        if (err != AudioError.NoError)
        {
            throw new InvalidOperationException($"Audio error {Enum.GetName(err)}");
        }
    }

    public static AudioMaster GetInstance()
    {
        instance ??= new();

        return instance;
    }

    private unsafe void MakeContextCurrent()
    {
        alc.MakeContextCurrent(context);
    }
}
