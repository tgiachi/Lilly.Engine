using System;
using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Assets;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Utils;
using Lilly.Engine.Vertexts;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Primitives;
using TrippyGL;

namespace Lilly.Engine.GameObjects.ThreeD;

/// <summary>
/// Renders a cached Assimp model using the model shader (pos/norm/uv).
/// </summary>
public class ModelGameObject : Base3dGameObject, IInitializable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IAssetManager _assetManager;
    private readonly string _modelName;
    private readonly string _shaderName;
    private readonly string _textureName;

    private ShaderProgram? _shaderProgram;
    private Texture2D? _texture;
    private Data.Assets.ModelAsset? _model;
    private BoundingBox _localBounds;

    public Vector3 LightDirection { get; set; } = Vector3.Normalize(new Vector3(-0.4f, -1.0f, -0.2f));
    public Vector3 LightColor { get; set; } = Vector3.One;
    public Vector3 Ambient { get; set; } = new(0.15f, 0.15f, 0.15f);
    public Vector4 Tint { get; set; } = Vector4.One;

    public ModelGameObject(
        GraphicsDevice graphicsDevice,
        IRenderPipeline gameObjectManager,
        IAssetManager assetManager,
        string modelName,
        string shaderName = "model",
        string? textureName = null,
        string name = "Model"
    ) : base(name, gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
        _modelName = modelName;
        _shaderName = shaderName;
        _textureName = textureName ?? DefaultTextures.WhiteTextureKey;
    }

    public void Initialize()
    {
        _model = _assetManager.GetModel(_modelName);
        _shaderProgram = _assetManager.GetShaderProgram(_shaderName);
        _texture = _assetManager.GetTexture<Texture2D>(_textureName);
        _localBounds = _model.Bounds;
    }

    public override BoundingBox BoundingBox
    {
        get
        {
            if (_model == null)
            {
                return base.BoundingBox;
            }

            return TransformBounds(_localBounds, Transform);
        }
    }

    public override void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera)
    {
        if (!IsActive || _model == null || _shaderProgram == null || _texture == null)
        {
            return;
        }

        graphicsDevice.ShaderProgram = _shaderProgram;
        graphicsDevice.BlendState = BlendState.NonPremultiplied;
        graphicsDevice.DepthState = DepthState.Default;
        graphicsDevice.FaceCullingEnabled = true;

        _shaderProgram.Uniforms["View"].SetValueMat4(camera.View);
        _shaderProgram.Uniforms["Projection"].SetValueMat4(camera.Projection);
        _shaderProgram.Uniforms["Texture"].SetValueTexture(_texture);
        _shaderProgram.Uniforms["LightDir"].SetValueVec3(LightDirection);
        _shaderProgram.Uniforms["LightColor"].SetValueVec3(LightColor);
        _shaderProgram.Uniforms["Ambient"].SetValueVec3(Ambient);
        _shaderProgram.Uniforms["Tint"].SetValueVec4(Tint);

        var objectTransform = Transform.GetTransformationMatrix();

        foreach (var instance in _model.Instances)
        {
            if (instance.MeshIndex < 0 || instance.MeshIndex >= _model.Meshes.Count)
            {
                continue;
            }

            var mesh = _model.Meshes[instance.MeshIndex];
            if (mesh.IndexCount == 0)
            {
                continue;
            }

            var world = instance.Transform * objectTransform;
            _shaderProgram.Uniforms["World"].SetValueMat4(world);

            graphicsDevice.VertexArray = mesh.VertexBuffer;
            graphicsDevice.DrawElements(PrimitiveType.Triangles, 0, mesh.IndexCount);
        }
    }

    private static BoundingBox TransformBounds(BoundingBox bounds, Transform3D transform)
    {
        var scale = Matrix4x4.CreateScale(transform.Scale);
        var rotation = Matrix4x4.CreateFromQuaternion(transform.Rotation);
        var translation = Matrix4x4.CreateTranslation(transform.Position);

        var world = scale * rotation * translation;
        return TransformBounds(bounds, world);
    }

    private static BoundingBox TransformBounds(BoundingBox bounds, Matrix4x4 matrix)
    {
        Span<Vector3> corners =
        [
            new(bounds.Min.X, bounds.Min.Y, bounds.Min.Z),
            new(bounds.Max.X, bounds.Min.Y, bounds.Min.Z),
            new(bounds.Min.X, bounds.Max.Y, bounds.Min.Z),
            new(bounds.Max.X, bounds.Max.Y, bounds.Min.Z),
            new(bounds.Min.X, bounds.Min.Y, bounds.Max.Z),
            new(bounds.Max.X, bounds.Min.Y, bounds.Max.Z),
            new(bounds.Min.X, bounds.Max.Y, bounds.Max.Z),
            new(bounds.Max.X, bounds.Max.Y, bounds.Max.Z)
        ];

        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);

        foreach (var corner in corners)
        {
            var v = Vector3.Transform(corner, matrix);
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }

        return new BoundingBox(min, max);
    }
}
