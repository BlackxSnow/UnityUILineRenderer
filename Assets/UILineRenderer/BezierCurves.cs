using System;
using System.Collections.Generic;
using UnityEngine;

namespace UILineRenderer
{
    internal static class BezierCurves
    {
        public static Vector2[] ExtractPositions(in BezierPoint[] controlPoints, bool includeControls = true)
        {
            Vector2[] newPoints = new Vector2[controlPoints.Length * 3 - 2];
            int currentIndex = 0;

            for (int i = 0; i < controlPoints.Length; i++)
            {
                BezierPoint point = controlPoints[i];
                Vector2 pos;
                if(point.TransformAsTarget)
                {
                    pos = point.Target != null ? (Vector2)point.Target.localPosition : new Vector2(0, 0);
                }
                else
                {
                    pos = point.Position;
                }

                if (includeControls && point.PreControl && i != 0)
                {
                    newPoints[currentIndex] = pos + point.PreControlOffset;
                    currentIndex++;
                }

                newPoints[currentIndex] = pos;
                currentIndex++;

                if (includeControls && point.PostControl && i != controlPoints.Length - 1)
                {
                    newPoints[currentIndex] = pos + point.PostControlOffset;
                    currentIndex++;
                }
            }
            Array.Resize(ref newPoints, currentIndex);
            return newPoints;
        }

        public static Vector2[] GetLines(in Vector2[] points)
        {
            Vector2[] newLines = new Vector2[points.Length - 1];
            for (int i = 0; i < points.Length - 1; i++)
            {
                //newLines[i] = points[i + 1] - points[i];
                newLines[i].x = points[i + 1].x - points[i].x;
                newLines[i].y = points[i + 1].y - points[i].y;
            }
            return newLines;
        }

        public static Vector2[] GetPointsOnLines(in Vector2[] points, in Vector2[] lines, in float t)
        {
            Vector2[] newPoints = new Vector2[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                //newPoints[i] = points[i] + lines[i] * t;
                newPoints[i].x = points[i].x + lines[i].x * t;
                newPoints[i].y = points[i].y + lines[i].y * t;

            }
            return newPoints;
        }

        public static Vector2 GetPointOnCurve(in Vector2[] points, in float t)
        {
            Vector2[] newPoints = points;
            while (newPoints.Length > 1)
            {
                newPoints = GetPointsOnLines(newPoints, GetLines(newPoints), t);
            }
            return newPoints[0];
        }

    }
}
