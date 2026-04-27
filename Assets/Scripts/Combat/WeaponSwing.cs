using UnityEngine;

/// <summary>
/// 武器の振りモーションをコードで生成する。
/// 全てのスイングは前方を経由する Bezier 曲線で補間。
/// </summary>
public class WeaponSwing : MonoBehaviour
{
    [Header("待機")]
    public Vector3 idleRotation = new(20f, -15f, 0f);

    private ComboRunner comboRunner;
    private ComboRunner.State lastState;

    private SwingPose poseFrom;
    private SwingPose poseMid;
    private SwingPose poseTo;
    private float swingDuration;
    private float swingTimer;

    private struct SwingPose
    {
        public float x; // ピッチ: 0=上, 90=前, 180=下
        public float y; // ヨー:   0=中央, 90=左, -90=右
    }

    public void Initialize(ComboRunner runner)
    {
        comboRunner = runner;
        transform.localRotation = Quaternion.Euler(idleRotation);
    }

    private void Update()
    {
        if (comboRunner == null) return;

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
                transform.localRotation =
                    Quaternion.AngleAxis(poseTo.y, Vector3.up) *
                    Quaternion.AngleAxis(poseTo.x, Vector3.right);
                break;

            default:
                // AngleAxis で待機角度を構築（他の状態と方式を統一）
                Quaternion idleTarget =
                    Quaternion.AngleAxis(idleRotation.y, Vector3.up) *
                    Quaternion.AngleAxis(idleRotation.x, Vector3.right);
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    idleTarget,
                    10f * Time.deltaTime);
                break;
        }
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

        poseFrom = PositionToPose(node.startPosition);
        poseTo = PositionToPose(node.endPosition);

        // 経由点: 必ず前方(x=90)を通る。y は始動と終端の中間。
        poseMid = new SwingPose
        {
            x = 90f,
            y = (poseFrom.y + poseTo.y) * 0.5f
        };

        transform.localRotation =
            Quaternion.AngleAxis(poseFrom.y, Vector3.up) *
            Quaternion.AngleAxis(poseFrom.x, Vector3.right);
    }

    private void UpdateSwing()
    {
        swingTimer += Time.deltaTime;
        float t = Mathf.Clamp01(swingTimer / swingDuration);

        // イーズアウト
        float eased = 1f - (1f - t) * (1f - t);

        // 2次 Bezier 曲線で補間
        // 始動 → 経由点 → 終端 を滑らかなカーブで結ぶ
        float oneMinusT = 1f - eased;
        float currentX = oneMinusT * oneMinusT * poseFrom.x
                       + 2f * oneMinusT * eased * poseMid.x
                       + eased * eased * poseTo.x;
        float currentY = oneMinusT * oneMinusT * poseFrom.y
                       + 2f * oneMinusT * eased * poseMid.y
                       + eased * eased * poseTo.y;

        // Euler ではなく回転を明示的に合成（ジンバルロック回避）
        // Y回転（左右）→ X回転（前後）の順で適用
        transform.localRotation =
            Quaternion.AngleAxis(currentY, Vector3.up) *
            Quaternion.AngleAxis(currentX, Vector3.right);
    }

    /// <summary>
    /// NodePosition → 構え角度
    ///
    /// 横から見た図（X = ピッチ、正が前方）:
    ///       x=0   上
    ///        ↑
    ///        |
    ///  Pivot ----→ 前方  x=90
    ///        |
    ///        ↓
    ///       x=180  下
    ///
    /// 上から見た図（Y = ヨー、正が右）:
    ///            前方
    ///             ↑
    ///  y=-90 左 ← Pivot → 右 y=90
    /// </summary>
    private static SwingPose PositionToPose(NodePosition pos)
    {
        return pos switch
        {
            NodePosition.Upper => new SwingPose { x = 0f,   y = 0f },
            NodePosition.Lower => new SwingPose { x = 180f, y = 0f },
            NodePosition.Left  => new SwingPose { x = 90f,  y = -90f },
            NodePosition.Right => new SwingPose { x = 90f,  y = 90f },
            NodePosition.Front => new SwingPose { x = 90f,  y = 0f },
            _ => new SwingPose()
        };
    }
}
