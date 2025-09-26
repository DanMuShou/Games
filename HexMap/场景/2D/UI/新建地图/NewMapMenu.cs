using Godot;

public partial class NewMapMenu : Control
{
    [Export] private Button _sBut, _mBut, _lBut, _closeBut;

    private HexGrid _hexGrid;

    public void Init(HexGrid hexGrid)
    {
        _hexGrid = hexGrid;

        _sBut.Pressed += () => CreateMap(20, 15);
        _mBut.Pressed += () => CreateMap(40, 30);
        _lBut.Pressed += () => CreateMap(80, 60);

        _closeBut.Pressed += Close;

        Close();
    }

    private void CreateMap(int x, int z)
    {
        _hexGrid.CreateMap(x, z);
        HexMapCamera.ValidatePosition();
        Close();
    }

    public void Open()
    {
        Show();
        ProcessMode = ProcessModeEnum.Inherit;

        InputManager.Instance.LockAll(true);
    }

    public void Close()
    {
        Hide();
        ProcessMode = ProcessModeEnum.Disabled;

        InputManager.Instance.LockAll(false);
    }
}