using UnityEngine;

/// <summary>
/// 武器の見た目と Hitbox をまとめて管理する。
/// プレイヤーの手元（WeaponPivot）にアタッチする。
///
/// 【セットアップ】
/// 1. プレイヤーの子に空オブジェクト "WeaponPivot" を作る
///    - 位置：キャラの右手あたり（例: X:0.5, Y:1.0, Z:0.5）
/// 2. WeaponPivot にこのスクリプトをアタッチ
/// 3. PlayerCombatController の hitbox は空のまま（このスクリプトが自動設定する）
/// </summary>
public class WeaponHolder : MonoBehaviour
{
    [Header("References")]
    public PlayerCombatController combatController;
    public ComboRunner comboRunner;

    private GameObject currentWeaponObject;
    private Hitbox currentHitbox;

    private void Start()
    {
        if (combatController == null) return;

        if (combatController.weapon != null)
        {
            Equip(combatController.weapon);
        }
    }

    public void Equip(WeaponData weapon)
    {
        // 既存の武器を破棄
        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
        }

        // 武器ルートオブジェクト
        currentWeaponObject = new GameObject(weapon.weaponName);
        currentWeaponObject.transform.SetParent(transform, false);

        // 仮モデル（Cylinder: 回転しても見た目の長さが変わらない）
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        model.name = "Model";
        model.transform.SetParent(currentWeaponObject.transform, false);
        model.transform.localPosition = weapon.modelOffset;
        model.transform.localScale = weapon.modelScale;

        // 見た目用 Collider は不要（Hitbox 側で判定する）
        Destroy(model.GetComponent<Collider>());

        // 色を設定
        var renderer = model.GetComponent<Renderer>();
        renderer.material.color = weapon.modelColor;

        // スイングモーション
        var swing = currentWeaponObject.AddComponent<WeaponSwing>();
        swing.Initialize(comboRunner);

        // Hitbox（当たり判定）— 武器オブジェクトの子なので一緒に振られる
        GameObject hitboxObj = new GameObject("Hitbox");
        hitboxObj.transform.SetParent(currentWeaponObject.transform, false);
        hitboxObj.layer = gameObject.layer;

        var boxCollider = hitboxObj.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        var rb = hitboxObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        currentHitbox = hitboxObj.AddComponent<Hitbox>();

        // PlayerCombatController に Hitbox を登録
        combatController.hitbox = currentHitbox;
        currentHitbox.owner = combatController;

        // 最初は無効
        hitboxObj.SetActive(false);
    }
}
