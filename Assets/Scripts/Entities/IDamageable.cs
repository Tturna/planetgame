using UnityEngine;

namespace Entities
{
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 damageSourcePosition);
        void Death(bool despawn = false);
        void Knockback(Vector3 damageSourcePosition, float amount);
    }
}