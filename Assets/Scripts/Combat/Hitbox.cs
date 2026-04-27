using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃判定。武器オブジェクト（またはその子）にアタッチする。
/// Collider（IsTrigger=true）が必要。
///
/// 【Unityでのセットアップ】
/// 1. 武器オブジェクトに BoxCollider か CapsuleCollider を追加
/// 2. Is Trigger にチェックを入れる
/// 3. このスクリプトをアタッチ
/// 4. 武器オブジェクトと敵に Rigidbody が必要（片方でもOK）
///    → 武器側に Rigidbody を付けて Is Kinematic にチェック
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    public PlayerCombatController owner;

    // 1回の攻撃で同じ敵に複数回当たるのを防ぐ
    private readonly HashSet<Hurtbox> hitThisSwing = new();

    private BoxCollider boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        hitThisSwing.Clear();
        ApplyReach();
    }

    /// <summary>
    /// 武器の reach に応じて Collider のサイズと位置を調整する。
    /// 武器モデルはローカル Y+ 方向に伸びるので、Y 方向にサイズを設定。
    /// 武器が回転すると Hitbox も一緒に回転するため、
    /// 振りの軌道上で敵と接触判定が行われる。
    /// </summary>
    private void ApplyReach()
    {
        if (boxCollider == null || owner == null || owner.weapon == null) return;

        float reach = owner.weapon.reach;

        boxCollider.size = new Vector3(0.5f, reach, 0.5f);
        boxCollider.center = new Vector3(0f, reach * 0.5f, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null) return;

        var hurtbox = other.GetComponent<Hurtbox>();
        if (hurtbox == null) return;

        // 自分自身には当たらない
        if (hurtbox.transform.root == owner.transform.root) return;

        // 同じスイングで既にヒット済み
        if (!hitThisSwing.Add(hurtbox)) return;

        owner.OnHitTarget(hurtbox);
    }
}
