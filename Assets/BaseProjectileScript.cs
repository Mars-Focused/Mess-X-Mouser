using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseProjectileScript : MonoBehaviour
{
    private Rigidbody rb;
    private float Lifetime = 2.5f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Invoke("DestroyGameObject", Lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Ran into Object");
        DestroyGameObject();
    }

    private void DestroyGameObject()
    {
        Debug.Log("Destroying Projectile");
        Destroy(gameObject);
    }
}
