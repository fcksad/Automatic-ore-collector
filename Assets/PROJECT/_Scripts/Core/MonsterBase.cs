using UnityEngine;

public class MonsterBase : MonoBehaviour, IDamageable
{
    public void TakeDamage(float damage)
    {
        Debug.LogError("Taked");
    }

}
