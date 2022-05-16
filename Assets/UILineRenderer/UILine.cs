using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UILineRenderer
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILine : Graphic
    {
        public enum LineTypeEnum
        {
            PointToPoint,
            PointToPointPolygon,
            Bezier,
            BezierPointToPoint
        }
        public float Size = 5f;
        public float PolygonSize = 10f;
        public int BezierResolution = 10;
        public int PolygonResolution = 8;
        public bool SkipFirstPoly = false;

        public BezierPoint[] BezierControlPoints = default;

        [SerializeField]
        private LineTypeEnum lineType;
        public LineTypeEnum LineType
        {
            get => lineType;
            set {
                lineType = value;
                OnLineTypeChanged?.Invoke(value);
            }
        }
        public Sprite m_Sprite;
        private Color32 Colour32;
        private CanvasRenderer CRenderer;

        private List<BezierPoint> TransformPoints = new List<BezierPoint>();

        [HideInInspector]
        public UnityEvent<LineTypeEnum> OnLineTypeChanged = new UnityEvent<LineTypeEnum>();

        public override Texture mainTexture
        {
            get
            {
                if (m_Sprite == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }

                return m_Sprite.texture;
            }
        }

        protected override void Start()
        {
            base.Start();
            SetAllDirty();
            OnLineTypeChanged.AddListener((_) => SetAllDirty());
            CRenderer = GetComponent<CanvasRenderer>();
        }

        protected override void OnCanvasGroupChanged()
        {
            base.OnCanvasGroupChanged();
            SetAllDirty();
        }

        private void Update()
        {
            if (canvasRenderer.materialCount == 0)
            {
                SetAllDirty();
            }
            if ((TransformPoints?.Count ?? 0) > 0)
            {
                bool isDirty = false;
                foreach (BezierPoint point in TransformPoints)
                {
                    if (point.Target == null)
                    {
                        Debug.LogWarning("TransformAsTarget is set but no transform has been assigned!");
                        continue;
                    }
                    if (point.Position != (Vector2)point.Target.position)
                    {
                        isDirty = true;
                        point.Position = point.Target.position;
                    }
                }
                if (isDirty)
                {
                    SetVerticesDirty();
                }
            }
        }

        public override void SetVerticesDirty()
        {
            base.SetVerticesDirty();
            TransformPoints.Clear();
            TransformPoints = BezierControlPoints?.Where(p => p.TransformAsTarget)?.ToList() ?? new List<BezierPoint>();
        }

        private Vector2[] ConstructBezier(int resolution, params BezierPoint[] points)
        {
            Vector2[] positions = BezierCurves.ExtractPositions(points);

            Vector2[] lineVerts = new Vector2[resolution + 2];
            lineVerts[0] = positions[0];
            lineVerts[lineVerts.Length - 1] = positions[positions.Length - 1];

            float tIncrement = 1f / (resolution + 1);

            Parallel.For(0, resolution, i => lineVerts[i + 1] = BezierCurves.GetPointOnCurve(positions, tIncrement * (i + 1)));

            //Parallel.For(0, resolution - 1, () =>
            //{
            //    return new Vector2[lineVerts.Length];
            //},
            //(i, loopState, localArray) =>
            //{
            //    localArray[i] = BezierCurves.GetPointOnCurve(positions, tIncrement * (i + 1));
            //    return localArray;
            //},
            //localArray =>
            //{
            //    lock (lineVerts)
            //    {
            //        for (int i = 0; i < resolution; i++)
            //        {
            //            lineVerts[i + 1] += localArray[i];
            //        }
            //    }
            //});

            //for (int i = 0; i < resolution; i++)
            //{
            //    lineVerts[i + 1] = BezierCurves.GetPointOnCurve(positions, tIncrement * (i + 1));
            //}
            return lineVerts;
        }

        protected override void OnDisable()
        {
            SetAllDirty();
            GetComponent<CanvasRenderer>().Clear();
            
        }
        protected override void OnEnable()
        {
            SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!isActiveAndEnabled)
            {
                return;
            }
            if ((BezierControlPoints?.Length ?? 0) == 0) return;
            Colour32 = color;
            Vector2[] verts;
            switch (LineType)
            {
                case LineTypeEnum.PointToPoint:
                    verts = BezierCurves.ExtractPositions(BezierControlPoints, false);
                    PopulateVHBezier(verts, vh, null, true);
                    break;
                case LineTypeEnum.PointToPointPolygon:
                    verts = BezierCurves.ExtractPositions(BezierControlPoints, false);
                    PopulateVHPolygon(verts, vh, SkipFirstPoly);
                    break;
                case LineTypeEnum.Bezier:
                    verts = ConstructBezier(BezierResolution, BezierControlPoints);
                    PopulateVHBezier(verts, vh);
                    break;
                case LineTypeEnum.BezierPointToPoint:
                    List<Vector2> bezierPTPVerts = new List<Vector2>();
                    List<int> majorIndices = new List<int>();
                    int lastCount = 0;
                    for (int i = 0; i < BezierControlPoints.Length - 1; i++)
                    {
                        bezierPTPVerts.AddRange(ConstructBezier(Mathf.CeilToInt((float)BezierResolution / (BezierControlPoints.Length - 1)), BezierControlPoints[i], BezierControlPoints[i + 1]));
                        if (i != BezierControlPoints.Length - 2)
                        {
                            bezierPTPVerts.RemoveAt(bezierPTPVerts.Count - 1);
                        }
                        else
                        {
                            majorIndices.Add(bezierPTPVerts.Count - 1);
                        }
                        if (i != 0)
                        {
                            majorIndices.Add(lastCount);
                        }
                        lastCount = bezierPTPVerts.Count;
                    }
                    PopulateVHBezier(bezierPTPVerts.ToArray(), vh, majorIndices.ToArray());
                    break;
                default:
                    break;
            }
        }

        protected void ExtrudeLine(Vector2 point, Vector2 direction, float xUV0, float amount, VertexHelper vh)
        {
            vh.AddVert(new UIVertex
            {
                position = point + direction * amount,
                color = Colour32,
                uv0 = new Vector2(xUV0, 0)
            });

            vh.AddVert(new UIVertex
            {
                position = point - direction * amount,
                color = Colour32,
                uv0 = new Vector2(xUV0, 1)
            });

        }

        protected void DebugSquare(Vector2 point, float size)
        {
            point = VectorUtil.FlipY(point);

            Rect rect = new Rect(point.x - size / 2f, point.x - size / 2f, size, size);
            Graphics.DrawTexture(new Rect(100, 100, 500, 500), Texture2D.redTexture);
        }

        protected void PopulateVHBezier(Vector2[] linePoints, VertexHelper vh, int[] majorIndices = null, bool allMajor = false)
        {
            Vector2 dirToNext = linePoints[1] - linePoints[0];
            Vector2 dirFromLast;
            dirToNext.UNormalize();
            Vector2 extrudeDir = dirToNext.Perpendicular();

            ExtrudeLine(linePoints[0], extrudeDir, 0, Size, vh);

            float uvIncrement = 1f / linePoints.Length;

            for (int i = 1; i < linePoints.Length - 1; i++)
            {
                dirFromLast = linePoints[i] - linePoints[i - 1];
                dirFromLast.UNormalize();
                dirToNext = linePoints[i + 1] - linePoints[i];
                dirToNext.UNormalize();
                extrudeDir = ((dirFromLast + dirToNext) / 2f).Perpendicular();
                extrudeDir.UNormalize();

                float lineWidth = Size;
                if (allMajor || (LineType != LineTypeEnum.Bezier && (majorIndices?.Contains(i) ?? false)))
                {
                    Vector2 extrudeLastDir = linePoints[i] - (dirFromLast.Perpendicular() * Size);
                    Vector2 extrudeNextDir = linePoints[i] - (dirToNext.Perpendicular() * Size);
                    float m_last = dirFromLast.y / dirFromLast.x;
                    float m_next = dirToNext.y / dirToNext.x;
                    float b_last = m_last * -extrudeLastDir.x + extrudeLastDir.y;
                    float b_next = m_next * -extrudeNextDir.x + extrudeNextDir.y;

                    Vector2 Intercept = new Vector2();
                    if ((dirFromLast.x == 0 && dirToNext.x == 0) || (dirFromLast.y == 0 && dirToNext.y == 0))
                    {
                        Intercept = extrudeLastDir;
                    }
                    else if (dirFromLast.x == 0)
                    {
                        Intercept.x = extrudeLastDir.x;
                        Intercept.y = m_next * Intercept.x + b_next;//y = mx+b
                    }
                    else if (dirToNext.x == 0)
                    {
                        Intercept.x = extrudeNextDir.x;
                        Intercept.y = m_last * Intercept.x + b_last;
                    }
                    else
                    {
                        Intercept.x = (b_next - b_last) / (m_last - m_next);
                        Intercept.y = m_last * Intercept.x + b_last;
                    }


                    Vector2 dirToVert = linePoints[i] - Intercept;

                    lineWidth = dirToVert.magnitude;
                }

                ExtrudeLine(linePoints[i], extrudeDir, uvIncrement * i, lineWidth, vh);
                vh.AddTriangle(i * 2 - 2, i * 2, i * 2 - 1);
                vh.AddTriangle(i * 2, i * 2 + 1, i * 2 - 1);
            }

            int c = linePoints.Length - 1;

            dirFromLast = linePoints[c] - linePoints[c - 1];
            dirFromLast.UNormalize();
            extrudeDir = dirFromLast.Perpendicular();

            ExtrudeLine(linePoints[c], extrudeDir, 1, Size, vh);

            vh.AddTriangle(c * 2 - 2, c * 2, c * 2 - 1);
            vh.AddTriangle(c * 2, c * 2 + 1, c * 2 - 1);
        }

        protected void PopulateVHPolygon(Vector2[] points, VertexHelper vh, bool skipFirstPoly = true)
        {
            float uvIncrement = 1f / points.Length;
            int lastIndex = 0;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 dirToNext = points[i+1] - points[i];
                dirToNext.UNormalize();
                Vector2 extrudeDir = dirToNext.Perpendicular();

                ExtrudeLine(points[i], extrudeDir, uvIncrement * i, Size, vh);
                ExtrudeLine(points[i + 1], extrudeDir, uvIncrement * (i+1), Size, vh);
                vh.AddTriangle(i * 4, i * 4 + 1, i * 4 + 2);
                vh.AddTriangle(i * 4 + 1, i * 4 + 2, i * 4 + 3);
                lastIndex = i * 4 + 3;
            }

            for (int i = 0; i < (skipFirstPoly ? points.Length - 1: points.Length); i++)
            {
                int centerIndex = lastIndex + 1 + i * (PolygonResolution + 1);
                //Center of fan
                vh.AddVert(new UIVertex
                {
                    position = points[skipFirstPoly ? i + 1 : i],
                    color = Colour32,
                    uv0 = new Vector2(0.5f, 0.5f)
                });

                float angle = (360f / PolygonResolution) * Mathf.PI/180;
                for (int v = 0; v < PolygonResolution; v++)
                {
                    Vector2 offsetPos = new Vector2(0 * Mathf.Cos(angle * v) - PolygonSize * Mathf.Sin(angle * v), 0 * Mathf.Sin(angle * v) + PolygonSize * Mathf.Cos(angle * v));
                    vh.AddVert(new UIVertex
                    {
                        position = offsetPos + points[skipFirstPoly ? i + 1 : i],
                        color = Colour32,
                        uv0 = new Vector2(0.5f, 0.5f)
                        //uv0 = new Vector2((offsetPos.x + Size) / (Size * 2), (offsetPos.y + Size) / (Size * 2))
                    });
                }

                for (int v = 0; v < PolygonResolution; v++)
                {
                    vh.AddTriangle(centerIndex, centerIndex + 1 + v, v+1 >= PolygonResolution ? centerIndex + 1 : centerIndex + 2 + v);
                }

            }
        }
    }

}