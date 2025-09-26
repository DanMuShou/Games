using Godot;

public partial class HexMapCamera : Node3D
{
    private static HexMapCamera _instance;

    [Export] private HexGrid _grid;

    [Export(PropertyHint.Range, "0f,500f")]
    private float _moveSpeedMinZoom = 40, _moveSpeedMaxZoom = 10;

    [Export(PropertyHint.Range, "0f,30f")] private float _rotationSpeed = 10f;

    [Export] private float _stickMinZoom = 25f, _stickMaxZoom = 4.5f;
    [Export] private float _swivelMinZoom = -90, _swivelMaxZoom = -45;

    [Export] private Node3D _swivel, _stick;
    [Export] public Camera3D Camera { get; private set; }

    private float _zoom = 1.0f;
    private InputManager _input;

    public override void _EnterTree() => _instance = this;

    public void Init()
    {
        _input = InputManager.Instance;
    }

    public void PhyProcess(float delta)
    {
        if (_input.LockCamera)
            return;

        AdjustZoom(delta);
        AdjustRotation(delta);
        AdjustPosition(delta);
    }

    private void AdjustZoom(float delta)
    {
        if (!_input.IsZoom)
            return;

        var vec = _input.CameraZoomDir;
        _zoom = Mathf.Clamp(_zoom + vec * delta, 0f, 1f);

        var distance = Mathf.Lerp(_stickMinZoom, _stickMaxZoom, _zoom);
        _stick.Position = new Vector3(0, 0, distance);

        var angle = Mathf.Lerp(_swivelMinZoom, _swivelMaxZoom, _zoom);
        _swivel.RotationDegrees = new Vector3(angle, 0f, 0f);
    }

    private void AdjustPosition(float delta = 1)
    {
        if (!_input.IsMove)
            return;

        var vec = _input.CameraMoveDir;
        var direction = new Vector3(vec.X, 0, vec.Y).Normalized().Rotated(Vector3.Up, Rotation.Y);
        var distance = Mathf.Lerp(_moveSpeedMinZoom, _moveSpeedMaxZoom, _zoom) * delta;
        var targetPosition = ClampPosition(GlobalPosition + direction * distance);
        GlobalPosition = targetPosition;
    }

    private void AdjustRotation(float delta)
    {
        if (!_input.IsRotate)
            return;

        var rotaVec = _input.CameraRotateDir;
        var rotationAngleY = rotaVec * _rotationSpeed * delta;
        RotateY(-rotationAngleY / Mathf.Pi);
    }

    private Vector3 ClampPosition(Vector3 position)
    {
        var xMax = (_grid.CellCountX - 0.5f) * (2f * HexMetrics.InnerRadius);
        position.X = Mathf.Clamp(position.X, 0, xMax);

        var zMax = (_grid.CellCountZ - 1) * (1.5f * HexMetrics.OuterRadius);
        position.Z = Mathf.Clamp(position.Z, 0, zMax);

        return position;
    }

    public static void ValidatePosition()
    {
        _instance.GlobalPosition = _instance.ClampPosition(_instance.GlobalPosition);
    }
}