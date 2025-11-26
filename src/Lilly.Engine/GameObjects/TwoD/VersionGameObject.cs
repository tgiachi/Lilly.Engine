using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Utils;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Services;
using TrippyGL;

namespace Lilly.Engine.GameObjects.TwoD;

public class VersionGameObject : Base2dGameObject
{
    private readonly IGameObjectFactory _gameObjectFactory;

    private readonly IVersionService _versionService;

    private readonly RenderContext _renderContext;

    public VersionGameObject(
        IGameObjectFactory gameObjectFactory,
        IVersionService versionService,
        RenderContext renderContext
    ) : base("VersionGameObject")
    {
        _gameObjectFactory = gameObjectFactory;
        _versionService = versionService;
        _renderContext = renderContext;
    }

    public override void Initialize()
    {
        Transform.Position = new(0, 0);

        var textGameObject = _gameObjectFactory.Create<TextGameObject>();
        textGameObject.Text = $"Lilly Engine v{_versionService.GetVersionInfo().Version}";
        textGameObject.FontName = DefaultFonts.DefaultFontHudBoldName;
        textGameObject.FontSize = 24;
        textGameObject.Color = Color4b.White;
        textGameObject.Transform.Position = new Vector2(65, 0);

        var logoTexture = _gameObjectFactory.Create<TextureGameObject>();
        logoTexture.TextureName = "logo";
        logoTexture.Size = new Vector2(64, 64);

        var fpsCounter = _gameObjectFactory.Create<FpsGameObject>();
        fpsCounter.Color = Color4b.White;
        fpsCounter.Transform.Position = new Vector2(65, textGameObject.Transform.Size.Y + 1);

        var rectangle = _gameObjectFactory.Create<RectangleGameObject>();
        rectangle.Size = new Vector2(textGameObject.Transform.Size.X + 65, 65);
        rectangle.Color = Color4b.Black;

        AddGameObject2d(rectangle, textGameObject, logoTexture, fpsCounter);
    }


}
