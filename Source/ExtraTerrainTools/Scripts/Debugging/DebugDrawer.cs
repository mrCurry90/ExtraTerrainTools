using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System.Collections.Generic;
using System;
using System.Linq;

namespace TerrainTools.Debugging
{
    public static class DebugDrawer
    {
        private const float ARROW_ANGLE = 30;
        public enum Shape
        {
            Line,
            Arrow
        }

        private class ShapeParam
        {
            public Shape Shape { get; private set; }
            public float3 Start { get; private set; }
            public float3 End { get; private set; }
            public Color Color { get; private set; }
            public float TimeLeft { get; set; }

            public ShapeParam(Shape shape, float3 start, float3 end, Color color, float duration)
            {
                Shape = shape;
                Start = start;
                End = end;
                Color = color;
                TimeLeft = duration;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Shape, Start, End, Color);
            }
        }

        private static bool _Registered = false;
        private static HashSet<ShapeParam> _Shapes = new();
        private static Material _LineMaterial;
        private static void CreateLineMaterial()
        {
            if (!_LineMaterial)
            {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _LineMaterial = new(shader) { hideFlags = HideFlags.HideAndDontSave };
                // Turn on alpha blending
                // _LineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                // _LineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                _LineMaterial.SetInt("_Cull", (int)CullMode.Off);
                // Turn off depth writes
                _LineMaterial.SetInt("_ZWrite", 0);
            }
        }

        public static void Add(Shape shape, float3 from, float3 to, Color color, float duration = 0)
        {
            if (!_Registered)
            {
                RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
                _Registered = true;
            }
                
            _Shapes.Add( new(
                shape, from, to, color, duration
            ));
        }

        private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            Debug.Log("Shapes to render: " + _Shapes.Count);
            if (_Shapes.Count > 0)
            {
                var item = _Shapes.First();
                Debug.Log(string.Format(
                    "{0} from {1} to {2} color {3} for {4}",
                    item.Shape,
                    item.Start,
                    item.End,
                    item.Color,
                    item.TimeLeft
                ));
            }
            CreateLineMaterial();


            //Debug
            GL.PushMatrix();
            _LineMaterial.SetPass(0);
            GL.LoadPixelMatrix();
            GL.Begin(GL.LINES);
            GL.Color(Color.red);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, Screen.height / 2, 0);
            GL.End();
            GL.PopMatrix();
            // Debug

            // GL.PushMatrix();
            // _LineMaterial.SetPass(0);
            // GL.LoadProjectionMatrix(camera.projectionMatrix);
            // GL.modelview = camera.worldToCameraMatrix;

            // // Draw lines - Primitives to draw: can be TRIANGLES, TRIANGLE_STRIP, QUADS or LINES.
            // GL.Begin(GL.LINES);
            // foreach (ShapeParam shape in _Shapes.ToArray())
            // {
            //     GL.Color(shape.Color);

            //     switch (shape.Shape)
            //     {
            //         case Shape.Line:
            //             DrawLine(shape.Start, shape.End);
            //             break;
            //         case Shape.Arrow:
            //             DrawArrow(shape.Start, shape.End);
            //             break;
            //     }

            //     if (shape.TimeLeft > 0)
            //     {
            //         Debug.Log("deltaTime: " + Time.deltaTime);
            //         Debug.Log("unscaledDeltaTime: " + Time.unscaledDeltaTime);

            //         shape.TimeLeft -= Time.unscaledDeltaTime;
            //     }
            //     else
            //     {
            //         _Shapes.Remove(shape);
            //     }
            // }

            // GL.End();
            // GL.PopMatrix();

            if (_Shapes.Count == 0)
            {
                RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
                _Registered = false;
            }
        }

        private static void DrawShape()
        {
            
        }

        private static void DrawLine(float3 start, float3 end)
        {
            GL.Vertex3(start.x, start.y, start.z);
            GL.Vertex3(end.x, end.y, end.z);
        }

        private static void DrawArrow(float3 start, float3 end)
        {
            float3 arrowNorm = math.normalize(end - start);

            float3 rotAxis = math.cross(arrowNorm,
                math.normalize(
                    math.cross(arrowNorm, math.up())
                )
            );

            float3 wingStep = -arrowNorm;
            float3 left = end + math.rotate(quaternion.AxisAngle(rotAxis, ARROW_ANGLE), wingStep);
            float3 right = end + math.rotate(quaternion.AxisAngle(rotAxis, -ARROW_ANGLE), wingStep);

            DrawLine(start, end);
            DrawLine(left, end);
            DrawLine(right, end);
        }
    }
}
