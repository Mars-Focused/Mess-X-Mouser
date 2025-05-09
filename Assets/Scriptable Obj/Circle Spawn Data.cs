using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CircleSpawnData", menuName = "ScriptableObjects/CircleSpawnData")]
public class CircleSpawnData : ScriptableObject
{
    public GameObject prefab;
    public int numberOfObjects;
    public float radius;
}
