using Godot;

public partial class InputManager : Node
{
    public static InputManager Instance { get; private set; }

    public bool LockEdit { get; set; }
    public bool LockCamera { get; set; }

    public bool IsZoom => CameraZoomDir != 0f;
    public bool IsRotate => CameraRotateDir != 0f;
    public bool IsMove => CameraMoveDir != Vector2.Zero;

    public bool IsClick { get; private set; }
    public float CameraZoomDir { get; private set; }
    public float CameraRotateDir { get; private set; }
    public Vector2 CameraMoveDir { get; private set; }

    public void Init()
    {
        Instance = this;
    }

    public void Process()
    {
        CameraZoomDir = Input.GetAxis(
            InputInformation.MapZoomUp, InputInformation.MapZoomDown);

        CameraRotateDir = Input.GetAxis(
            InputInformation.MapRotateRight, InputInformation.MapRotateLeft);

        CameraMoveDir = Input.GetVector(
            InputInformation.MapMoveLeft, InputInformation.MapMoveRight,
            InputInformation.MapMoveUp, InputInformation.MapMoveDown);

        IsClick = Input.IsActionPressed(InputInformation.MouseClickLeft);
    }

    public bool IsMouseOnUi(Control ui)
        => ui.GetGlobalRect().HasPoint(GetViewport().GetMousePosition());

    private float GetMouseScroll()
    {
        if (Input.IsActionJustReleased(InputInformation.MouseScrollWheelUp))
            return 1f;
        if (Input.IsActionJustReleased(InputInformation.MouseScrollWheelDown))
            return -1f;

        return 0f;
    }

    public void LockAll(bool lockAll)
    {
        LockEdit = lockAll;
        LockCamera = lockAll;
    }
}