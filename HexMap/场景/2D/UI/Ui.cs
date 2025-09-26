using Godot;
using System;

public partial class Ui : Control
{
    [Export] private HexMapEditor _editor;
    [Export] private NewMapMenu _newMapMenu;
    [Export] private SaveLoadMenu _saveLoadMenu;

    public void Init(HexGrid hexGrid, HexMapCamera camera)
    {
        _editor.OnNewMapButPress += _newMapMenu.Open;
        _editor.OnSaveOrLoadButPress += (mode) => _saveLoadMenu.Open(mode);

        _editor.Init(hexGrid, camera);
        _newMapMenu.Init(hexGrid);
        _saveLoadMenu.Init(hexGrid);
    }

    public void PhyProcess()
    {
        _editor.PhyProcess();
    }
}