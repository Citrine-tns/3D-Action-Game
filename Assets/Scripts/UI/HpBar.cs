using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HPバー表示。CanvasのSliderと連動する。
///
/// 【Unityでのセットアップ】
///
/// ■ Canvas作成
/// 1. Hierarchy で右クリック → UI → Canvas
///    - Canvas の Render Mode は "Screen Space - Overlay" のまま
///    - EventSystem も自動で作られる
///
/// ■ HPバー作成
/// 2. Canvas を右クリック → UI → Slider
///    - 名前を "PlayerHpBar" に変更
///    - RectTransform の Anchor を左上に設定
///      （Anchor Presets から左上を選択。Shiftを押しながら選ぶとPivotも移動）
///    - Pos X: 20, Pos Y: -20, Width: 300, Height: 30
///
/// 3. Slider の子オブジェクト "Handle Slide Area" を削除（つまみは不要）
///
/// 4. Slider コンポーネントの設定:
///    - Interactable: OFF（クリックで動かないように）
///    - Min Value: 0
///    - Max Value: 1
///    - Value: 1
///
/// 5. "Background" の Image の Color を暗い色（ダメージ分の背景）に設定
/// 6. "Fill Area/Fill" の Image の Color を緑や赤に設定（HP部分）
///
/// ■ テキスト追加（任意）
/// 7. Slider を右クリック → UI → Text - TextMeshPro
///    - 名前を "HpText" に変更
///    - RectTransform を Slider に合わせて stretch
///    - Alignment: Center / Middle
///    - Font Size: 16
///
/// ■ スクリプト接続
/// 8. Canvas（またはSlider）にこのスクリプトをアタッチ
/// 9. slider と hpText を Inspector で紐づけ
/// 10. target に HP を監視したい対象（プレイヤー等）の Health をセット
/// </summary>
public class HpBar : MonoBehaviour
{
    [Header("UI References")]
    public Slider slider;
    public TextMeshProUGUI hpText;

    [Header("監視対象")]
    public Health target;

    private void OnEnable()
    {
        if (target != null)
        {
            target.OnHpChanged += UpdateDisplay;
            UpdateDisplay(target.CurrentHp, target.maxHp);
        }
    }

    private void OnDisable()
    {
        if (target != null)
        {
            target.OnHpChanged -= UpdateDisplay;
        }
    }

    private void UpdateDisplay(int current, int max)
    {
        if (max <= 0) return;

        slider.value = (float)current / max;

        if (hpText != null)
        {
            hpText.text = $"{current} / {max}";
        }
    }
}
