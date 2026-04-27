using UnityEngine;

/// <summary>
/// ダメージ計算。
/// 最終ダメージ = 武器基礎ダメージ × ノード倍率 × 属性相性倍率 (× 最終コンボ補正) (× 背面補正)
/// </summary>
public static class DamageCalculator
{
    public static int Calculate(
        WeaponData weapon,
        ComboNodeData node,
        AttributeResistance resistance,
        bool isFinalNode,
        bool isBackAttack)
    {
        float damage = weapon.baseDamage;

        // ノード倍率
        damage *= node.damageMultiplier;

        // 属性相性
        damage *= resistance.GetMultiplier(weapon.attribute);

        // 最終コンボ補正
        if (isFinalNode)
        {
            damage *= weapon.finalNodeDamageMultiplier;
        }

        // 背面補正（短剣等）
        if (isBackAttack && weapon.backAttackMultiplier > 1f)
        {
            damage *= weapon.backAttackMultiplier;
        }

        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }
}
