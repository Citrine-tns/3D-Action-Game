using UnityEngine;

/// <summary>
/// プレイヤーの戦闘を統括する。
/// 入力 → ComboRunner → Hitbox → Hurtbox → DamageCalculator を繋ぐ役割。
///
/// 【Unityでのセットアップ】
/// 1. プレイヤーオブジェクトにこのスクリプトをアタッチ
/// 2. Inspectorで input, comboRunner, hitbox, weapon を設定
/// 3. hitbox は武器オブジェクトに付いた Hitbox コンポーネント
/// </summary>
public class PlayerCombatController : MonoBehaviour
{
    [Header("References")]
    public PlayerInputReader input;
    public ComboRunner comboRunner;
    public Hitbox hitbox;

    [Header("装備中の武器")]
    public WeaponData weapon;

    private void Start()
    {
        // Hitbox は最初は無効
        hitbox.gameObject.SetActive(false);
        hitbox.owner = this;
    }

    private void Update()
    {
        if (input.AttackPressedThisFrame)
        {
            comboRunner.OnAttackInput();
        }

        // ComboRunner の状態に応じて Hitbox を切り替え
        bool shouldBeActive = comboRunner.CurrentState == ComboRunner.State.Active;
        if (hitbox.gameObject.activeSelf != shouldBeActive)
        {
            hitbox.gameObject.SetActive(shouldBeActive);
        }
    }

    /// <summary>
    /// Hitbox が敵に当たった時に呼ばれる。
    /// </summary>
    public void OnHitTarget(Hurtbox target)
    {
        if (weapon == null || comboRunner.CurrentNode == null)
            return;

        bool isFinal = comboRunner.IsFinalNode();
        bool isBack = IsBackAttack(target.transform);

        target.ReceiveHit(weapon, comboRunner.CurrentNode, isFinal, isBack);
    }

    private bool IsBackAttack(Transform target)
    {
        Vector3 toAttacker = (transform.position - target.position).normalized;
        float dot = Vector3.Dot(target.forward, toAttacker);
        // dot > 0 = 攻撃者がターゲットの前方にいる（=背面攻撃ではない）
        // dot < 0 = 攻撃者がターゲットの背後にいる（=背面攻撃）
        return dot < -0.3f;
    }
}
