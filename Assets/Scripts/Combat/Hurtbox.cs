using UnityEngine;

/// <summary>
/// ダメージを受ける側。敵やオブジェクトにアタッチする。
/// Health と AttributeResistance を持つ。
/// </summary>
[RequireComponent(typeof(Health))]
public class Hurtbox : MonoBehaviour
{
    public AttributeResistance resistance = AttributeResistance.Default;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    public void ReceiveHit(WeaponData weapon, ComboNodeData node, bool isFinalNode, bool isBackAttack)
    {
        int damage = DamageCalculator.Calculate(
            weapon, node, resistance, isFinalNode, isBackAttack);

        health.TakeDamage(damage);
    }
}
