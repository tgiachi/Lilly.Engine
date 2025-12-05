using System.Numerics;
using System.Collections.Generic;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Physics;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Entities.Transparent;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Types;
using Lilly.Rendering.Core.Primitives;
using Lilly.Engine.Vertexts;
using Lilly.Engine.Interfaces.Physics;
using Lilly.Engine.Interfaces.Services;
using Serilog;
using Silk.NET.OpenGL;
using TrippyGL;
using System.Runtime.InteropServices;

namespace Lilly.Engine.Pipelines;

public class ThreeDLayer : BaseRenderLayer<IGameObject3d>
{
    private readonly RenderContext _renderContext;
    private readonly IAssetManager _assetManager;

    public bool IsWireframe { get; set; }
    public float WireframeLineWidth { get; set; } = 1.0f;

    private readonly ICamera3dService _camera3dService;
    public bool DebugDrawBoundingBoxes { get; set; } = false;
    public bool DebugDrawPhysicsShapes { get; set; } = true;
    public Color4b DebugBoundsColor { get; set; } = Color4b.Green;
    public Color4b DebugPhysicsColor { get; set; } = Color4b.Magenta;
    public float DebugLineWidth { get; set; } = 1.5f;

    private ShaderProgram? _debugLineShader;
    private VertexBuffer<PositionVertex>? _lineVbo;

    public List<IGameObject3d> EntitiesInCullingFrustum { get; } = new();

    public List<IGameObject3d> EntitiesOutsideCullingFrustum { get; } = new();

    public ThreeDLayer(
        RenderContext renderContext,
        ICamera3dService camera3dService,
        IAssetManager assetManager
    ) : base("ThreeD", RenderPriority.ThreeD)
    {
        _renderContext = renderContext;
        _camera3dService = camera3dService;
        _assetManager = assetManager;
    }

    public override void Initialize()
    {
        _camera3dService.UpdateViewport(_renderContext.GraphicsDevice.Viewport);
        base.Initialize();
    }

    public override void Render(GameTime gameTime)
    {
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

        // 3. Draw Opaque Pass (Front-to-Back)
        foreach (var entity in EntitiesInCullingFrustum)
        {
            entity.Draw(gameTime, _renderContext.GraphicsDevice, _camera3dService.ActiveCamera);

            ProcessedEntityCount++;
        }

        // 4. Draw Transparent Pass (Back-to-Front)
        for (int i = EntitiesInCullingFrustum.Count - 1; i >= 0; i--)
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

    private void RestoreState()
    {
        _renderContext.OpenGl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
        _renderContext.GraphicsDevice.DepthState = DepthState.None;
        _renderContext.GraphicsDevice.BlendState = BlendState.Opaque;
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

    private void EnsureLineShader()
    {
        _debugLineShader ??= _assetManager.GetShaderProgram("debug_line");
    }

    private void DrawBoundingBox(BoundingBox bounds, Vector4 color, Matrix4x4 view, Matrix4x4 projection)
    {
        var cornersArray = new Vector3[8];
        bounds.GetCorners(cornersArray);
        var corners = cornersArray.AsSpan();
        var edges = new (int, int)[]
        {
            (0, 1), (1, 2), (2, 3), (3, 0), // top
            (4, 5), (5, 6), (6, 7), (7, 4), // bottom
            (0, 4), (1, 5), (2, 6), (3, 7)  // verticals
        };

        DrawLineList(corners, edges, color, view, projection);
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
                        new[]
                        {
                            (0, 1), (1, 3), (3, 2), (2, 0),
                            (4, 5), (5, 7), (7, 6), (6, 4),
                            (0, 4), (1, 5), (2, 6), (3, 7)
                        }
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
            default:
                return;
        }

        if (vertices.Count == 0 || edges.Count == 0)
        {
            return;
        }

        DrawLineList(CollectionsMarshal.AsSpan(vertices), CollectionsMarshal.AsSpan(edges), color, view, projection);
    }

    private static void AddEdge(HashSet<(int, int)> edges, int a, int b)
    {
        if (a == b)
            return;

        var key = a < b ? (a, b) : (b, a);
        edges.Add(key);
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

        for (int i = 0; i < edges.Length; i++)
        {
            var edge = edges[i];
            lineVertices[i * 2] = new PositionVertex(vertices[edge.Item1]);
            lineVertices[i * 2 + 1] = new PositionVertex(vertices[edge.Item2]);
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
        _renderContext.GraphicsDevice.DrawArrays(TrippyGL.PrimitiveType.Lines, 0, (uint)lineVertices.Length);
    }
}
