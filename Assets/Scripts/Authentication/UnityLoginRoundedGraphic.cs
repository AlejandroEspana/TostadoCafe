using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ToastedVr.Authentication
{
    public sealed class UnityLoginRoundedGraphic : MaskableGraphic
    {
        [SerializeField] private float cornerRadius = 24f;
        [SerializeField] private int cornerSegments = 10;

        public void Configure(Color graphicColor, float radius)
        {
            color = graphicColor;
            cornerRadius = radius;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            var rect = rectTransform.rect;
            var radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
            var points = new List<Vector2>();
            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), -90f, 0f, radius);
            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), 0f, 90f, radius);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), 90f, 180f, radius);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), 180f, 270f, radius);

            vertexHelper.AddVert(rect.center, color, Vector2.zero);
            for (var index = 0; index < points.Count; index++)
            {
                vertexHelper.AddVert(points[index], color, Vector2.zero);
            }

            for (var index = 0; index < points.Count; index++)
            {
                var nextIndex = index == points.Count - 1 ? 1 : index + 2;
                vertexHelper.AddTriangle(0, index + 1, nextIndex);
            }
        }

        private void AddCorner(List<Vector2> points, Vector2 center, float startAngle, float endAngle, float radius)
        {
            for (var segment = 0; segment <= cornerSegments; segment++)
            {
                var progress = segment / (float)cornerSegments;
                var angle = Mathf.Lerp(startAngle, endAngle, progress) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }
    }
}
