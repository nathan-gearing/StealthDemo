using UnityEngine;

public static class DebugExtension
{
    public static void DrawCircle(Vector3 center, Vector3 up, Color color, float radius, int segments = 24)
    {
        Vector3 lastPoint = center + (Quaternion.AngleAxis(0, up) * Vector3.forward) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = 360f * i / segments;
            Vector3 nextPoint = center + (Quaternion.AngleAxis(angle, up) * Vector3.forward) * radius;
            Debug.DrawLine(lastPoint, nextPoint, color, 0.2f);
            lastPoint = nextPoint;
        }
    }
}