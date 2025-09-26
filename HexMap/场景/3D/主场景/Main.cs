using Godot;

public partial class Main : Node3D
{
    [Export] private HexGrid _hexGrid;
    [Export] private HexMapCamera _camera;
    [Export] private Ui _ui;

    [Export] private InputManager _input;

    public override void _Ready()
    {
        _input.Init();
        _hexGrid.Init();
        _camera.Init();
        _ui.Init(_hexGrid, _camera);
    }

    public override void _Process(double delta)
    {
        _input.Process();
    }

    public override void _PhysicsProcess(double delta)
    {
        _ui.PhyProcess();
        _camera.PhyProcess((float)delta);
    }
}