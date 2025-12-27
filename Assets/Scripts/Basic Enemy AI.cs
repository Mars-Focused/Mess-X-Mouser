using UnityEngine;
using UnityEngine.AI;
using Assets.Interfaces;

public class Enemy_AI2 : MonoBehaviour, IDamageable 
{
    public Transform player;
    public GameObject projectile;
    public Transform attackPoint;
    public float speed;
    public float attackingDistance;
    public float retreatDistance;
    public float attackCooldown;
    public float projectileSpeed;
    public float health = 3;
    public bool readyToAttack;

    private Vector3 direction;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        readyToAttack = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (ToFar())
        {
            Pursue();
        }
        else if (ToClose())
        {
            Retreat();
        }
        else
        {
            AttackPlayer();
        }
    }

    private bool ToFar()
    {
        if (DistanceToPlayer() > attackingDistance) { return true; } else { return false; }
    }

    private bool ToClose()
    {
        if (DistanceToPlayer() < retreatDistance) { return true; } else { return false; }
    }

    private float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, player.position);
    }

    private void Pursue()
    {
        transform.position = Vector3.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
    }

    private void Retreat()
    {
        transform.position = Vector3.MoveTowards(transform.position, player.position, -speed * Time.deltaTime);
    }

    private void SetAttack()
    {
        readyToAttack = false;
    }

    private void ResetAttack()
    {
        readyToAttack = true;
    }

    private void AttackPlayer()
    {
        Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(targetPosition);
        // Attack code goes here
        if (readyToAttack)
        {
            SetAttack();
            Invoke(nameof(ResetAttack), attackCooldown);
            // Calculate direction from attackPoint to targetPoint
            direction = player.position - attackPoint.position;

            GameObject currentBullet = Instantiate(projectile, attackPoint.position, Quaternion.identity);
            currentBullet.transform.forward = direction.normalized;
            currentBullet.GetComponent<Rigidbody>().AddForce(direction.normalized * projectileSpeed, ForceMode.Impulse);

        }
    }

    public void Damage(float damage)
    {
        health -= damage;

        if (health < 0)
        {
            DestroyThisEnemy();
        }
    }

    private void DestroyThisEnemy()
    {
        Destroy(gameObject);
    }
}
