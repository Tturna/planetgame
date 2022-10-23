using UnityEngine;

namespace Entities
{
    public interface IDamageable
    {
        void TakeDamage(float amount);
        void Death();
        void Knockback(Vector3 damageSourcePosition, float amount);
    }
}