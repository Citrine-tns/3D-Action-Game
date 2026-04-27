using UnityEngine;

/// <summary>
/// 武器1種類分のデータ。
/// Projectウィンドウで右クリック → Create → Weapon で作成。
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("基本情報")]
    public string weaponName;
    public WeaponType weaponType;
    public PhysicalAttribute attribute;

    [Header("性能")]
    public float baseDamage = 10f;
    public float reach = 2f;
    public float attackSpeed = 1f;

    [Header("コンボ")]
    public int maxComboCount = 3;
    public float finalNodeDamageMultiplier = 1.5f;

    [Header("固有補正")]
    public float backAttackMultiplier = 1f;
    public bool canStun;

    [Header("仮モデル（見た目）")]
    public Vector3 modelScale = new(0.15f, 1.2f, 0.15f);
    public Vector3 modelOffset = new(0f, 0.6f, 0f);
    public Color modelColor = Color.white;

    [Header("所属ノード一覧（この武器で使える全ノード）")]
    public ComboNodeData[] availableNodes;
}
