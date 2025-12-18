using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PreefabCircleSpawner : MonoBehaviour
{
    [SerializeField] CircleSpawnData[] spawnData;
    /*
    [SerializeField] GameObject prefab;
    [SerializeField] int numberOfObjects;
    [SerializeField] float radius;
    */
    [ContextMenu("GenerateStructures")]
    /*
    [SerializeField] GameObject prefab;
    [SerializeField] int numberOfObjects;
    [SerializeField] float radius;
    */
    private void Start()
    {
        GenerateStructures();
    }
    void GenerateStructures()
    {
        for (int i = 0; i < spawnData.Length; i++)
        {
            int numberOfPrefabs = spawnData[i].numberOfObjects;
            float radius = spawnData[i].radius;
            GameObject prefab = spawnData[i].prefab.gameObject;
            for (int j = 0; j < numberOfPrefabs; j++)
            {
                float angle = j * Mathf.PI * 2 / numberOfPrefabs;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                Vector3 pos = transform.position + new Vector3(x, 0, z);
                float angleDegrees = -angle * Mathf.Rad2Deg;
                Quaternion rot = Quaternion.Euler(0, angleDegrees, 0);
                Instantiate(prefab, pos, rot);
            }
        }
    }
}
