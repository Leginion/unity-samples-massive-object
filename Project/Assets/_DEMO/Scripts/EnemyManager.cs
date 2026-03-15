using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    private static readonly List<Transform> _transforms = new();

    public static void Add(Transform t)
    {
        _transforms.Add(t);
    }

    public static void Remove(Transform t)
    {
        _transforms.Remove(t);
    }

    public static void NotifyUpdate(float deltaTime)
    {
        Vector3 deltaMove = Vector3.forward * deltaTime;
        foreach (Transform t in _transforms)
        {
            SimpleEnemyMovementNoMono.Move(t, deltaMove);
        }
    }

    private void Update()
    {
        NotifyUpdate(Time.deltaTime);
    }
}
