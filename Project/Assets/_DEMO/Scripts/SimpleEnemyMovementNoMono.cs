using UnityEngine;

public static class SimpleEnemyMovementNoMono
{
    public static void Move(Transform transform, Vector3 movement)
    {
        transform.localPosition += movement;
    }
}
