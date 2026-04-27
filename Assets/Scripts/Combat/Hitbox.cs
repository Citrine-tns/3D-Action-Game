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

    private void OnEnable()
    {
        hitThisSwing.Clear();
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
