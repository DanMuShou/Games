using System;
using System.IO;
using Godot;

public partial class HexMapEditor : Control
{
    [Signal]
    public delegate void OnNewMapButPressEventHandler();

    [Signal]
    public delegate void OnSaveOrLoadButPressEventHandler(bool mode);

    [Export] private ColorToggle
        _colorInit;

    [Export] private OptionalToggle
        _riverInit, _roadInit, _wallInit;

    [Export] private bool _printInfo;

    [Export] private EnableLevelComponent
        _elevation, _water, _urban, _farm, _plant, _special;

    [Export] private OptionSelectComponent
        _color, _river, _road, _wall;

    [Export] private Label _brushSizeLabel;
    [Export] private HSlider _brushSizeSelector;

    [Export] private CheckButton _labelVisualCheck;
    [Export] private Button _saveBut, _loadBut, _newMapBut;

    private enum OptionalToggle
    {
        Ignore,
        Yes,
        No
    }

    private enum ColorToggle
    {
        None,
        White,
        Red,
        Green,
        Blue,
        Yellow,
        Orange,
        Purple,
        Black
    }

    private int _brushSize;
    private bool _isDrag;
    private HexCell _previousCell;
    private HexDirection _dragDirection;

    private HexGrid _hexGrid;
    private HexMapCamera _camera;
    private InputManager _input;

    public void Init(HexGrid hexGrid, HexMapCamera camera)
    {
        _hexGrid = hexGrid;
        _camera = camera;
        _input = InputManager.Instance;

        _brushSizeSelector.ValueChanged += OnBrushSizeChanged;
        _labelVisualCheck.Pressed += () => _hexGrid.ShowUi(_labelVisualCheck.ButtonPressed);

        _saveBut.Pressed += () => EmitSignalOnSaveOrLoadButPress(true);
        _loadBut.Pressed += () => EmitSignalOnSaveOrLoadButPress(false);

        _newMapBut.Pressed += EmitSignalOnNewMapButPress;

        CompletionUiComponent();

        OnBrushSizeChanged(0);
        _hexGrid.ShowUi(false);
    }


    private void CompletionUiComponent()
    {
        _elevation.Init("地高", (0, 5, 0));
        _water.Init("水面", (0, 5, 0));
        _urban.Init("城镇", (0, 3, 0));
        _farm.Init("村庄", (0, 3, 0));
        _plant.Init("植被", (0, 3, 0));
        _special.Init("特殊", (0, 3, 0));

        var colorToggle = GetEnumNames(typeof(ColorToggle));
        _color.Init("颜色", (colorToggle, (int)_colorInit));

        var optionalToggle = GetEnumNames(typeof(OptionalToggle));
        _river.Init("河流", (optionalToggle, (int)_riverInit));
        _road.Init("道路", (optionalToggle, (int)_roadInit));
        _wall.Init("城墙", (optionalToggle, (int)_wallInit));
    }

    public void PhyProcess()
    {
        if (_input.LockEdit)
            return;

        if (_input.IsClick && !_input.IsMouseOnUi(this))
            SelectCell();
        else
            _previousCell = null;
    }

    private void SelectCell()
    {
        var from = _camera.Camera.ProjectRayOrigin(GetViewport().GetMousePosition());
        var to = from + _camera.Camera.ProjectRayNormal(GetViewport().GetMousePosition()) * 400.0f;

        var spaceState = _hexGrid.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = true;
        var result = spaceState.IntersectRay(query);

        if (result.Count == 0)
        {
            _previousCell = null;
            return;
        }

        if (result.TryGetValue("position", out var position))
        {
            var currentCell = _hexGrid.GetCell((Vector3)position);

            if (_previousCell != null && _previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else
            {
                _isDrag = false;
            }

            EditCells(currentCell);
            _previousCell = currentCell;
        }
        else
        {
            _previousCell = null;
        }

        if (_printInfo)
        {
            GD.Print("result :" + result);
            GD.Print();
        }
    }

    private void EditCells(HexCell center)
    {
        var centerX = center.Coordinates.X;
        var centerZ = center.Coordinates.Z;

        for (int r = 0, z = centerZ - _brushSize; z <= centerZ; z++, r++)
        for (var x = centerX - r; x <= centerX + _brushSize; x++)
        {
            EditCell(_hexGrid.GetCell(new HexCoordinates(x, z)));
        }

        for (int r = 0, z = centerZ + _brushSize; z > centerZ; z--, r++)
        for (var x = centerX - _brushSize; x <= centerX + r; x++)
        {
            EditCell(_hexGrid.GetCell(new HexCoordinates(x, z)));
        }
    }

    private void EditCell(HexCell cell)
    {
        if (cell == null)
            return;


        if (_elevation.Enable)
            cell.Elevation = _elevation.Level;
        if (_water.Enable)
            cell.WaterLevel = _water.Level;

        if (_urban.Enable)
            cell.UrbanLevel = _urban.Level;
        if (_farm.Enable)
            cell.FarmLevel = _farm.Level;
        if (_plant.Enable)
            cell.PlantLevel = _plant.Level;

        if (_special.Enable)
            cell.SpecialIndex = _special.Level;

        if (_color.ModeIndex != (int)ColorToggle.None)
            cell.TerrainTypeIndex = _color.ModeIndex - 1;
        if (_river.ModeIndex == (int)OptionalToggle.No)
            cell.RemoveRiver();
        if (_road.ModeIndex == (int)OptionalToggle.No)
            cell.RemoveRoads();
        if (_wall.ModeIndex != (int)OptionalToggle.Ignore)
            cell.Walled = _wall.ModeIndex == (int)OptionalToggle.Yes;


        if (_isDrag)
        {
            var otherCell = cell.GetNeighbor(_dragDirection.Opposite());
            if (otherCell == null) return;

            if (_river.ModeIndex == (int)OptionalToggle.Yes)
                otherCell.SetOutgoingRiver(_dragDirection);

            if (_road.ModeIndex == (int)OptionalToggle.Yes)
                otherCell.AddRoads(_dragDirection);
        }
    }

    private void ValidateDrag(HexCell currentCell)
    {
        for (_dragDirection = HexDirection.SW; _dragDirection <= HexDirection.SE; _dragDirection++)
        {
            if (_previousCell.GetNeighbor(_dragDirection) != currentCell)
                continue;

            _isDrag = true;
            return;
        }

        _isDrag = false;
    }

    private string[] GetEnumNames(Type enumType)
    {
        return Enum.GetNames(enumType);
    }

    private void OnBrushSizeChanged(double value)
    {
        _brushSize = (int)value;
        _brushSizeLabel.Text = $"笔刷大小 : R = {_brushSize}";
    }
}