using UnityEngine;

public class MonsterBase : MonoBehaviour, IDamageable
{
    public void ApplyDamage(float damage)
    {
        Debug.LogWarning(damage);
    }

}
