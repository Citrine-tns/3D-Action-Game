using UnityEngine;

/// <summary>
/// 歩行アニメーション（10パーツ対応）。
///
/// 武器未装備: 全リム通常振り（基本姿勢 = identity）
/// 武器装備: WeaponData.basePose に従い、基本姿勢ベースで振り幅を制御
///
/// コンボ中（Active/Recovery）は右腕の制御を WeaponSwing に譲る。
///
/// 【セットアップ】
/// Player にこのスクリプトをアタッチし、各 Pivot を Inspector で設定。
/// </summary>
public class PlayerWalkAnimation : MonoBehaviour
{
    [Header("Body")]
    public Transform bodyPivot;

    [Header("Arm Pivots")]
    public Transform armL1Pivot;
    public Transform armL2Pivot;
    public Transform armR1Pivot;
    public Transform armR2Pivot;

    [Header("Leg Pivots")]
    public Transform legL1Pivot;
    public Transform legL2Pivot;
    public Transform legR1Pivot;
    public Transform legR2Pivot;

    [Header("Combat")]
    public ComboRunner comboRunner;

    [Header("Swing")]
    public float swingAngle = 30f;
    public float swingSpeed = 10f;
    public float returnSpeed = 10f;

    [Header("Joint Bend")]
    [Range(0f, 1f)] public float elbowBend = 0.4f;
    [Range(0f, 1f)] public float kneeBend = 0.5f;

    private float phase;
    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float lerp = returnSpeed * dt;

        // 武器の基本姿勢（未装備なら全部ゼロ = identity）
        bool hasWeapon = comboRunner != null && comboRunner.weapon != null;
        WeaponBasePose pose = hasWeapon ? comboRunner.weapon.basePose : default;
        float armLScale = hasWeapon ? pose.armLSwing : 1f;
        float armRScale = hasWeapon ? pose.armRSwing : 1f;
        float legScale  = hasWeapon ? pose.legSwing  : 1f;

        bool isComboActive = comboRunner != null
            && comboRunner.CurrentState != ComboRunner.State.Idle;

        Vector3 vel = controller.velocity;
        vel.y = 0f;
        bool isMoving = vel.sqrMagnitude > 0.01f;

        // --- 体幹: 常に基本姿勢へ ---
        if (bodyPivot != null)
        {
            bodyPivot.localRotation = Quaternion.Slerp(
                bodyPivot.localRotation,
                Quaternion.Euler(pose.body),
                lerp);
        }

        if (isMoving)
        {
            phase += dt * swingSpeed;
            float angle = Mathf.Sin(phase) * swingAngle;

            // --- 脚 ---
            float legAngle = angle * legScale;
            ApplyLimb(legL1Pivot, legL2Pivot,
                pose.legL1, pose.legL2, legAngle, kneeBend, lerp);
            ApplyLimb(legR1Pivot, legR2Pivot,
                pose.legR1, pose.legR2, -legAngle, kneeBend, lerp);

            // --- 左腕 ---
            float armLAngle = -angle * armLScale;
            ApplyLimb(armL1Pivot, armL2Pivot,
                pose.armL1, pose.armL2, armLAngle, elbowBend, lerp);

            // --- 右腕（コンボ中はスキップ）---
            if (!isComboActive)
            {
                float armRAngle = angle * armRScale;
                ApplyLimb(armR1Pivot, armR2Pivot,
                    pose.armR1, pose.armR2, armRAngle, elbowBend, lerp);
            }
        }
        else
        {
            // 停止 → 基本姿勢へ戻す
            ReturnTo(legL1Pivot, pose.legL1, lerp);
            ReturnTo(legL2Pivot, pose.legL2, lerp);
            ReturnTo(legR1Pivot, pose.legR1, lerp);
            ReturnTo(legR2Pivot, pose.legR2, lerp);

            ReturnTo(armL1Pivot, pose.armL1, lerp);
            ReturnTo(armL2Pivot, pose.armL2, lerp);

            if (!isComboActive)
            {
                ReturnTo(armR1Pivot, pose.armR1, lerp);
                ReturnTo(armR2Pivot, pose.armR2, lerp);
            }
        }
    }

    /// <summary>
    /// 上位パーツを基本姿勢+振り角度へ、下位パーツに屈伸を加える。
    /// </summary>
    private void ApplyLimb(Transform upperPivot, Transform lowerPivot,
        Vector3 upperBase, Vector3 lowerBase,
        float swingOffset, float bendRatio, float lerp)
    {
        Quaternion upperTarget = Quaternion.Euler(
            upperBase.x + swingOffset, upperBase.y, upperBase.z);
        upperPivot.localRotation = Quaternion.Slerp(
            upperPivot.localRotation, upperTarget, lerp);

        float bend = Mathf.Max(0f, swingOffset) * bendRatio;
        Quaternion lowerTarget = Quaternion.Euler(
            lowerBase.x + bend, lowerBase.y, lowerBase.z);
        lowerPivot.localRotation = Quaternion.Slerp(
            lowerPivot.localRotation, lowerTarget, lerp);
    }

    private static void ReturnTo(Transform pivot, Vector3 baseAngles, float lerp)
    {
        pivot.localRotation = Quaternion.Slerp(
            pivot.localRotation, Quaternion.Euler(baseAngles), lerp);
    }
}
