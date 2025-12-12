using System.Numerics;
using System.Runtime.InteropServices;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Physics;
using Lilly.Engine.Interfaces.Physics;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Vertexts;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Entities.Transparent;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Lights;
using Lilly.Rendering.Core.Primitives;
using Lilly.Rendering.Core.Types;
using Silk.NET.OpenGL;
using TrippyGL;
using PrimitiveType = TrippyGL.PrimitiveType;
using DirectionalLight = Lilly.Rendering.Core.Lights.DirectionalLight;
using PointLight = Lilly.Rendering.Core.Lights.PointLight;
using SpotLight = Lilly.Rendering.Core.Lights.SpotLight;

namespace Lilly.Engine.Pipelines;

public class ThreeDLayer : BaseRenderLayer<IGameObject3d>, IDisposable
{
    private readonly RenderContext _renderContext;
    private readonly IAssetManager _assetManager;
    private readonly ILightManager _lightManager;

    public bool IsWireframe { get; set; }
    public float WireframeLineWidth { get; set; } = 1.0f;

    private readonly ICamera3dService _camera3dService;
    public bool DebugDrawBoundingBoxes { get; set; }
    public bool DebugDrawPhysicsShapes { get; set; } = true;
    public Color4b DebugBoundsColor { get; set; } = Color4b.Green;
    public Color4b DebugPhysicsColor { get; set; } = Color4b.Magenta;
    public float DebugLineWidth { get; set; } = 1.5f;

    private ShaderProgram? _debugLineShader;
    private ShaderProgram? _shadowDepthShader;
    private Matrix4x4 _lightViewMatrix = Matrix4x4.Identity;
    private Matrix4x4 _lightProjectionMatrix = Matrix4x4.Identity;

    private VertexBuffer<PositionVertex>? _lineVbo;

    private ShadowFramebuffer _shadowFramebuffer;

    public List<IGameObject3d> EntitiesInCullingFrustum { get; } = new();

    public List<IGameObject3d> EntitiesOutsideCullingFrustum { get; } = new();

    public ThreeDLayer(
        RenderContext renderContext,
        ICamera3dService camera3dService,
        IAssetManager assetManager,
        ILightManager lightManager
    ) : base("ThreeD", RenderPriority.ThreeD)
    {
        _renderContext = renderContext;
        _camera3dService = camera3dService;
        _assetManager = assetManager;
        _lightManager = lightManager;
    }

    public override void Initialize()
    {
        _camera3dService.UpdateViewport(_renderContext.GraphicsDevice.Viewport);
        _shadowFramebuffer = new ShadowFramebuffer(_renderContext.OpenGl, _renderContext.GraphicsDevice, 2048, 2048);

        base.Initialize();
    }

    public override void Render(GameTime gameTime)
    {
        _shadowDepthShader ??= _assetManager.GetShaderProgram("shadow_depth");
        _renderContext.OpenGl.Enable(GLEnum.Multisample);

        // Revert to CullFront (though irrelevant if culling is disabled below) to match original state
        _renderContext.GraphicsDevice.CullFaceMode = CullingMode.CullFront;

        StartRenderTimer();

        if (_camera3dService.ActiveCamera == null)
        {
            EndRenderTimer();

            return;
        }

        EntitiesInCullingFrustum.Clear();
        EntitiesOutsideCullingFrustum.Clear();
        ProcessedEntityCount = 0;
        SkippedEntityCount = 0;

        CheckWireframe();
        _renderContext.GraphicsDevice.DepthState = DepthState.Default;
        _renderContext.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        // 1. Filter and Categorize (Culling)
        foreach (var entity in Entities)
        {
            if (entity.IsActive && _camera3dService.ActiveCamera.IsInFrustum(entity))
            {
                EntitiesInCullingFrustum.Add(entity);
            }
            else
            {
                EntitiesOutsideCullingFrustum.Add(entity);
                SkippedEntityCount++;
            }
        }

        // 2. Sort (Front-to-Back for Early-Z Optimization)
        var cameraPos = _camera3dService.ActiveCamera.Position;

        EntitiesInCullingFrustum.Sort(
            (a, b) =>
            {
                var distA = Vector3.DistanceSquared(a.Transform.Position, cameraPos);
                var distB = Vector3.DistanceSquared(b.Transform.Position, cameraPos);

                return distA.CompareTo(distB); // Ascending order (Near -> Far)
            }
        );

        var (dirLights, pointLights, spotLights) = _lightManager.GetActiveLights();
        var shadowLight = _lightManager.ShadowLight is { IsActive: true, CastsShadows: true }
                              ? _lightManager.ShadowLight
                              : null;

        if (shadowLight != null)
        {
            BuildLightMatrices(shadowLight, cameraPos);
            RenderShadowPass();
        }

        PrepareMaterialLitShader(cameraPos, dirLights, pointLights, spotLights, shadowLight != null);

        // 3. Draw Opaque Pass (Front-to-Back)
        foreach (var entity in EntitiesInCullingFrustum)
        {
            if (entity is IMaterialCaster materialCaster)
            {
                var materialLit = _assetManager.GetShaderProgram("material_lit");
                var receiveShadows = entity is IShadowReceiver3d shadowReceiver ? shadowReceiver.ReceiveShadows : true;
                ApplyMaterial(materialLit, materialCaster, receiveShadows);
            }

            entity.Draw(gameTime, _renderContext.GraphicsDevice, _camera3dService.ActiveCamera);

            ProcessedEntityCount++;
        }

        // 4. Draw Transparent Pass (Back-to-Front)
        for (var i = EntitiesInCullingFrustum.Count - 1; i >= 0; i--)
        {
            var entity = EntitiesInCullingFrustum[i];

            if (entity is ITransparentRenderable3d transparentEntity)
            {
                transparentEntity.DrawTransparent(gameTime, _renderContext.GraphicsDevice, _camera3dService.ActiveCamera);
            }
        }

        if (DebugDrawBoundingBoxes || DebugDrawPhysicsShapes)
        {
            DrawDebugOverlays();
        }

        RestoreState();
        EndRenderTimer();

        _renderContext.GraphicsDevice.ResetStates();
    }

    public void Dispose()
    {
        _shadowFramebuffer?.Dispose();
        _lineVbo?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void AddEdge(HashSet<(int, int)> edges, int a, int b)
    {
        if (a == b)
        {
            return;
        }

        var key = a < b ? (a, b) : (b, a);
        edges.Add(key);
    }

    private void CheckWireframe()
    {
        if (IsWireframe)
        {
            _renderContext.OpenGl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);
            _renderContext.OpenGl.Disable(GLEnum.CullFace);
            _renderContext.OpenGl.LineWidth(WireframeLineWidth);
        }
        else
        {
            // Disable culling to prevent "missing faces" if vertices are not strictly wound
            _renderContext.GraphicsDevice.FaceCullingEnabled = false;
        }
    }

    private void DrawBoundingBox(BoundingBox bounds, Vector4 color, Matrix4x4 view, Matrix4x4 projection)
    {
        var cornersArray = new Vector3[8];
        bounds.GetCorners(cornersArray);
        var corners = cornersArray.AsSpan();
        var edges = new[]
        {
            (0, 1), (1, 2), (2, 3), (3, 0), // top
            (4, 5), (5, 6), (6, 7), (7, 4), // bottom
            (0, 4), (1, 5), (2, 6), (3, 7)  // verticals
        };

        DrawLineList(corners, edges, color, view, projection);
    }

    private void DrawDebugOverlays()
    {
        var camera = _camera3dService.ActiveCamera;

        if (camera == null)
        {
            return;
        }

        EnsureLineShader();
        _renderContext.OpenGl.LineWidth(DebugLineWidth);

        foreach (var entity in EntitiesInCullingFrustum)
        {
            if (DebugDrawBoundingBoxes)
            {
                DrawBoundingBox(entity.BoundingBox, DebugBoundsColor, camera.View, camera.Projection);
            }

            if (DebugDrawPhysicsShapes && entity is IPhysicsGameObject3d physicsGameObject)
            {
                DrawPhysicsShape(physicsGameObject, DebugPhysicsColor, camera.View, camera.Projection);
            }
        }

        _renderContext.OpenGl.LineWidth(1f);
    }

    private void DrawLineList(
        ReadOnlySpan<Vector3> vertices,
        ReadOnlySpan<(int, int)> edges,
        Vector4 color,
        Matrix4x4 view,
        Matrix4x4 projection
    )
    {
        if (_debugLineShader == null)
        {
            return;
        }

        var lineVertices = new PositionVertex[edges.Length * 2];

        for (var i = 0; i < edges.Length; i++)
        {
            var edge = edges[i];
            lineVertices[i * 2] = new(vertices[edge.Item1]);
            lineVertices[i * 2 + 1] = new(vertices[edge.Item2]);
        }

        _lineVbo?.Dispose();
        _lineVbo = new VertexBuffer<PositionVertex>(_renderContext.GraphicsDevice, lineVertices, BufferUsage.DynamicDraw);

        _renderContext.GraphicsDevice.DepthState = DepthState.Default;
        _renderContext.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        _renderContext.GraphicsDevice.FaceCullingEnabled = false;

        _renderContext.GraphicsDevice.ShaderProgram = _debugLineShader;
        _debugLineShader!.Uniforms["World"].SetValueMat4(Matrix4x4.Identity);
        _debugLineShader.Uniforms["View"].SetValueMat4(view);
        _debugLineShader.Uniforms["Projection"].SetValueMat4(projection);
        _debugLineShader.Uniforms["Color"].SetValueVec4(color);

        _renderContext.GraphicsDevice.VertexArray = _lineVbo;
        _renderContext.GraphicsDevice.DrawArrays(PrimitiveType.Lines, 0, (uint)lineVertices.Length);
    }

    private void DrawPhysicsShape(
        IPhysicsGameObject3d physicsGameObject,
        Vector4 color,
        Matrix4x4 view,
        Matrix4x4 projection
    )
    {
        var config = physicsGameObject.BuildBodyConfig();
        var pose = config.Pose;

        List<Vector3> vertices = new();
        List<(int, int)> edges = new();

        switch (config.Shape)
        {
            case BoxShape box:
                {
                    var half = new Vector3(box.Width, box.Height, box.Depth) * 0.5f;
                    var localCorners = new[]
                    {
                        new Vector3(-half.X, -half.Y, -half.Z),
                        new Vector3(-half.X, -half.Y, half.Z),
                        new Vector3(-half.X, half.Y, -half.Z),
                        new Vector3(-half.X, half.Y, half.Z),
                        new Vector3(half.X, -half.Y, -half.Z),
                        new Vector3(half.X, -half.Y, half.Z),
                        new Vector3(half.X, half.Y, -half.Z),
                        new Vector3(half.X, half.Y, half.Z)
                    };

                    foreach (var c in localCorners)
                    {
                        vertices.Add(Vector3.Transform(c, pose.Rotation) + pose.Position);
                    }

                    edges.AddRange(
                        [
                            (0, 1), (1, 3), (3, 2), (2, 0),
                            (4, 5), (5, 7), (7, 6), (6, 4),
                            (0, 4), (1, 5), (2, 6), (3, 7)
                        ]
                    );

                    break;
                }
            case MeshShape mesh:
                {
                    for (var i = 0; i < mesh.Vertices.Count; i++)
                    {
                        vertices.Add(Vector3.Transform(mesh.Vertices[i], pose.Rotation) + pose.Position);
                    }

                    var edgeSet = new HashSet<(int, int)>();

                    for (var i = 0; i < mesh.Indices.Count; i += 3)
                    {
                        AddEdge(edgeSet, mesh.Indices[i], mesh.Indices[i + 1]);
                        AddEdge(edgeSet, mesh.Indices[i + 1], mesh.Indices[i + 2]);
                        AddEdge(edgeSet, mesh.Indices[i + 2], mesh.Indices[i]);
                    }

                    edges.AddRange(edgeSet);

                    break;
                }
            case ConvexHullShape hull:
                {
                    for (var i = 0; i < hull.Vertices.Count; i++)
                    {
                        vertices.Add(Vector3.Transform(hull.Vertices[i], pose.Rotation) + pose.Position);
                    }

                    // For visualization, draw edges of a convex hull by connecting each pair of vertices that form a hull edge is non-trivial without the faces.
                    // As a simple fallback, draw a bounding box around hull points.
                    if (vertices.Count > 0)
                    {
                        var min = vertices[0];
                        var max = vertices[0];

                        foreach (var v in vertices)
                        {
                            min = Vector3.Min(min, v);
                            max = Vector3.Max(max, v);
                        }

                        var bbox = new BoundingBox(min, max);
                        var cornersArray = new Vector3[8];
                        bbox.GetCorners(cornersArray);
                        vertices.Clear();
                        vertices.AddRange(cornersArray);

                        edges.AddRange(
                            [
                                (0, 1), (1, 2), (2, 3), (3, 0), // top
                                (4, 5), (5, 6), (6, 7), (7, 4), // bottom
                                (0, 4), (1, 5), (2, 6), (3, 7)  // verticals
                            ]
                        );
                    }

                    break;
                }
            default:
                return;
        }

        if (vertices.Count == 0 || edges.Count == 0)
        {
            return;
        }

        DrawLineList(CollectionsMarshal.AsSpan(vertices), CollectionsMarshal.AsSpan(edges), color, view, projection);
    }

    private void EnsureLineShader()
    {
        _debugLineShader ??= _assetManager.GetShaderProgram("debug_line");
    }

    private void RestoreState()
    {
        _renderContext.OpenGl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
        _renderContext.GraphicsDevice.DepthState = DepthState.None;
        _renderContext.GraphicsDevice.BlendState = BlendState.Opaque;
    }

    private void BuildLightMatrices(DirectionalLight light, Vector3 targetCenter, float orthoSize = 50f)
    {
        _lightViewMatrix = light.GetShadowViewMatrix(targetCenter, distance: 100f);
        _lightProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
            -orthoSize,
            orthoSize,
            -orthoSize,
            orthoSize,
            0.1f,
            200f
        );
    }

    private void RenderShadowPass()
    {
        if (_shadowDepthShader == null)
        {
            return;
        }

        var originalViewport = _renderContext.GraphicsDevice.Viewport;

        _shadowFramebuffer.Bind();
        _renderContext.OpenGl.Clear(ClearBufferMask.DepthBufferBit);
        _renderContext.OpenGl.CullFace(GLEnum.Front);

        foreach (var entity in EntitiesInCullingFrustum)
        {
            if (entity is not IShadowCaster3d shadowCaster)
            {
                continue;
            }

            shadowCaster.DrawShadow(_shadowDepthShader, _lightViewMatrix, _lightProjectionMatrix);
        }

        _renderContext.OpenGl.CullFace(GLEnum.Back);
        _shadowFramebuffer.Unbind();
        _renderContext.GraphicsDevice.Viewport = originalViewport;
    }

    private void PrepareMaterialLitShader(
        Vector3 cameraPosition,
        DirectionalLight[] dirLights,
        PointLight[] pointLights,
        SpotLight[] spotLights,
        bool hasShadowMap
    )
    {
        var materialLit = _assetManager.GetShaderProgram("material_lit");
        materialLit.Uniforms["uCameraPos"].SetValueVec3(cameraPosition);
        materialLit.Uniforms["uLightView"].SetValueMat4(_lightViewMatrix);
        materialLit.Uniforms["uLightProjection"].SetValueMat4(_lightProjectionMatrix);

        ApplyLights(materialLit, dirLights, pointLights, spotLights);

        if (hasShadowMap)
        {
            materialLit.Uniforms["uShadowMap"].SetValueTexture(_shadowFramebuffer.DepthTexture);
        }
        else
        {
            materialLit.Uniforms["uShadowMap"].SetValueTexture(_assetManager.GetWhiteTexture());
        }
    }

    private void ApplyMaterial(
        ShaderProgram materialLit,
        IMaterialCaster caster,
        bool receiveShadows
    )
    {
        var material = caster.Material;

        if (material == null)
        {
            BindDefaultMaterial(materialLit, receiveShadows);

            return;
        }

        var white = _assetManager.GetWhiteTexture();
        var black = _assetManager.GetBlackTexture();

        materialLit.Uniforms["uAlbedoMap"]
                   .SetValueTexture(
                       material.HaveAlbedoTexture ? _assetManager.GetTexture<Texture2D>(material.AlbedoTexture) : white
                   );
        materialLit.Uniforms["uNormalMap"]
                   .SetValueTexture(
                       material.HaveNormalTexture ? _assetManager.GetTexture<Texture2D>(material.NormalTexture) : white
                   );
        materialLit.Uniforms["uRoughnessMap"]
                   .SetValueTexture(
                       material.HaveRoughnessTexture ? _assetManager.GetTexture<Texture2D>(material.RoughnessTexture) : white
                   );
        materialLit.Uniforms["uMetallicMap"]
                   .SetValueTexture(
                       material.HaveMetallicTexture ? _assetManager.GetTexture<Texture2D>(material.MetallicTexture) : white
                   );
        materialLit.Uniforms["uEmissiveMap"]
                   .SetValueTexture(
                       material.HaveEmissiveTexture ? _assetManager.GetTexture<Texture2D>(material.EmissiveTexture) : black
                   );

        materialLit.Uniforms["uTint"].SetValueVec4(material.Tint);
        materialLit.Uniforms["uRoughness"].SetValueFloat(material.Roughness);
        materialLit.Uniforms["uMetallic"].SetValueFloat(material.Metallic);
        materialLit.Uniforms["uEmissiveColor"].SetValueVec3(material.EmissiveColor);
        materialLit.Uniforms["uEmissiveIntensity"].SetValueFloat(material.EmissiveIntensity);

        materialLit.Uniforms["uEnableShadows"].SetValueBool(receiveShadows && material.ReceiveShadows);
    }

    private void BindDefaultMaterial(ShaderProgram materialLit, bool receiveShadows)
    {
        var white = _assetManager.GetWhiteTexture();
        var black = _assetManager.GetBlackTexture();

        materialLit.Uniforms["uAlbedoMap"].SetValueTexture(white);
        materialLit.Uniforms["uNormalMap"].SetValueTexture(white);
        materialLit.Uniforms["uRoughnessMap"].SetValueTexture(white);
        materialLit.Uniforms["uMetallicMap"].SetValueTexture(white);
        materialLit.Uniforms["uEmissiveMap"].SetValueTexture(black);

        materialLit.Uniforms["uTint"].SetValueVec4(Vector4.One);
        materialLit.Uniforms["uRoughness"].SetValueFloat(0.5f);
        materialLit.Uniforms["uMetallic"].SetValueFloat(0.0f);
        materialLit.Uniforms["uEmissiveColor"].SetValueVec3(Vector3.Zero);
        materialLit.Uniforms["uEmissiveIntensity"].SetValueFloat(0.0f);

        materialLit.Uniforms["uEnableShadows"].SetValueBool(receiveShadows);
    }

    private static void ApplyLights(
        ShaderProgram shader,
        DirectionalLight[] dirLights,
        PointLight[] pointLights,
        SpotLight[] spotLights
    )
    {
        shader.Uniforms["uDirectionalLightCount"].SetValueInt(dirLights.Length);

        for (var i = 0; i < dirLights.Length; i++)
        {
            dirLights[i].ApplyToShader(shader, $"uDirectionalLights[{i}]");
        }

        shader.Uniforms["uPointLightCount"].SetValueInt(pointLights.Length);

        for (var i = 0; i < pointLights.Length; i++)
        {
            pointLights[i].ApplyToShader(shader, $"uPointLights[{i}]");
        }

        shader.Uniforms["uSpotLightCount"].SetValueInt(spotLights.Length);

        for (var i = 0; i < spotLights.Length; i++)
        {
            spotLights[i].ApplyToShader(shader, $"uSpotLights[{i}]");
        }
    }
}
