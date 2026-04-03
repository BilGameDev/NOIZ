using UnityEngine;

public static class SwipeDirectionHelper
{
    public static CutDirection GetDirectionFromVector(Vector2 dir)
    {
        dir.Normalize();

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? CutDirection.Right : CutDirection.Left;
        else
            return dir.y > 0 ? CutDirection.Up : CutDirection.Down;
    }

    public static bool IsCorrectCut(CutDirection required, Vector2 swipeDir, float toleranceDegrees = 35f)
    {
        Vector2 target = required switch
        {
            CutDirection.Up => Vector2.up,
            CutDirection.Down => Vector2.down,
            CutDirection.Left => Vector2.left,
            CutDirection.Right => Vector2.right,
            _ => Vector2.up
        };

        float angle = Vector2.Angle(target, swipeDir.normalized);
        return angle <= toleranceDegrees;
    }

    public enum CutDirection
    {
        Up,
        Down,
        Left,
        Right
    }
}