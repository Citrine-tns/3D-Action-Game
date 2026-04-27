using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot;
    public LockOnController lockOn;
    public PlayerInputReader input;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 10f;
    public float gravity = -9.8f;

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Vector2 moveInput = input.Move;

        Vector3 moveDir = lockOn.IsStrafing
            ? GetMoveRelativeToPlayer(moveInput)
            : GetMoveRelativeToCamera(moveInput);

        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        HandleRotation(moveDir);
        HandleMovement(moveDir);
    }

    private Vector3 GetMoveRelativeToCamera(Vector2 inputMove)
    {
        Vector3 forward = cameraPivot.forward;
        Vector3 right   = cameraPivot.right;

        forward.y = 0f;
        right.y   = 0f;

        forward.Normalize();
        right.Normalize();

        return forward * inputMove.y + right * inputMove.x;
    }

    private Vector3 GetMoveRelativeToPlayer(Vector2 inputMove)
    {
        Vector3 forward = transform.forward;
        Vector3 right   = transform.right;

        forward.y = 0f;
        right.y   = 0f;

        forward.Normalize();
        right.Normalize();

        return forward * inputMove.y + right * inputMove.x;
    }

    private void HandleRotation(Vector3 moveDir)
    {
        if (lockOn.IsLocked)
        {
            Transform target = lockOn.CurrentTarget;
            if (target == null) return;

            Vector3 lookDir = target.position - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude < 0.0001f) return;

            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
            return;
        }

        // Strafe中は向き固定（回転しない）
        if (lockOn.IsStrafing) return;

        if (moveDir.sqrMagnitude < 0.0001f) return;

        Quaternion freeRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            freeRot,
            rotationSpeed * Time.deltaTime
        );
    }

    private void HandleMovement(Vector3 moveDir)
    {
        Vector3 velocity = moveDir * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -1f;

        verticalVelocity += gravity * Time.deltaTime;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
}
