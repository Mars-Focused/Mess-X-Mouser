using Assets.Interfaces;

namespace Assets.Supers
{
    abstract class Enemy : IDamagable
    {
        float health;
        float damage;
        float speed;
        public Enemy()
        {
            //code to init a base enemy
            this.health = 100;
            this.damage = 10;
            this.speed = 10;
        }
        public Enemy(float health, float damage, float speed)
        {
            //code to init an enemy with custom values
            this.health = health;
            this.damage = damage;
            this.speed = speed;
        }

    }
}
