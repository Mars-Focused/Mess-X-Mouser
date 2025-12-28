using Assets.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseProjectileScript : MonoBehaviour
{
    private Rigidbody rb;
    private float Lifetime = 2.5f;
    private int damage;
    private string teamTag;
    public Collider bulletCollider;
    public Collider playerCollider;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Invoke("DestroyGameObject", Lifetime);
    }


    public void SetDamage(int assignedDamage)
    {
        damage = assignedDamage;
    }

    public void SetTeam(string team)
    {
        teamTag = team;
    }

    private void OnCollisionEnter(Collision collision)
    {
        /*
        if (teamTag == "Player")
        {
            Physics.IgnoreCollision(playerCollider, bulletCollider);
        }
        */

        if (collision.gameObject.tag != teamTag)
        {
            var targetScript = collision.gameObject.GetComponent<IDamageable>();

            if (targetScript != null)
            {
                // Debug.Log("Dealt Damage");
                targetScript.Damage(damage);
            }
            DestroyGameObject();
        }
    }

    private void DestroyGameObject()
    {
        //Debug.Log("Destroying Projectile");
        Destroy(gameObject);
    }
}
