using System;
using UnityEngine;

/// <summary>
/// HP管理。プレイヤーにも敵にもアタッチする。
/// </summary>
public class Health : MonoBehaviour
{
    [Header("HP")]
    public int maxHp = 100;

    public int CurrentHp { get; private set; }
    public bool IsDead => CurrentHp <= 0;

    /// <summary> ダメージを受けた時に発火。UIの更新等に使う。 </summary>
    public event Action<int, int> OnHpChanged; // (currentHp, maxHp)
    public event Action OnDied;

    private void Awake()
    {
        CurrentHp = maxHp;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        OnHpChanged?.Invoke(CurrentHp, maxHp);

        if (IsDead)
        {
            OnDied?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);
        OnHpChanged?.Invoke(CurrentHp, maxHp);
    }
}
