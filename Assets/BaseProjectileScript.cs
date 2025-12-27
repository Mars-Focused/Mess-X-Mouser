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
        if (collision.gameObject.name != this.name)
        {
            DestroyGameObject();
            Debug.Log("ran Into a " + collision.gameObject.name);
        }

        if (collision.gameObject.tag == "Player" || collision.gameObject.tag == "Enemy")
        {
            var targetScript = collision.gameObject.GetComponent<Enemy_AI2>(); // Change to a seperate Health component.
            if (targetScript != null)
            {
                Debug.Log("Dealt Damage");
                targetScript.Damage(damage);
            }
        }
        DestroyGameObject();
    }


    private void DestroyGameObject()
    {
        //Debug.Log("Destroying Projectile");
        Destroy(gameObject);
    }
}
