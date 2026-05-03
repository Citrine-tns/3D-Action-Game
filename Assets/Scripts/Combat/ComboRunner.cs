using UnityEngine;

/// <summary>
/// コンボの実行を管理する状態マシン。
/// Idle → Active → Recovery → Active → ... → Idle
/// </summary>
public class ComboRunner : MonoBehaviour
{
    public enum State
    {
        Idle,
        Active,
        Recovery
    }

    [Header("References")]
    public ComboPreset currentPreset;
    public WeaponData weapon;

    public State CurrentState { get; private set; } = State.Idle;

    /// <summary> WeaponSwing が構え遷移中に true にする。攻撃判定の制御に使う。 </summary>
    public bool IsInTransition { get; set; }
    public int CurrentNodeIndex { get; private set; } = -1;
    public ComboNodeData CurrentNode =>
        (currentPreset != null && CurrentNodeIndex >= 0 && CurrentNodeIndex < currentPreset.slots.Count)
            ? currentPreset.slots[CurrentNodeIndex].node
            : null;
    public ComboSlot? CurrentSlot =>
        (currentPreset != null && CurrentNodeIndex >= 0 && CurrentNodeIndex < currentPreset.slots.Count)
            ? currentPreset.slots[CurrentNodeIndex]
            : null;

    private float stateTimer;
    private bool attackBuffered;

    /// <summary>
    /// Active 状態の残り時間を延長する（構え遷移の分）。
    /// </summary>
    public void ExtendActiveTimer(float extraTime)
    {
        if (CurrentState == State.Active)
            stateTimer += extraTime;
    }

    private void Update()
    {
        stateTimer -= Time.deltaTime;

        switch (CurrentState)
        {
            case State.Idle:
                break;

            case State.Active:
                if (stateTimer <= 0f)
                {
                    EnterRecovery();
                }
                break;

            case State.Recovery:
                if (attackBuffered)
                {
                    attackBuffered = false;
                    AdvanceCombo();
                }
                else if (stateTimer <= 0f)
                {
                    ReturnToIdle();
                }
                break;
        }
    }

    /// <summary>
    /// 攻撃ボタンが押された時に呼ぶ。
    /// </summary>
    public void OnAttackInput()
    {
        if (currentPreset == null || currentPreset.slots.Count == 0)
            return;

        switch (CurrentState)
        {
            case State.Idle:
                StartCombo();
                break;

            case State.Active:
                // Active中の入力はバッファに溜める
                attackBuffered = true;
                break;

            case State.Recovery:
                attackBuffered = true;
                break;
        }
    }

    private void StartCombo()
    {
        CurrentNodeIndex = 0;
        EnterActive();
    }

    private void AdvanceCombo()
    {
        int nextIndex = CurrentNodeIndex + 1;

        // 最大コンボ数チェック
        if (nextIndex >= currentPreset.slots.Count)
        {
            ReturnToIdle();
            return;
        }

        CurrentNodeIndex = nextIndex;
        EnterActive();
    }

    private void EnterActive()
    {
        CurrentState = State.Active;
        stateTimer = CurrentNode.motionDuration / GetSpeedMultiplier();
        attackBuffered = false;
    }

    private void EnterRecovery()
    {
        bool isFinalNode = (CurrentNodeIndex >= currentPreset.slots.Count - 1);

        CurrentState = State.Recovery;
        float speed = GetSpeedMultiplier();

        if (isFinalNode)
        {
            // 最終ノード：入力受付なし、硬直のみ
            stateTimer = CurrentNode.recoveryDuration / speed;
        }
        else
        {
            // 途中ノード：入力受付窓あり
            stateTimer = CurrentNode.inputWindowDuration / speed;
        }
    }

    private float GetSpeedMultiplier()
    {
        return (weapon != null && weapon.attackSpeed > 0f) ? weapon.attackSpeed : 1f;
    }

    private void ReturnToIdle()
    {
        CurrentState = State.Idle;
        CurrentNodeIndex = -1;
        attackBuffered = false;
    }

    /// <summary>
    /// 現在のノードが最終ノードかどうか。ダメージ計算に使う。
    /// </summary>
    public bool IsFinalNode()
    {
        return CurrentNodeIndex >= 0
            && CurrentNodeIndex >= currentPreset.slots.Count - 1;
    }
}
