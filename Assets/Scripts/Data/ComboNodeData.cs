using UnityEngine;

/// <summary>
/// コンボノード1つ分のデータ。
/// Projectウィンドウで右クリック → Create → Combo Node で作成。
/// </summary>
[CreateAssetMenu(fileName = "NewComboNode", menuName = "Combo Node")]
public class ComboNodeData : ScriptableObject
{
    [Header("基本情報")]
    public string nodeName;
    public WeaponType weaponType;

    [Header("位置制約")]
    public NodePosition startPosition;
    public NodePosition endPosition;

    [Header("性能")]
    public float damageMultiplier = 1f;
    public float motionDuration = 0.5f;
    public float recoveryDuration = 0.3f;
    public float inputWindowDuration = 0.4f;

    [Header("解放")]
    public int requiredLevel = 1;

    [Header("アニメーション")]
    public AnimationClip animationClip;
}
