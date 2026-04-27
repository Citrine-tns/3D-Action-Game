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
    public int CurrentNodeIndex { get; private set; } = -1;
    public ComboNodeData CurrentNode =>
        (currentPreset != null && CurrentNodeIndex >= 0 && CurrentNodeIndex < currentPreset.nodes.Count)
            ? currentPreset.nodes[CurrentNodeIndex]
            : null;

    private float stateTimer;
    private bool attackBuffered;

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
        if (currentPreset == null || currentPreset.nodes.Count == 0)
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
        if (nextIndex >= currentPreset.nodes.Count)
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

        // TODO: アニメーション再生
    }

    private void EnterRecovery()
    {
        bool isFinalNode = (CurrentNodeIndex >= currentPreset.nodes.Count - 1);

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
        // attackSpeed: 1.0 = 標準, 1.5 = 1.5倍速, 0.6 = 遅い
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
            && CurrentNodeIndex >= currentPreset.nodes.Count - 1;
    }
}
