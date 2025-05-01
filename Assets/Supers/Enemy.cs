using Assets.Interfaces;
using UnityEngine;
;

namespace Assets.Supers
{
    abstract class Enemy : IDamageable
    {
        float health;
        float maxHealth;
        float damage;
        float speed;
        bool alive;
        public Enemy()
        {
            //code to init a base enemy
            this.health = 100;
            this.maxHealth = 100;
            this.damage = 10;
            this.speed = 10;
            this.alive = true;
        }
        public Enemy(float health, float damage, float speed)
        {
            //code to init an enemy with custom values
            this.health = health;
            this.maxHealth = maxHealth;
            this.damage = damage;
            this.speed = speed;
            this.alive = alive;
        }

        public void Damage(float ammount)
        {
            health -= ammount;
            health = Mathf.Clamp(health, 0, maxHealth);
            if (health == 0)
            {
                alive = false;
            }
        }

    }
}
