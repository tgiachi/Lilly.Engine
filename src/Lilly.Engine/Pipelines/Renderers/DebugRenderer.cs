using System.Numerics;
using System.Runtime.InteropServices;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Physics;
using Lilly.Engine.Interfaces.Physics;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Vertexts;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Primitives;
using Silk.NET.OpenGL;
using TrippyGL;
using PrimitiveType = TrippyGL.PrimitiveType;

namespace Lilly.Engine.Pipelines.Renderers;

public class DebugRenderer : IDisposable
{
    private readonly RenderContext _renderContext;
    private readonly IAssetManager _assetManager;

    public bool DrawBoundingBoxes { get; set; }
    public bool DrawPhysicsShapes { get; set; } = true;
    public Color4b BoundsColor { get; set; } = Color4b.Green;
    public Color4b PhysicsColor { get; set; } = Color4b.Magenta;
    public float LineWidth { get; set; } = 1.5f;

    private ShaderProgram? _debugLineShader;
    private VertexBuffer<PositionVertex>? _lineVbo;

    public DebugRenderer(RenderContext renderContext, IAssetManager assetManager)
    {
        _renderContext = renderContext;
        _assetManager = assetManager;
    }

    public void Render(List<IGameObject3d> entities, ICamera3D camera)
    {
        if (camera == null || (!DrawBoundingBoxes && !DrawPhysicsShapes))
        {
            return;
        }

        EnsureLineShader();
        _renderContext.OpenGl.LineWidth(LineWidth);

        foreach (var entity in entities)
        {
            if (DrawBoundingBoxes)
            {
                DrawBoundingBox(entity.BoundingBox, BoundsColor, camera.View, camera.Projection);
            }

            if (DrawPhysicsShapes && entity is IPhysicsGameObject3d physicsGameObject)
            {
                DrawPhysicsShape(physicsGameObject, PhysicsColor, camera.View, camera.Projection);
            }
        }

        _renderContext.OpenGl.LineWidth(1f);
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

    private static void AddEdge(HashSet<(int, int)> edges, int a, int b)
    {
        if (a == b)
        {
            return;
        }

        var key = a < b ? (a, b) : (b, a);
        edges.Add(key);
    }

    private void EnsureLineShader()
    {
        _debugLineShader ??= _assetManager.GetShaderProgram("debug_line");
    }

    public void Dispose()
    {
        _lineVbo?.Dispose();
        GC.SuppressFinalize(this);
    }
}
