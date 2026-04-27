using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform cameraTransform;
    public PlayerInputReader input;

    [Header("Normal Camera")]
    public float normalDistance = 6f;
    public float strafeResetPitch = 20f;
    public float minPitch = -89f;
    public float maxPitch = 89f;
    public float yawSpeed = 100f;
    public float pitchSpeed = 100f;

    [Header("Reset Assist")]
    public float resetAssistDuration = 0.2f;
    [Range(0f, 1f)] public float resetYawAssistStrength = 0.18f;
    [Range(0f, 1f)] public float resetPitchAssistStrength = 0.18f;
    [Range(0f, 1f)] public float resetDistanceAssistStrength = 0.22f;

    [Header("Collision")]
    public float cameraRadius = 0.3f;
    public LayerMask collisionMask;
    public float collisionOffset = 0.15f;

    [Header("LockOn Camera")]
    public float lockPitchMin = 0f;
    public float lockPitchMax = 89f;
    public float lockDistanceMultiplier = 1.5f;
    public float lockDistanceOffset = 3f;
    public float distanceSmooth = 10f;

    [Range(0f, 1f)] public float pivotSymmetryFactor = 0.35f;
    [Range(0f, 1f)] public float maxPivotOffsetRatio = 0.4f;

    private float yaw;
    private float pitch;
    private float currentDistance;

    private bool resetAssistActive;
    private float resetAssistTimer;
    private float assistTargetYaw;
    private float assistTargetPitch;
    private float assistTargetDistance;

    private Transform lockTarget;
    private bool inLockOn;
    private Camera cam;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    private void Start()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);
        currentDistance = normalDistance;
        cam = cameraTransform.GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (inLockOn && lockTarget != null)
        {
            UpdateLockOnCamera();
        }
        else
        {
            UpdateNormalCamera();
        }
    }

    public void ResetToStrafeView()
    {
        inLockOn = false;
        lockTarget = null;

        BeginResetAssist(
            player.eulerAngles.y,
            strafeResetPitch,
            normalDistance
        );
    }

    public void EnterLockOn(Transform target)
    {
        lockTarget = target;
        inLockOn = target != null;
        resetAssistActive = false;
    }

    public void UpdateLockOnTarget(Transform target)
    {
        lockTarget = target;
    }

    public void ExitLockOnToNormal()
    {
        inLockOn = false;
        lockTarget = null;
    }

    public void ExitLockOnKeepCurrentView()
    {
        inLockOn = false;
        lockTarget = null;

        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);
    }

    private void UpdateNormalCamera()
    {
        transform.position = player.position;

        yaw -= input.Look.x * yawSpeed * Time.deltaTime;
        pitch += input.Look.y * pitchSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (resetAssistActive)
        {
            resetAssistTimer += Time.deltaTime;
            float t = Mathf.Clamp01(resetAssistTimer / resetAssistDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            yaw = Mathf.LerpAngle(yaw, assistTargetYaw, eased * resetYawAssistStrength);
            pitch = Mathf.Lerp(pitch, assistTargetPitch, eased * resetPitchAssistStrength);
            currentDistance = Mathf.Lerp(
                currentDistance,
                assistTargetDistance,
                eased * resetDistanceAssistStrength
            );

            if (t >= 1f)
            {
                resetAssistActive = false;
            }
        }

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        float desiredDistance = ResolveCameraDistance(normalDistance);
        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, distanceSmooth * Time.deltaTime);

        cameraTransform.localPosition = new Vector3(0f, 0f, -currentDistance);
    }

    private void UpdateLockOnCamera()
    {
        Vector3 playerPos = player.position;
        Vector3 targetPos = lockTarget.position;

        Vector3 mid = (playerPos + targetPos) * 0.5f;
        Vector3 axis = targetPos - playerPos;
        float pairDistance = axis.magnitude;

        if (pairDistance > 0.0001f)
        {
            axis /= pairDistance;
        }
        else
        {
            axis = transform.forward;
        }

        yaw -= input.Look.x * yawSpeed * Time.deltaTime;
        pitch += input.Look.y * pitchSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, lockPitchMin, lockPitchMax);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // ここだけ yaw ベースに変更
        float desiredDistance = CalculateLockOnDistance(pairDistance);
        desiredDistance = ResolveCameraDistance(desiredDistance);

        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, distanceSmooth * Time.deltaTime);

        Vector3 desiredCameraWorldPos = mid - transform.forward * currentDistance;
        float signed = Vector3.Dot(desiredCameraWorldPos - mid, axis);

        float maxPivotOffset = pairDistance * maxPivotOffsetRatio;
        float pivotOffset = Mathf.Clamp(
            -signed * pivotSymmetryFactor,
            -maxPivotOffset,
            maxPivotOffset
        );

        Vector3 desiredPivot = mid + axis * pivotOffset;

        transform.position = desiredPivot;

        cameraTransform.localPosition = new Vector3(0f, 0f, -currentDistance);
    }

    // =========================================================
    // yaw ベースのロックオン距離
    //   - 背面/正面寄りでは短め
    //   - 真横寄りでは長め
    // =========================================================
    private float CalculateLockOnDistance(float pairDistance)
    {
        if (cam == null)
        {
            float fallback = pairDistance * lockDistanceMultiplier + lockDistanceOffset;
            return fallback;
        }

        Vector3 toTarget = lockTarget.position - player.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            float fallback = pairDistance * lockDistanceMultiplier + lockDistanceOffset;
            return fallback;
        }

        float axisYaw = Quaternion.LookRotation(toTarget).eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(axisYaw, yaw);

        // 0 = 背面/正面寄り, 1 = 真横寄り
        float sideFactor = Mathf.Abs(Mathf.Sin(yawDelta * Mathf.Deg2Rad));

        float verticalFovRad = cam.fieldOfView * Mathf.Deg2Rad;
        float horizontalFovRad = 2f * Mathf.Atan(Mathf.Tan(verticalFovRad * 0.5f) * cam.aspect);

        // 真横で両方を収めたい距離
        float distanceSide = (pairDistance * 0.5f) / Mathf.Tan(horizontalFovRad * 0.5f);

        // 通常のベース距離
        float baseDistance = pairDistance * lockDistanceMultiplier + lockDistanceOffset;

        // 背面/正面ではベース距離，真横ではFOVベース距離へ寄せる
        float desiredDistance = Mathf.Lerp(baseDistance, distanceSide, sideFactor);

        return desiredDistance;
    }

    private float ResolveCameraDistance(float desiredDistance)
    {
        Vector3 origin = transform.position;
        Vector3 dir = -transform.forward;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            cameraRadius,
            dir,
            hitBuffer,
            desiredDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );

        float nearest = desiredDistance;

        for (int i = 0; i < hitCount; i++)
        {
            Transform hitTransform = hitBuffer[i].transform;
            if (hitTransform == null) continue;

            Transform hitRoot = hitTransform.root;

            if (player != null && hitRoot == player.root) continue;
            if (lockTarget != null && hitRoot == lockTarget.root) continue;

            if (hitBuffer[i].distance < nearest)
            {
                nearest = hitBuffer[i].distance;
            }
        }

        return Mathf.Max(0.2f, nearest - collisionOffset);
    }

    private float NormalizePitch(float xAngle)
    {
        if (xAngle > 180f)
        {
            xAngle -= 360f;
        }

        return Mathf.Clamp(xAngle, minPitch, maxPitch);
    }

    private void BeginResetAssist(float targetYaw, float targetPitch, float targetDistance)
    {
        resetAssistActive = true;
        resetAssistTimer = 0f;

        assistTargetYaw = targetYaw;
        assistTargetPitch = targetPitch;
        assistTargetDistance = targetDistance;
    }
}
