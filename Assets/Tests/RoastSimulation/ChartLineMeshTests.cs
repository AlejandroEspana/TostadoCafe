using NUnit.Framework;
using UnityEngine;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class ChartLineMeshTests
    {
        private const float Tolerance = 1e-5f;

        private static void AssertVertex(Vector3 actual, float x, float y)
        {
            Assert.AreEqual(x, actual.x, Tolerance);
            Assert.AreEqual(y, actual.y, Tolerance);
            Assert.AreEqual(0f, actual.z, Tolerance);
        }

        [Test]
        public void SegmentIsARectangleOfTheGivenWidth()
        {
            var line = new ChartLineMesh();

            line.AddSegment(new Vector2(0f, 0f), new Vector2(2f, 0f), 0.2f);

            Assert.AreEqual(4, line.Vertices.Count);
            Assert.AreEqual(6, line.TriangleIndexCount);
            AssertVertex(line.Vertices[0], 0f, -0.1f);
            AssertVertex(line.Vertices[1], 0f, 0.1f);
            AssertVertex(line.Vertices[2], 2f, 0.1f);
            AssertVertex(line.Vertices[3], 2f, -0.1f);
        }

        [Test]
        public void PolylineJoinsOverlapByHalfTheWidth()
        {
            var line = new ChartLineMesh();

            line.AddPolyline(new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f) }, 0.2f);

            Assert.AreEqual(8, line.Vertices.Count);
            AssertVertex(line.Vertices[0], -0.1f, -0.1f);
            AssertVertex(line.Vertices[2], 1.1f, 0.1f);
        }

        [Test]
        public void DashedSegmentCutsTheLastDash()
        {
            var line = new ChartLineMesh();

            line.AddDashedSegment(new Vector2(0f, 0f), new Vector2(10f, 0f), 0.2f, 4f, 3f);

            Assert.AreEqual(8, line.Vertices.Count, "dos trazos: 0-4 y 7-10");
            AssertVertex(line.Vertices[4], 7f, -0.1f);
            AssertVertex(line.Vertices[7], 10f, -0.1f);
        }

        [Test]
        public void ZeroLengthSegmentsAreSkipped()
        {
            var line = new ChartLineMesh();

            line.AddSegment(Vector2.one, Vector2.one, 0.2f);
            line.AddDashedSegment(Vector2.one, Vector2.one, 0.2f, 4f, 3f);
            line.AddPolyline(new[] { Vector2.one, Vector2.one }, 0.2f);

            Assert.AreEqual(0, line.Vertices.Count);
        }

        [Test]
        public void AppliesToAMeshAndClearsForTheNextDraw()
        {
            var line = new ChartLineMesh();
            var mesh = new Mesh();
            try
            {
                line.AddSegment(Vector2.zero, Vector2.right, 0.2f);
                line.ApplyTo(mesh);
                Assert.AreEqual(4, mesh.vertexCount);
                Assert.AreEqual(6, mesh.GetTriangles(0).Length);

                line.Clear();
                line.ApplyTo(mesh);
                Assert.AreEqual(0, mesh.vertexCount);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
