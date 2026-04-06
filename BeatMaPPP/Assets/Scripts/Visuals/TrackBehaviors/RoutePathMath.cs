using System.Collections.Generic;
using UnityEngine;

public static class RoutePathMath
{
    public static float GetPathLength(IList<Vector3> points)
    {
        if (points == null || points.Count < 2)
        {
            return 0f;
        }

        float total = 0f;
        for (int i = 0; i < points.Count - 1; i++)
        {
            total += Vector3.Distance(points[i], points[i + 1]);
        }

        return total;
    }

    public static float ProjectDistance(IList<Vector3> points, Vector3 worldPoint)
    {
        if (points == null || points.Count < 2)
        {
            return 0f;
        }

        float bestDistSqr = float.MaxValue;
        float bestAlong = 0f;
        float accumulated = 0f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            Vector3 ab = b - a;
            float segLen = ab.magnitude;

            if (segLen <= 0.0001f)
            {
                continue;
            }

            Vector3 dir = ab / segLen;
            float tMeters = Mathf.Clamp(Vector3.Dot(worldPoint - a, dir), 0f, segLen);
            Vector3 projected = a + dir * tMeters;
            float distSqr = (worldPoint - projected).sqrMagnitude;

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestAlong = accumulated + tMeters;
            }

            accumulated += segLen;
        }

        return bestAlong;
    }

    public static Vector3 SampleAtDistance(IList<Vector3> points, float distance)
    {
        if (points == null || points.Count == 0)
        {
            return Vector3.zero;
        }

        if (points.Count == 1)
        {
            return points[0];
        }

        float remaining = Mathf.Max(0f, distance);

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            float segLen = Vector3.Distance(a, b);

            if (segLen <= 0.0001f)
            {
                continue;
            }

            if (remaining <= segLen)
            {
                float t = remaining / segLen;
                return Vector3.Lerp(a, b, t);
            }

            remaining -= segLen;
        }

        return points[points.Count - 1];
    }

    public static Vector3 TangentAtDistance(IList<Vector3> points, float distance, float lookAhead = 0.25f)
    {
        Vector3 p0 = SampleAtDistance(points, distance);
        Vector3 p1 = SampleAtDistance(points, distance + Mathf.Max(lookAhead, 0.05f));
        Vector3 tangent = p1 - p0;

        if (tangent.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        return tangent.normalized;
    }
}
