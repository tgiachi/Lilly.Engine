using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Builders;

/// <summary>
/// Builder class for constructing block types with various properties and textures.
/// </summary>
public class BlockTypeBuilder
{
    private ushort _id;
    private string _name;
    private bool _isSolid;
    private bool _isLiquid;
    private bool _isOpaque;
    private bool _isTransparent;
    private float _hardness;
    private bool _isBreakable;
    private bool _isBillboard;
    private bool _isActionable;

    private BlockTextureSet? _textureSet;

    private BlockTextureObject? _topTexture;
    private BlockTextureObject? _bottomTexture;
    private BlockTextureObject? _leftTexture;
    private BlockTextureObject? _rightTexture;
    private BlockTextureObject? _frontTexture;
    private BlockTextureObject? _backTexture;

    public BlockTypeBuilder AsActionable(bool isActionable = true)
    {
        _isActionable = isActionable;

        return this;
    }

    public BlockTypeBuilder AsBillboard(bool isBillboard = true)
    {
        _isBillboard = isBillboard;

        return this;
    }

    public BlockTypeBuilder AsBreakable(bool isBreakable = true)
    {
        _isBreakable = isBreakable;

        return this;
    }

    public BlockTypeBuilder AsLiquid(bool isLiquid = true)
    {
        _isLiquid = isLiquid;

        return this;
    }

    public BlockTypeBuilder AsOpaque(bool isOpaque = true)
    {
        _isOpaque = isOpaque;

        return this;
    }

    public BlockTypeBuilder AsSolid(bool isSolid = true)
    {
        _isSolid = isSolid;

        return this;
    }

    public BlockTypeBuilder AsTransparent(bool isTransparent = true)
    {
        _isTransparent = isTransparent;

        return this;
    }

    public BlockType Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            throw new InvalidOperationException("BlockType name must be set.");
        }

        if (_textureSet == null && HasAnyTexture())
        {
            _textureSet = new(
                _topTexture ?? throw new InvalidOperationException("Top texture must be set."),
                _bottomTexture ?? throw new InvalidOperationException("Bottom texture must be set."),
                _leftTexture ?? throw new InvalidOperationException("Left texture must be set."),
                _rightTexture ?? throw new InvalidOperationException("Right texture must be set."),
                _frontTexture ?? throw new InvalidOperationException("Front texture must be set."),
                _backTexture ?? throw new InvalidOperationException("Back texture must be set.")
            );
        }

        return new()
        {
            Id = _id,
            Name = _name,
            IsSolid = _isSolid,
            IsLiquid = _isLiquid,
            IsOpaque = _isOpaque,
            IsTransparent = _isTransparent,
            Hardness = _hardness,
            IsBreakable = _isBreakable,
            TextureSet = _textureSet,
            IsBillboard = _isBillboard,
            IsActionable = _isActionable
        };
    }

    public BlockTypeBuilder WithAllFaces(string atlasName, int index)
    {
        var texture = new BlockTextureObject(atlasName, index);
        _topTexture = _bottomTexture = _leftTexture = _rightTexture = _frontTexture = _backTexture = texture;

        return this;
    }

    public BlockTypeBuilder WithBack(string atlasName, int index)
    {
        _backTexture = new BlockTextureObject(atlasName, index);

        return this;
    }

    public BlockTypeBuilder WithBottom(string atlasName, int index)
    {
        _bottomTexture = new BlockTextureObject(atlasName, index);

        return this;
    }

    public BlockTypeBuilder WithFront(string atlasName, int index)
    {
        _frontTexture = new BlockTextureObject(atlasName, index);

        return this;
    }

    public BlockTypeBuilder WithHardness(float hardness)
    {
        _hardness = hardness;

        return this;
    }

    public BlockTypeBuilder WithId(ushort id)
    {
        _id = id;

        return this;
    }

    public BlockTypeBuilder WithLeft(string atlasName, int index)
    {
        _leftTexture = new BlockTextureObject(atlasName, index);

        return this;
    }

    public BlockTypeBuilder WithName(string name)
    {
        _name = name;

        return this;
    }

    public BlockTypeBuilder WithRight(string atlasName, int index)
    {
        _rightTexture = new BlockTextureObject(atlasName, index);

        return this;
    }

    public BlockTypeBuilder WithTexture(BlockFace blockFace, string assetName, int index)
    {
        var texture = new BlockTextureObject(assetName, index);

        switch (blockFace)
        {
            case BlockFace.Top:
                _topTexture = texture;

                break;
            case BlockFace.Bottom:
                _bottomTexture = texture;

                break;
            case BlockFace.Left:
                _leftTexture = texture;

                break;
            case BlockFace.Right:
                _rightTexture = texture;

                break;
            case BlockFace.Front:
                _frontTexture = texture;

                break;
            case BlockFace.Back:
                _backTexture = texture;

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(blockFace), blockFace, null);
        }

        return this;
    }

    public BlockTypeBuilder WithTextures(BlockTextureSet textureSet)
    {
        _textureSet = textureSet;

        return this;
    }

    public BlockTypeBuilder WithTop(string atlasName, int index)
    {
        _topTexture = new BlockTextureObject(atlasName, index);

        return this;
    }

    public BlockTypeBuilder WithTopBottomAndSides(string atlasName, int topIndex, int bottomIndex, int sidesIndex)
    {
        _topTexture = new BlockTextureObject(atlasName, topIndex);
        _bottomTexture = new BlockTextureObject(atlasName, bottomIndex);
        _leftTexture = _rightTexture = _frontTexture = _backTexture = new BlockTextureObject(atlasName, sidesIndex);

        return this;
    }

    private bool HasAnyTexture()
        => _topTexture.HasValue ||
           _bottomTexture.HasValue ||
           _leftTexture.HasValue ||
           _rightTexture.HasValue ||
           _frontTexture.HasValue ||
           _backTexture.HasValue;
}
