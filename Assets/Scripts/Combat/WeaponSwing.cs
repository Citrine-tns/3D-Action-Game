using UnityEngine;

/// <summary>
/// 武器の振りモーションを腕+手首の2レイヤーで表現する。
///
/// レイヤー1: 右腕 (armR1Pivot=肩, armR2Pivot=肘) — 大きな弧
/// レイヤー2: 武器 (transform) — 手首のスナップ・傾き
///
/// Idle 時の構えは WeaponData.basePose から読む。
/// </summary>
public class WeaponSwing : MonoBehaviour
{
    private ComboRunner comboRunner;
    private Transform armR1Pivot;
    private Transform armR2Pivot;
    private Quaternion baseRotation;
    private ComboRunner.State lastState;

    private WeaponBasePose BasePose =>
        (comboRunner.weapon != null) ? comboRunner.weapon.basePose : default;

    private SwingPose armFrom;
    private SwingPose armMid;
    private SwingPose armTo;
    private float swingDuration;
    private float swingTimer;

    private struct SwingPose
    {
        public float pitch;
        public float yaw;
    }

    public void Initialize(ComboRunner runner, Transform shoulder, Transform elbow)
    {
        comboRunner = runner;
        armR1Pivot = shoulder;
        armR2Pivot = elbow;
        baseRotation = transform.localRotation;

        ApplyIdlePose();
    }

    private void Update()
    {
        if (comboRunner == null || armR1Pivot == null) return;

        var state = comboRunner.CurrentState;

        if (state == ComboRunner.State.Active && lastState != ComboRunner.State.Active)
        {
            StartSwing();
        }

        lastState = state;

        switch (state)
        {
            case ComboRunner.State.Active:
                UpdateSwing();
                break;

            case ComboRunner.State.Recovery:
                // 肩: 終端位置で保持
                armR1Pivot.localRotation =
                    Quaternion.AngleAxis(armTo.yaw, Vector3.up) *
                    Quaternion.AngleAxis(armTo.pitch, Vector3.right);
                // 肘: 伸ばしたまま保持
                if (armR2Pivot != null)
                    armR2Pivot.localRotation = Quaternion.Euler(-20f, 0f, 0f);
                break;

            default:
                // Idle: 腕は PlayerWalkAnimation に任せる
                // 武器だけ基本姿勢へ戻す
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    baseRotation * Quaternion.Euler(BasePose.wrist),
                    10f * Time.deltaTime);
                break;
        }
    }

    /// <summary>
    /// 基本姿勢を即座に適用する（装備時）。
    /// </summary>
    private void ApplyIdlePose()
    {
        armR1Pivot.localRotation = Quaternion.Euler(BasePose.armR1);

        if (armR2Pivot != null)
            armR2Pivot.localRotation = Quaternion.Euler(BasePose.armR2);

        transform.localRotation = baseRotation * Quaternion.Euler(BasePose.wrist);
    }

    private void StartSwing()
    {
        var node = comboRunner.CurrentNode;
        if (node == null) return;

        float speed = 1f;
        if (comboRunner.weapon != null && comboRunner.weapon.attackSpeed > 0f)
            speed = comboRunner.weapon.attackSpeed;

        swingDuration = node.motionDuration / speed;
        swingTimer = 0f;

        armFrom = PositionToArmPose(node.startPosition);
        armTo = PositionToArmPose(node.endPosition);

        armMid = new SwingPose
        {
            pitch = -90f,
            yaw = (armFrom.yaw + armTo.yaw) * 0.5f
        };

        // 肩を始動位置へ
        armR1Pivot.localRotation =
            Quaternion.AngleAxis(armFrom.yaw, Vector3.up) *
            Quaternion.AngleAxis(armFrom.pitch, Vector3.right);

        // 肘: 振りかぶり時は曲げる
        if (armR2Pivot != null)
            armR2Pivot.localRotation = Quaternion.Euler(-40f, 0f, 0f);

        // 手首
        transform.localRotation = baseRotation * Quaternion.Euler(BasePose.wrist);
    }

    private void UpdateSwing()
    {
        swingTimer += Time.deltaTime;
        float t = Mathf.Clamp01(swingTimer / swingDuration);

        float eased = 1f - (1f - t) * (1f - t);

        // --- 肩: Bezier 曲線 ---
        float oneMinusT = 1f - eased;
        float currentPitch = oneMinusT * oneMinusT * armFrom.pitch
                           + 2f * oneMinusT * eased * armMid.pitch
                           + eased * eased * armTo.pitch;
        float currentYaw = oneMinusT * oneMinusT * armFrom.yaw
                         + 2f * oneMinusT * eased * armMid.yaw
                         + eased * eased * armTo.yaw;

        armR1Pivot.localRotation =
            Quaternion.AngleAxis(currentYaw, Vector3.up) *
            Quaternion.AngleAxis(currentPitch, Vector3.right);

        // --- 肘: 振りかぶり(曲げ) → 振り終わり(伸ばし) ---
        if (armR2Pivot != null)
        {
            float elbowAngle = Mathf.Lerp(-40f, -20f, eased);
            armR2Pivot.localRotation = Quaternion.Euler(elbowAngle, 0f, 0f);
        }

        // --- 手首: スナップ ---
        float wristSnap = eased * 80f;
        transform.localRotation = baseRotation * Quaternion.Euler(
            BasePose.wrist.x + wristSnap,
            BasePose.wrist.y,
            BasePose.wrist.z);
    }

    /// <summary>
    /// NodePosition → 肩の構え角度
    /// </summary>
    private static SwingPose PositionToArmPose(NodePosition pos)
    {
        return pos switch
        {
            NodePosition.Upper => new SwingPose { pitch = -160f, yaw = 0f },
            NodePosition.Lower => new SwingPose { pitch = -20f,  yaw = 0f },
            NodePosition.Left  => new SwingPose { pitch = -90f,  yaw = -70f },
            NodePosition.Right => new SwingPose { pitch = -90f,  yaw = 70f },
            NodePosition.Front => new SwingPose { pitch = -90f,  yaw = 0f },
            _ => new SwingPose()
        };
    }
}
