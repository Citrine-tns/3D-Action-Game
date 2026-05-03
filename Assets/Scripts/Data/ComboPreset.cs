using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーがメニューで設定したコンボの並び。
/// 武器ごとに1つ保持する。
/// </summary>
[CreateAssetMenu(fileName = "NewComboPreset", menuName = "Combo Preset")]
public class ComboPreset : ScriptableObject
{
    public WeaponData weapon;
    public List<ComboSlot> slots = new();

    /// <summary>
    /// ノードの並びが接続ルールを満たしているか検証する。
    /// </summary>
    public bool Validate()
    {
        if (weapon == null || slots.Count == 0)
            return false;

        if (slots.Count > weapon.maxComboCount)
            return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].node == null)
                return false;

            if (slots[i].node.weaponType != weapon.weaponType)
                return false;

            // 隣接ノード間の接続チェック
            if (i > 0)
            {
                var prev = slots[i - 1];
                var curr = slots[i];
                NodePosition effectiveEnd = prev.followThrough
                    ? NodePositionHelper.GetOpposite(prev.node.endPosition)
                    : prev.node.endPosition;

                if (!NodePositionHelper.CanConnect(effectiveEnd, curr.node.startPosition))
                    return false;
            }
        }

        return true;
    }
}

/// <summary>
/// コンボプリセット内の1スロット。
/// ノードの参照 + そのノード後にフォロースルーするかの設定。
/// </summary>
[Serializable]
public struct ComboSlot
{
    public ComboNodeData node;

    [Tooltip("ONの場合、このノード終了後に武器を体の後ろに回して対角から次に繋ぐ")]
    public bool followThrough;
}
