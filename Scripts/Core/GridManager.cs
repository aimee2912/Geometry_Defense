using System.Collections.Generic;
using Godot;

namespace VectorBastion;

/// <summary>
/// Manages the 2D buildable grid, generates the fixed neon Path2D route,
/// and renders the minimalist coordinate grid, glowing track, and entry portal.
/// </summary>
public partial class GridManager : Node2D
{
	public const int CellSize = 48;
	public const int Columns = 24;
	public const int Rows = 11;
	public static readonly Vector2 GridOrigin = new(64f, 68f);

	public Path2D EnemyPath { get; private set; }
	public Vector2 EntryPosition => CellToWorld(PathWaypoints[0]);
	public Vector2 CorePosition => CellToWorld(PathWaypoints[^1]);

	private static readonly Vector2I[] PathWaypoints =
	{
		new(0, 2),
		new(5, 2),
		new(5, 8),
		new(11, 8),
		new(11, 2),
		new(17, 2),
		new(17, 8),
		new(23, 8)
	};

	private readonly HashSet<Vector2I> _pathCells = new();
	private readonly Dictionary<Vector2I, BaseTower> _occupiedCells = new();
	private Vector2[] _pathWorldPoints;
	private float _pulseTime;

	public override void _Ready()
	{
		BuildPathData();
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		_pulseTime += (float)delta;
		QueueRedraw();
	}

	private void BuildPathData()
	{
		_pathCells.Clear();
		_pathWorldPoints = new Vector2[PathWaypoints.Length];

		EnemyPath = new Path2D { Name = "EnemyPath" };
		var curve = new Curve2D();

		for (int i = 0; i < PathWaypoints.Length; i++)
		{
			Vector2 world = CellToWorld(PathWaypoints[i]);
			_pathWorldPoints[i] = world;
			curve.AddPoint(world);

			if (i < PathWaypoints.Length - 1)
			{
				MarkSegmentCells(PathWaypoints[i], PathWaypoints[i + 1]);
			}
		}

		EnemyPath.Curve = curve;
		AddChild(EnemyPath);
	}

	private void MarkSegmentCells(Vector2I a, Vector2I b)
	{
		int x = a.X;
		int y = a.Y;
		int dx = Mathf.Sign(b.X - a.X);
		int dy = Mathf.Sign(b.Y - a.Y);

		_pathCells.Add(new Vector2I(x, y));
		while (x != b.X || y != b.Y)
		{
			x += dx;
			y += dy;
			_pathCells.Add(new Vector2I(x, y));
		}
	}

	public bool IsInsideGrid(Vector2I cell) =>
		cell.X >= 0 && cell.X < Columns && cell.Y >= 0 && cell.Y < Rows;

	public Vector2I WorldToCell(Vector2 worldPos)
	{
		Vector2 local = worldPos - GridOrigin;
		return new Vector2I(
			Mathf.FloorToInt(local.X / CellSize),
			Mathf.FloorToInt(local.Y / CellSize)
		);
	}

	public Vector2 CellToWorld(Vector2I cell) =>
		GridOrigin + new Vector2(cell.X * CellSize + CellSize * 0.5f, cell.Y * CellSize + CellSize * 0.5f);

	public bool CanBuildAt(Vector2I cell) =>
		IsInsideGrid(cell) && !_pathCells.Contains(cell) && !_occupiedCells.ContainsKey(cell);

	public void RegisterTower(Vector2I cell, BaseTower tower)
	{
		_occupiedCells[cell] = tower;
	}

	public void UnregisterTower(Vector2I cell)
	{
		_occupiedCells.Remove(cell);
	}

	public BaseTower GetTowerAt(Vector2I cell) =>
		_occupiedCells.TryGetValue(cell, out var tower) ? tower : null;

	public IEnumerable<BaseTower> GetAllTowers() => _occupiedCells.Values;

	public void ClearAllTowers()
	{
		foreach (var tower in _occupiedCells.Values)
		{
			if (IsInstanceValid(tower))
				tower.QueueFree();
		}
		_occupiedCells.Clear();
	}

	public override void _Draw()
	{
		// 1. Subtle coordinate grid
		var gridColor = new Color(0.11f, 0.14f, 0.22f, 0.55f);
		var gridRect = new Rect2(GridOrigin, new Vector2(Columns * CellSize, Rows * CellSize));
		DrawRect(gridRect, new Color(0.055f, 0.062f, 0.095f, 0.85f), true);

		for (int x = 0; x <= Columns; x++)
		{
			float px = GridOrigin.X + x * CellSize;
			DrawLine(new Vector2(px, GridOrigin.Y), new Vector2(px, GridOrigin.Y + Rows * CellSize), gridColor, 1f);
		}
		for (int y = 0; y <= Rows; y++)
		{
			float py = GridOrigin.Y + y * CellSize;
			DrawLine(new Vector2(GridOrigin.X, py), new Vector2(GridOrigin.X + Columns * CellSize, py), gridColor, 1f);
		}

		// Outer frame
		DrawRect(gridRect, new Color(0.18f, 0.28f, 0.44f, 0.75f), false, 2f);

		// 2. Path cells dark fill
		var pathFill = new Color(0.075f, 0.095f, 0.15f, 0.95f);
		foreach (var cell in _pathCells)
		{
			var rect = new Rect2(GridOrigin + new Vector2(cell.X * CellSize, cell.Y * CellSize), new Vector2(CellSize, CellSize));
			DrawRect(rect, pathFill, true);
		}

		// 3. Glowing neon path line
		if (_pathWorldPoints != null && _pathWorldPoints.Length > 1)
		{
			DrawPolyline(_pathWorldPoints, new Color(0.08f, 0.55f, 0.85f, 0.28f), 26f, true);
			DrawPolyline(_pathWorldPoints, new Color(0.15f, 0.78f, 1.0f, 0.55f), 4f, true);
			DrawPolyline(_pathWorldPoints, new Color(0.55f, 0.95f, 1.25f, 0.95f), 1.8f, true);

			// Animated flow pulses along path
			if (EnemyPath?.Curve != null)
			{
				float totalLen = EnemyPath.Curve.GetBakedLength();
				const float spacing = 96f;
				float offset = (_pulseTime * 55f) % spacing;
				for (float d = offset; d < totalLen; d += spacing)
				{
					Vector2 p = EnemyPath.Curve.SampleBaked(d);
					DrawCircle(p, 3.2f, new Color(0.35f, 1.15f, 1.35f, 0.85f));
				}
			}
		}

		// 4. Entry Portal visual (rotating neon diamond/ring)
		DrawEntryPortal(EntryPosition);
	}

	private void DrawEntryPortal(Vector2 pos)
	{
		float pulse = 0.85f + 0.15f * Mathf.Sin(_pulseTime * 4.5f);
		DrawArc(pos, 18f * pulse, 0, Mathf.Tau, 24, new Color(1.3f, 0.22f, 0.45f, 0.95f), 2.5f, true);

		Vector2[] tri = new Vector2[4];
		for (int i = 0; i < 3; i++)
		{
			float a = _pulseTime * 2.2f + i * Mathf.Tau / 3f;
			tri[i] = pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 11f;
		}
		tri[3] = tri[0];
		DrawPolyline(tri, new Color(1.4f, 0.45f, 0.65f, 0.95f), 2f, true);
	}
}
