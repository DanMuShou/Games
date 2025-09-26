using System.IO;
using Godot;

public partial class HexGrid : Node3D
{
    [Export(PropertyHint.Range, "0,0,or_greater")]
    private ulong _seed;

    [Export] private bool _useRandom = false;
    [Export] public int CellCountX = 20, CellCountZ = 15;

    [Export] private Color[] _colors;

    [Export] private NoiseGenerator _noiseGenerator;

    [Export] private PackedScene _cellPacked;
    [Export] private PackedScene _chunkPacked;

    private HexGridChunk[] _chunks;
    private HexCell[] _cells;

    private int _chunkCountX, _chunkCountZ;

    public void Init()
    {
        HexMetrics.InitializeHashGrid(_seed);

        _noiseGenerator.CreateNoiseImage(
            _useRandom ? HexMetrics.Rng.Randi() : 1, new Vector2I(512, 512),
            out HexMetrics.NoiseSource);

        HexMetrics.Colors = _colors;

        CreateMap(CellCountX, CellCountZ);
    }


    private void CreateChunks()
    {
        _chunks = new HexGridChunk[_chunkCountX * _chunkCountZ];

        for (int z = 0, index = 0; z < _chunkCountZ; z++)
        for (var x = 0; x < _chunkCountX; x++)
        {
            var chunk = _chunkPacked.Instantiate<HexGridChunk>();
            _chunks[index++] = chunk;
            AddChild(chunk);
            chunk.Init();
        }
    }

    private void CreateCells()
    {
        _cells = new HexCell[CellCountX * CellCountZ];
        for (int z = 0, index = 0; z < CellCountZ; z++)
        for (var x = 0; x < CellCountX; x++)
        {
            CreateCell(x, z, index);
            index++;
        }
    }

    private void CreateCell(int x, int z, int index)
    {
        Vector3 position;
        //计算HexCell的位置 z * 0.5f - z / 2 运用int float的除法运算
        position.X = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
        position.Y = 0f;
        position.Z = z * (HexMetrics.OuterRadius * 1.5f);

        var cell = _cells[index] = _cellPacked.Instantiate<HexCell>();
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, _cells[index - 1]);
        }

        if (z > 0)
        {
            if (z % 2 == 0)
            {
                cell.SetNeighbor(HexDirection.NE, _cells[index - CellCountX]);
                if (x > 0)
                    cell.SetNeighbor(HexDirection.NW, _cells[index - CellCountX - 1]);
            }
            else
            {
                cell.SetNeighbor(HexDirection.NW, _cells[index - CellCountX]);
                if (x < CellCountX - 1)
                    cell.SetNeighbor(HexDirection.NE, _cells[index - CellCountX + 1]);
            }
        }

        AddCellToChunk(x, z, cell, position);
    }

    private void AddCellToChunk(int x, int z, HexCell cell, Vector3 position)
    {
        var chunkX = x / HexMetrics.ChunkSizeX;
        var chunkZ = z / HexMetrics.ChunkSizeZ;
        var chunk = _chunks[chunkX + chunkZ * _chunkCountX];

        var localX = x - chunkX * HexMetrics.ChunkSizeX;
        var localZ = z - chunkZ * HexMetrics.ChunkSizeZ;

        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);
        cell.SetInfoLabel(cell.Coordinates.ToString());
        cell.GlobalPosition = position;
        cell.Elevation = 0;
        cell.Chunk = chunk;
    }

    public HexCell GetCell(Vector3 position)
    {
        var coordinates = HexCoordinates.FromPosition(position);
        var index = coordinates.X + coordinates.Z * CellCountX + coordinates.Z / 2;
        return _cells[index];
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        var z = coordinates.Z;
        if (z < 0 || z >= CellCountZ)
            return null;

        var x = coordinates.X + z / 2;
        if (x < 0 || x >= CellCountX)
            return null;

        return _cells[x + z * CellCountX];
    }

    public void ShowUi(bool visible)
    {
        foreach (var cell in _cells)
            cell.ShowUi(visible);
    }

    public bool CreateMap(int x, int z)
    {
        if (x <= 0 || x % HexMetrics.ChunkSizeX != 0 ||
            z <= 0 || z % HexMetrics.ChunkSizeZ != 0)
        {
            GD.PrintErr("Invalid map size");
            return false;
        }

        if (_chunks != null)
            foreach (var chunk in _chunks)
                chunk.QueueFree();

        CellCountX = x;
        CellCountZ = z;

        _chunkCountX = CellCountX / HexMetrics.ChunkSizeX;
        _chunkCountZ = CellCountZ / HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();

        return true;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)CellCountX);
        writer.Write((byte)CellCountZ);

        foreach (var cell in _cells)
            cell.Save(writer);
    }

    public void Load(BinaryReader reader, int header)
    {
        var x = 20;
        var z = 15;
        if (header >= 1)
        {
            x = reader.ReadByte();
            z = reader.ReadByte();
        }

        if (x != CellCountX || z != CellCountZ)
        {
            if (!CreateMap(x, z))
                return;
        }

        foreach (var cell in _cells)
            cell.Load(reader);

        foreach (var chunk in _chunks)
            chunk.Refresh();

        ShowUi(false);
    }
}