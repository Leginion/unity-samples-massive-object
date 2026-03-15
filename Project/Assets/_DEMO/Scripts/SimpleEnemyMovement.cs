using System;
using UnityEngine;

public class SimpleEnemyMovement : MonoBehaviour
{
    private Transform _t;

    private void Awake()
    {
        _t = GetComponent<Transform>();
    }

    private void Update()
    {
        _t.Translate(Vector3.forward * Time.deltaTime);
    }
}
