using UnityEngine;

namespace UILineRenderer
{
    public static class VectorUtil
    {
        public static Vector2 FlipY(Vector2 vector)
        {
            return new Vector2(vector.x, Screen.height - vector.y);
        }
        public static Vector2 Perpendicular(this Vector2 a)
        {
            Vector2 b;
            if (a.x != 0)
            {
                b = new Vector2(0, a.x);
                b.x = -(a.y * b.y) / a.x;
            }
            else /*if (a.y != 0)*/
            {
                b = new Vector2(a.y, 0);
                b.y = -(a.x * b.x) / a.y;
                b = -b;
            }
            //else
            //{
            //    throw new System.ArgumentException("Input vector was 0");
            //}
            b.UNormalize();
            b *= a.magnitude;
            return b;
        }

        public static void UNormalize(this ref Vector2 v)
        {
            //if (v.x == 0 && v.y == 0)
            //{
            //    throw new System.ArgumentException("Input vector was 0");
            //}
            Vector2 result = v;
            result /= Mathf.Sqrt(Mathf.Pow(v.x, 2) + Mathf.Pow(v.y, 2));
            v = result;
        }
    }
}
