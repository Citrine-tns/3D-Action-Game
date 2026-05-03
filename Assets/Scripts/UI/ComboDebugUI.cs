using UnityEngine;
using TMPro;

/// <summary>
/// コンボの状態をデバッグ表示する。開発中の確認用。
///
/// 【セットアップ】
/// 1. Canvas を右クリック → UI → Text - TextMeshPro
///    - 名前: "ComboDebugText"
///    - Anchor: 右上
///    - Alignment: Right / Top
/// 2. このスクリプトをアタッチし、comboRunner と text を紐づけ
/// </summary>
public class ComboDebugUI : MonoBehaviour
{
    public ComboRunner comboRunner;
    public TextMeshProUGUI text;

    private void Update()
    {
        if (comboRunner == null || text == null) return;

        var node = comboRunner.CurrentNode;
        int index = comboRunner.CurrentNodeIndex;
        int total = comboRunner.currentPreset != null
            ? comboRunner.currentPreset.slots.Count
            : 0;

        string pos = node != null
            ? $"{node.startPosition} > {node.endPosition}"
            : "-";

        text.text = $"State: {comboRunner.CurrentState}\n"
                  + $"Node: {pos}\n"
                  + $"Combo: {index + 1} / {total}";
    }
}
