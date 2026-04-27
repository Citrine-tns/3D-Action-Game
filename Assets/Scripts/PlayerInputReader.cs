using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }

    public bool AttackPressedThisFrame { get; private set; }

    public bool LockHeld { get; private set; }
    public bool LockPressedThisFrame { get; private set; }
    public bool LockReleasedThisFrame { get; private set; }

    private PlayerInputActions input;

    // 攻撃入力（.inputactions を再生成せずに追加）
    private InputAction attackAction;

    private void Awake()
    {
        input = new PlayerInputActions();

        input.Player.Move.performed += ctx => Move = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled  += _   => Move = Vector2.zero;

        input.Player.CameraMove.performed += ctx => Look = ctx.ReadValue<Vector2>();
        input.Player.CameraMove.canceled  += _   => Look = Vector2.zero;

        input.Player.TargetLock.performed += _ =>
        {
            if (!LockHeld)
                LockPressedThisFrame = true;

            LockHeld = true;
        };

        input.Player.TargetLock.canceled += _ =>
        {
            LockHeld = false;
            LockReleasedThisFrame = true;
        };

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Keyboard>/enter");
        attackAction.AddBinding("<Mouse>/leftButton");
        attackAction.AddBinding("<SwitchProControllerHID>/buttonWest");
        attackAction.performed += _ => AttackPressedThisFrame = true;
    }

    private void OnEnable()
    {
        input.Enable();
        attackAction.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
        attackAction.Disable();
    }
    private void OnDestroy() => input?.Dispose();

    private void LateUpdate()
    {
        AttackPressedThisFrame = false;
        LockPressedThisFrame = false;
        LockReleasedThisFrame = false;
    }
}
