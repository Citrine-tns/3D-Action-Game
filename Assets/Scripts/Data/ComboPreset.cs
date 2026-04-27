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
    public List<ComboNodeData> nodes = new();

    /// <summary>
    /// ノードの並びが接続ルールを満たしているか検証する。
    /// </summary>
    public bool Validate()
    {
        if (weapon == null || nodes.Count == 0)
            return false;

        if (nodes.Count > weapon.maxComboCount)
            return false;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null)
                return false;

            if (nodes[i].weaponType != weapon.weaponType)
                return false;

            // 隣接ノード間の接続チェック（終了位置 → 次の開始位置）
            if (i > 0 && nodes[i - 1].endPosition != nodes[i].startPosition)
                return false;
        }

        return true;
    }
}
