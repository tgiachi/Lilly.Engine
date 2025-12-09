using System.Drawing;
using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Interfaces.Services;
using TrippyGL;

namespace Lilly.Engine.GameObjects.TwoD;

/// <summary>
/// A game object that renders a texture/sprite.
/// Supports world transforms for hierarchical positioning.
/// </summary>
public class TextureGameObject : Base2dGameObject
{
    private string _textureName;
    private Vector2 _size = Vector2.Zero;

    private readonly IAssetManager _assetManager;

    /// <summary>
    /// Gets or sets the name of the texture to render.
    /// </summary>
    public string TextureName
    {
        get => _textureName;
        set
        {
            _textureName = value;
            Transform.Size = _size;
        }
    }

    public Vector2 Position
    {
        set => Transform.Position = value;
        get => Transform.Position;
    }

    /// <summary>
    /// Gets or sets the size of the texture when rendered.
    /// If not set (Vector2.Zero), the texture will be drawn at its original size.
    /// </summary>
    public Vector2 Size
    {
        get => _size;
        set
        {
            _size = value;
            Transform.Size = value;
        }
    }

    /// <summary>
    /// Gets or sets the color tint to apply to the texture.
    /// Default is white (no tint).
    /// </summary>
    public Color4b Color { get; set; } = Color4b.White;

    /// <summary>
    /// Gets or sets the origin point for rotation and scaling.
    /// Default is Vector2.Zero (top-left corner).
    /// </summary>
    public Vector2 Origin { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the source rectangle to render from the texture.
    /// If null, the entire texture will be rendered.
    /// </summary>
    public Rectangle? SourceRectangle { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureGameObject" /> class.
    /// </summary>
    /// <param name="textureName">The name of the texture to render.</param>
    /// <param name="name">The name of the game object (default: "TextureGameObject").</param>
    /// <param name="zIndex">The rendering z-index (default: 0).</param>
    public TextureGameObject(IGameObjectManager gameObjectManager, IAssetManager assetManager)
        : base("TextureGameObject", gameObjectManager)
        => _assetManager = assetManager;

    protected Vector2 GetTextureSize()
    {
        if (SpriteBatcher == null || string.IsNullOrEmpty(_textureName))
        {
            return Vector2.Zero;
        }

        var textureInfo = _assetManager.GetTexture<Texture2D>(_textureName);

        return textureInfo == null ? Vector2.Zero : new Vector2(textureInfo.Width, textureInfo.Height) * Transform.Scale;
    }

    /// <summary>
    /// Draws the texture using the SpriteBatcher.
    /// Uses world transforms for hierarchical positioning.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    protected override void OnDraw(GameTime gameTime)
    {
        if (SpriteBatcher == null || string.IsNullOrEmpty(_textureName))
        {
            return;
        }

        var worldPosition = GetWorldPosition();
        var worldRotation = GetWorldRotation();
        var worldScale = GetWorldScale();

        // If size is set, use it as destination rectangle
        Rectangle? destination = null;

        if (_size != Vector2.Zero)
        {
            var worldSize = _size * worldScale;
            destination = new Rectangle(
                (int)worldPosition.X,
                (int)worldPosition.Y,
                (int)worldSize.X,
                (int)worldSize.Y
            );
        }

        SpriteBatcher.DrawTexure(
            _textureName,
            _size == Vector2.Zero ? worldPosition : null,
            destination,
            SourceRectangle,
            Color,
            Origin,
            _size == Vector2.Zero ? worldScale : null,
            worldRotation,
            ZIndex
        );
    }
}
