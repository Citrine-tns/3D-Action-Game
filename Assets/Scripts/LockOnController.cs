using System.Collections.Generic;
using UnityEngine;

public class LockOnController : MonoBehaviour
{
    public enum LockState
    {
        Normal,
        Strafe,
        Locked,
        RetargetGrace
    }

    [Header("References")]
    public Transform player;
    public PlayerInputReader input;
    public CameraController cameraController;

    [Header("Search")]
    public float searchRadius = 20f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public string targetTag = "Enemy";

    [Header("Lock Rules")]
    public float loseLockDistance = 25f;
    public float retargetGraceTime = 1.0f;
    public float targetCheckOriginHeight = 1.2f;
    public float targetCheckTargetHeight = 1.0f;

    public LockState CurrentState { get; private set; } = LockState.Normal;
    public Transform CurrentTarget { get; private set; }

    public bool IsLocked => CurrentState == LockState.Locked;
    public bool IsStrafing => CurrentState == LockState.Strafe || CurrentState == LockState.Locked;

    private readonly List<Transform> candidates = new();
    private Transform lastReleasedTarget;
    private float graceTimer;

    private void Update()
    {
        switch (CurrentState)
        {
            case LockState.Normal:
                UpdateNormal();
                break;

            case LockState.Strafe:
                UpdateStrafe();
                break;

            case LockState.Locked:
                UpdateLocked();
                break;

            case LockState.RetargetGrace:
                UpdateRetargetGrace();
                break;
        }
    }

    private void UpdateNormal()
    {
        if (input.LockPressedThisFrame)
        {
            bool locked = TryAcquireTarget(skipTarget: null);

            if (locked)
            {
                CurrentState = LockState.Locked;
                cameraController.EnterLockOn(CurrentTarget);
            }
            else
            {
                CurrentState = LockState.Strafe;
                cameraController.ResetToStrafeView();
            }
        }
    }

    private void UpdateStrafe()
    {
        if (!input.LockHeld)
        {
            CurrentState = LockState.Normal;
            return;
        }

        // Strafe中に新規敵が入ってきても，自動再探索はしない
        // 仕様通り「押した瞬間」に探索した結果だけ使う
    }

    private void UpdateLocked()
    {
        if (CurrentTarget == null)
        {
            CurrentTarget = null;
            cameraController.ExitLockOnKeepCurrentView();
            CurrentState = LockState.Strafe;
            return;
        }

        if (input.LockReleasedThisFrame)
        {
            lastReleasedTarget = CurrentTarget;
            CurrentTarget = null;

            CurrentState = LockState.RetargetGrace;
            graceTimer = retargetGraceTime;

            cameraController.ExitLockOnToNormal();
            return;
        }

        if (IsTargetLost(CurrentTarget))
        {
            lastReleasedTarget = CurrentTarget;
            CurrentTarget = null;

            cameraController.ExitLockOnKeepCurrentView();
            CurrentState = LockState.Strafe;
            return;
        }

        cameraController.UpdateLockOnTarget(CurrentTarget);
    }

    private void UpdateRetargetGrace()
    {
        graceTimer -= Time.deltaTime;

        if (input.LockPressedThisFrame)
        {
            bool locked = TryAcquireTarget(skipTarget: lastReleasedTarget);

            if (locked)
            {
                CurrentState = LockState.Locked;
                cameraController.EnterLockOn(CurrentTarget);
            }
            else
            {
                CurrentState = LockState.Strafe;
                // ここではリセットしない
            }

            return;
        }

        if (graceTimer <= 0f)
        {
            lastReleasedTarget = null;
            CurrentState = LockState.Normal;
        }
    }

    private void LoseLockToStrafe()
    {
        lastReleasedTarget = CurrentTarget;
        CurrentTarget = null;

        cameraController.ExitLockOnKeepCurrentView();
        CurrentState = LockState.Strafe;
    }

    private bool TryAcquireTarget(Transform skipTarget)
    {
        RefreshCandidates();

        for (int i = 0; i < candidates.Count; i++)
        {
            Transform candidate = candidates[i];
            if (candidate == null) continue;
            if (candidate == skipTarget) continue;

            if (IsOccluded(candidate))
                continue;

            CurrentTarget = candidate;
            return true;
        }

        CurrentTarget = null;
        return false;
    }

    private void RefreshCandidates()
    {
        candidates.Clear();

        Collider[] hits = Physics.OverlapSphere(player.position, searchRadius, targetMask);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag(targetTag))
                continue;

            Transform t = hit.transform;
            if (t == null) continue;

            candidates.Add(t);
        }

        candidates.Sort((a, b) =>
        {
            float da = (a.position - player.position).sqrMagnitude;
            float db = (b.position - player.position).sqrMagnitude;
            return da.CompareTo(db);
        });
    }

    private bool IsTargetLost(Transform target)
    {
        if (target == null) return true;

        float distSqr = (target.position - player.position).sqrMagnitude;
        if (distSqr > loseLockDistance * loseLockDistance)
            return true;

        if (IsOccluded(target))
            return true;

        return false;
    }

    private bool IsOccluded(Transform target)
    {
        Vector3 origin = player.position + Vector3.up * targetCheckOriginHeight;
        Vector3 targetPoint = target.position + Vector3.up * targetCheckTargetHeight;

        Vector3 dir = targetPoint - origin;
        float distance = dir.magnitude;
        if (distance <= 0.01f) return false;

        dir /= distance;

        return Physics.Raycast(origin, dir, distance, obstacleMask);
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, searchRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, loseLockDistance);
    }
}
