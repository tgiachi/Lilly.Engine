using System.Numerics;
using DryIoc;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.GameObjects.ThreeD;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Demo.Plugin;

public class LillyDemoPlugin : ILillyPlugin
{
    public LillyPluginData LillyData
        => new(
            "com.tgiachi.lilly.demmo",
            "Lilly Demo Plugin",
            "0.1.0",
            "squid",
            "com.tgiachi.lilly.gameobjects"
        );

    public void EngineInitialized(IContainer container) { }
    public void EngineReady(IContainer container) { }

    public IEnumerable<IGameObject> GetGlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        var plane = gameObjectFactory.Create<SimpleBoxGameObject>();
        plane.Transform.Position = new(0f, -10f, 0f);

        //plane.IgnoreFrustumCulling = true;
        plane.Transform.Scale = new(10f, 1f, 10f);
        plane.Width = 10f;
        plane.Height = 4f;
        plane.Depth = 10f;

        plane.TextureName = "ground_texture";

        yield return plane;

        foreach (var index in Enumerable.Range(0, 1000))
        {
            var cube = gameObjectFactory.Create<SimpleCubeGameObject>();

            cube.YRotationSpeed = Random.Shared.NextSingle() * 0.1f;

            // cube.Transform.Rotation = new Vector3(;
            //     Random.Shared.NextSingle() * MathF.PI,
            //     Random.Shared.NextSingle() * MathF.PI,
            //     Random.Shared.NextSingle() * MathF.PI
            // );

            cube.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(
                Random.Shared.NextSingle() * MathF.PI * 2f, // Yaw (Y axis)
                Random.Shared.NextSingle() * MathF.PI * 2f, // Pitch (X axis)
                Random.Shared.NextSingle() * MathF.PI * 2f  // Roll (Z axis)
            );
            cube.Transform.Position = new(
                index % 5 * 2f - 4f,
                +100f,
                index / 5 * 2f - 1f
            );

            yield return cube;
        }

        var capsule = gameObjectFactory.Create<SimpleCapsuleGameObject>();
        capsule.Transform.Position = new(5f, 0f, 0f);
        capsule.Height = 3f;
        capsule.Radius = 0.5f;

        yield return capsule;

        var model = gameObjectFactory.Create<ModelGameObject>();
        model.ModelName = "crate";
        model.Transform.Position = new(-5f, 3f, 0f);

        yield return model;


        var jeep = gameObjectFactory.Create<ModelGameObject>();
        jeep.ModelName = "jeep";
        jeep.UseCompoundShape = true;
        jeep.IsStatic = false;
        jeep.Transform.Position = new(-3f, 3f, 0f);

        yield return jeep;
    }

    public IContainer RegisterModule(IContainer container)
    {
        return container;
    }
}
