using UnityEngine;

/// <summary>
/// マイクラ式歩行アニメーション。
/// 移動中は手足を Sin 波で前後に振る。停止時は Lerp で初期姿勢に戻る。
/// 体全体は平行移動のみ。動くのは手足だけ。
///
/// 【セットアップ】
/// 1. Player にこのスクリプトをアタッチ
/// 2. 各 Pivot を Inspector で設定
/// 3. motor を設定（移動判定に使う）
/// </summary>
public class PlayerWalkAnimation : MonoBehaviour
{
    [Header("References")]
    public Transform armLPivot;
    public Transform armRPivot;
    public Transform legLPivot;
    public Transform legRPivot;
    public PlayerMotor motor;

    [Header("Swing")]
    public float swingAngle = 30f;
    public float swingSpeed = 10f;
    public float returnSpeed = 10f;

    private float phase;
    private float armLAngle;
    private float armRAngle;
    private float legLAngle;
    private float legRAngle;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 水平速度で移動中かどうかを判定
        Vector3 vel = controller.velocity;
        vel.y = 0f;
        bool isMoving = vel.sqrMagnitude > 0.01f;

        float armLTarget, armRTarget, legLTarget, legRTarget;

        if (isMoving)
        {
            phase += dt * swingSpeed;

            float angle = Mathf.Sin(phase) * swingAngle;

            armRTarget =  angle;
            armLTarget = -angle;
            legRTarget = -angle;
            legLTarget =  angle;
        }
        else
        {
            armLTarget = 0f;
            armRTarget = 0f;
            legLTarget = 0f;
            legRTarget = 0f;
        }

        float lerp = returnSpeed * dt;
        armLAngle = Mathf.Lerp(armLAngle, armLTarget, lerp);
        armRAngle = Mathf.Lerp(armRAngle, armRTarget, lerp);
        legLAngle = Mathf.Lerp(legLAngle, legLTarget, lerp);
        legRAngle = Mathf.Lerp(legRAngle, legRTarget, lerp);

        armLPivot.localRotation = Quaternion.Euler(armLAngle, 0f, 0f);
        armRPivot.localRotation = Quaternion.Euler(armRAngle, 0f, 0f);
        legLPivot.localRotation = Quaternion.Euler(legLAngle, 0f, 0f);
        legRPivot.localRotation = Quaternion.Euler(legRAngle, 0f, 0f);
    }
}
