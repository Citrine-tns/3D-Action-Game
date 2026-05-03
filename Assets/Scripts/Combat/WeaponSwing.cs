using UnityEngine;

/// <summary>
/// 武器の振りモーションを制御する。
///
/// ComboNodeData に animationClip があればそれを再生、
/// なければコード駆動（Bezier 曲線）でフォールバック。
///
/// Idle → 構え遷移（RotateTowards）→ スイング → Recovery → Idle
/// 遷移中は swingTimer を進めず、遷移完了後にスイング開始。
/// </summary>
public class WeaponSwing : MonoBehaviour
{
    private ComboRunner comboRunner;
    private Transform armR1Pivot;
    private Transform armR2Pivot;
    private Quaternion baseRotation;
    private Quaternion weaponPivotBaseRotation;
    private GameObject playerRoot;
    private ComboRunner.State lastState;

    private WeaponBasePose BasePose =>
        (comboRunner.weapon != null) ? comboRunner.weapon.basePose : default;

    // コード駆動用
    private SwingPose armFrom;
    private SwingPose armMid;
    private SwingPose armTo;
    private float swingDuration;
    private float swingTimer;
    private AnimationClip currentClip;

    // 構えへの遷移用
    private bool _inTransition;
    private bool inTransition
    {
        get => _inTransition;
        set
        {
            _inTransition = value;
            if (comboRunner != null)
                comboRunner.IsInTransition = value;
        }
    }
    public float transitionSpeed = 180f; // 度/秒

    // クリップモード遷移: 全 Transform を保存して crossfade
    private Transform[] allTransforms;
    private Quaternion[] preSwingRotations;
    private Quaternion[] clipStartRotations;
    private float transitionBlend;
    private float transitionMaxAngle;

    // コード駆動遷移: 3つの Pivot のみ
    private Quaternion transitionArmCurrent;
    private Quaternion transitionElbowCurrent;
    private Quaternion transitionWristCurrent;
    private Quaternion transitionArmTarget;
    private Quaternion transitionElbowTarget;
    private Quaternion transitionWristTarget;

    private struct SwingPose
    {
        public float pitch;
        public float yaw;
    }

    public void Initialize(ComboRunner runner, Transform shoulder, Transform elbow, GameObject player)
    {
        comboRunner = runner;
        armR1Pivot = shoulder;
        armR2Pivot = elbow;
        playerRoot = player;
        baseRotation = transform.localRotation;
        weaponPivotBaseRotation = transform.parent.localRotation;

        // Animator は編集用に残すが、実行時は無効化（SampleAnimation は Animator 不要）
        var animator = playerRoot.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;

        // 全 Transform のキャッシュ（クリップ crossfade 用）
        allTransforms = playerRoot.GetComponentsInChildren<Transform>();
        preSwingRotations = new Quaternion[allTransforms.Length];
        clipStartRotations = new Quaternion[allTransforms.Length];

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
                if (currentClip != null)
                {
                    currentClip.SampleAnimation(playerRoot, currentClip.length);
                }
                else
                {
                    armR1Pivot.localRotation =
                        Quaternion.AngleAxis(armTo.yaw, Vector3.up) *
                        Quaternion.AngleAxis(armTo.pitch, Vector3.right);
                    if (armR2Pivot != null)
                        armR2Pivot.localRotation = Quaternion.Euler(-20f, 0f, 0f);
                }
                break;

            default:
                // Idle
                currentClip = null;
                inTransition = false;
                float idleLerp = 10f * Time.deltaTime;
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    baseRotation * Quaternion.Euler(BasePose.wrist),
                    idleLerp);
                transform.parent.localRotation = Quaternion.Slerp(
                    transform.parent.localRotation,
                    weaponPivotBaseRotation,
                    idleLerp);
                break;
        }
    }

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
        currentClip = node.animationClip;

        if (currentClip != null)
        {
            StartSwingClip();
        }
        else
        {
            StartSwingCodeDriven();
        }
    }

    /// <summary>
    /// クリップモード: 全 Transform の現在値と目標値を保存。
    /// SampleAnimation で目標値を取得後、現在値に戻す。
    /// </summary>
    private void StartSwingClip()
    {
        // 全 Transform の現在値を保存
        for (int i = 0; i < allTransforms.Length; i++)
            preSwingRotations[i] = allTransforms[i].localRotation;

        // クリップの0フレーム目をサンプリングして目標値を取得
        currentClip.SampleAnimation(playerRoot, 0f);
        transitionMaxAngle = 0f;
        for (int i = 0; i < allTransforms.Length; i++)
        {
            clipStartRotations[i] = allTransforms[i].localRotation;
            float angle = Quaternion.Angle(preSwingRotations[i], clipStartRotations[i]);
            if (angle > transitionMaxAngle)
                transitionMaxAngle = angle;
        }

        // 全 Transform を元に戻す
        for (int i = 0; i < allTransforms.Length; i++)
            allTransforms[i].localRotation = preSwingRotations[i];

        transitionBlend = 0f;
        inTransition = transitionMaxAngle > 1f;

        // ComboRunner のタイマーに遷移時間を加算
        if (inTransition && transitionSpeed > 0f)
            comboRunner.ExtendActiveTimer(transitionMaxAngle / transitionSpeed);
    }

    /// <summary>
    /// コード駆動モード: 腕・肘・手首の現在値と目標値を保存。
    /// </summary>
    private void StartSwingCodeDriven()
    {
        armFrom = PositionToArmPose(comboRunner.CurrentNode.startPosition);
        armTo = PositionToArmPose(comboRunner.CurrentNode.endPosition);

        armMid = new SwingPose
        {
            pitch = -90f,
            yaw = (armFrom.yaw + armTo.yaw) * 0.5f
        };

        transitionArmCurrent = armR1Pivot.localRotation;
        transitionElbowCurrent = (armR2Pivot != null) ? armR2Pivot.localRotation : Quaternion.identity;
        transitionWristCurrent = transform.localRotation;

        transitionArmTarget =
            Quaternion.AngleAxis(armFrom.yaw, Vector3.up) *
            Quaternion.AngleAxis(armFrom.pitch, Vector3.right);
        transitionElbowTarget = Quaternion.Euler(-40f, 0f, 0f);
        transitionWristTarget = baseRotation * Quaternion.Euler(BasePose.wrist);

        float maxAngle = Quaternion.Angle(transitionArmCurrent, transitionArmTarget);
        inTransition = maxAngle > 1f;

        if (inTransition && transitionSpeed > 0f)
            comboRunner.ExtendActiveTimer(maxAngle / transitionSpeed);
    }

    private void UpdateSwing()
    {
        if (inTransition)
        {
            if (currentClip != null)
                UpdateTransitionClip();
            else
                UpdateTransitionCodeDriven();
            return;
        }

        // --- 遷移完了: スイング本体 ---
        swingTimer += Time.deltaTime;

        if (currentClip != null)
        {
            float t = Mathf.Clamp01(swingTimer / swingDuration);
            currentClip.SampleAnimation(playerRoot, t * currentClip.length);
        }
        else
        {
            UpdateSwingCodeDriven();
        }
    }

    /// <summary>
    /// クリップ遷移: 全 Transform を preSwing → clipStart へ crossfade。
    /// </summary>
    private void UpdateTransitionClip()
    {
        float blendStep = (transitionMaxAngle > 0.01f)
            ? (transitionSpeed / transitionMaxAngle) * Time.deltaTime
            : 1f;
        transitionBlend = Mathf.Clamp01(transitionBlend + blendStep);

        for (int i = 0; i < allTransforms.Length; i++)
        {
            allTransforms[i].localRotation = Quaternion.Slerp(
                preSwingRotations[i],
                clipStartRotations[i],
                transitionBlend);
        }

        if (transitionBlend >= 1f)
            inTransition = false;
    }

    /// <summary>
    /// コード駆動遷移: 腕・肘・手首を RotateTowards。
    /// </summary>
    private void UpdateTransitionCodeDriven()
    {
        float maxDeg = transitionSpeed * Time.deltaTime;

        transitionArmCurrent = Quaternion.RotateTowards(transitionArmCurrent, transitionArmTarget, maxDeg);
        transitionElbowCurrent = Quaternion.RotateTowards(transitionElbowCurrent, transitionElbowTarget, maxDeg);
        transitionWristCurrent = Quaternion.RotateTowards(transitionWristCurrent, transitionWristTarget, maxDeg);

        armR1Pivot.localRotation = transitionArmCurrent;
        if (armR2Pivot != null)
            armR2Pivot.localRotation = transitionElbowCurrent;
        transform.localRotation = transitionWristCurrent;

        if (Quaternion.Angle(transitionArmCurrent, transitionArmTarget) < 1f)
            inTransition = false;
    }

    /// <summary>
    /// コード駆動スイング本体。
    /// </summary>
    private void UpdateSwingCodeDriven()
    {
        float t = Mathf.Clamp01(swingTimer / swingDuration);
        float eased = 1f - (1f - t) * (1f - t);

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

        if (armR2Pivot != null)
        {
            float elbowAngle = Mathf.Lerp(-40f, -20f, eased);
            armR2Pivot.localRotation = Quaternion.Euler(elbowAngle, 0f, 0f);
        }

        float wristSnap = eased * 80f;
        transform.localRotation = baseRotation * Quaternion.Euler(
            BasePose.wrist.x + wristSnap,
            BasePose.wrist.y,
            BasePose.wrist.z);
    }

    private static SwingPose PositionToArmPose(NodePosition pos)
    {
        return pos switch
        {
            NodePosition.Upper      => new SwingPose { pitch = -160f, yaw = 0f },
            NodePosition.UpperRight => new SwingPose { pitch = -140f, yaw = 50f },
            NodePosition.Right      => new SwingPose { pitch = -90f,  yaw = 70f },
            NodePosition.LowerRight => new SwingPose { pitch = -40f,  yaw = 50f },
            NodePosition.Lower      => new SwingPose { pitch = -20f,  yaw = 0f },
            NodePosition.LowerLeft  => new SwingPose { pitch = -40f,  yaw = -50f },
            NodePosition.Left       => new SwingPose { pitch = -90f,  yaw = -70f },
            NodePosition.UpperLeft  => new SwingPose { pitch = -140f, yaw = -50f },
            NodePosition.Front      => new SwingPose { pitch = -90f,  yaw = 0f },
            NodePosition.Back       => new SwingPose { pitch = -30f,  yaw = 0f },
            _ => new SwingPose()
        };
    }
}
