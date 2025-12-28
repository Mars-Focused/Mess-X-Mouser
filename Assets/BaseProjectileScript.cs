using Assets.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseProjectileScript : MonoBehaviour
{
    private Rigidbody rb;
    private float Lifetime = 2.5f;
    public int damage;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Invoke("DestroyGameObject", Lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Debug.Log(collision.gameObject.GetComponent<IDamageable>());

        /*
        if (collision.gameObject.name != this.name)
        {
            DestroyGameObject();
            Debug.Log("ran Into a " + collision.gameObject.name);
        }
        */

        var targetScript = collision.gameObject.GetComponent<IDamageable>(); // Change to a seperate Health component.

        if (targetScript != null)
        {
            // Debug.Log("Dealt Damage");
            targetScript.Damage(damage);
        }
        
        DestroyGameObject();
    }


    private void DestroyGameObject()
    {
        //Debug.Log("Destroying Projectile");
        Destroy(gameObject);
    }
}
