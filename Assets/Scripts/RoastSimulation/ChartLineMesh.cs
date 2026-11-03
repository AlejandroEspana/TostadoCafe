using System.Collections.Generic;
using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Arma líneas como franjas de triángulos en el plano XY. A diferencia de un LineRenderer, el ancho se escala
    // con la estación, así la línea se ve igual de gruesa con cualquier escala.
    public sealed class ChartLineMesh
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();

        public IReadOnlyList<Vector3> Vertices => vertices;
        public int TriangleIndexCount => triangles.Count;

        public void Clear()
        {
            vertices.Clear();
            triangles.Clear();
        }

        // Cada tramo se alarga medio ancho en sus puntas para que las uniones no dejen huecos.
        public void AddPolyline(IReadOnlyList<Vector2> points, float width)
        {
            for (var index = 1; index < points.Count; index++)
            {
                AddQuad(points[index - 1], points[index], width, width / 2f);
            }
        }

        public void AddSegment(Vector2 from, Vector2 to, float width)
        {
            AddQuad(from, to, width, 0f);
        }

        public void AddDashedSegment(Vector2 from, Vector2 to, float width, float dash, float gap)
        {
            var length = Vector2.Distance(from, to);
            if (length <= 0f || dash <= 0f)
            {
                return;
            }

            var direction = (to - from) / length;
            for (var start = 0f; start < length; start += dash + gap)
            {
                AddQuad(from + direction * start, from + direction * Mathf.Min(start + dash, length), width, 0f);
            }
        }

        public void ApplyTo(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private void AddQuad(Vector2 from, Vector2 to, float width, float extension)
        {
            var delta = to - from;
            var length = delta.magnitude;
            if (length <= 0f)
            {
                return;
            }

            var direction = delta / length;
            var normal = new Vector2(-direction.y, direction.x) * (width / 2f);
            var start = from - direction * extension;
            var end = to + direction * extension;
            var first = vertices.Count;

            vertices.Add(start - normal);
            vertices.Add(start + normal);
            vertices.Add(end + normal);
            vertices.Add(end - normal);

            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }
    }
}
