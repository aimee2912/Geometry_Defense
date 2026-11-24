using System.Collections.Generic;
using Godot;

namespace VectorBastion;

/// <summary>
/// Handles tower placement preview, grid snapping, currency validation, tower selection,
/// upgrading, selling, and geometric resonant links between adjacent same-type towers.
/// </summary>
public partial class BuildManager : Node2D
{
	public TowerData PendingBuildTower { get; private set; }
	public BaseTower SelectedTower { get; private set; }

	private GridManager _grid;
	private WaveManager _waveManager;
	private List<TowerData> _catalog = new();
	private Vector2I _hoveredCell = new(-1, -1);
	private bool _hoverValid;
	private readonly List<(Vector2 A, Vector2 B, Color Color)> _resonantLines = new();

	public void Initialize(GridManager grid, WaveManager waveManager, List<TowerData> catalog)
	{
		_grid = grid;
		_waveManager = waveManager;
		_catalog = catalog;
		ZIndex = 3;
	}

	public void ResetState()
	{
		PendingBuildTower = null;
		SelectTower(null);
		_resonantLines.Clear();
		QueueRedraw();
	}

	public void SetBuildSelection(TowerData data)
	{
		PendingBuildTower = data;
		if (data != null)
			SelectTower(null);
		QueueRedraw();
	}

	public void CancelBuildSelection()
	{
		PendingBuildTower = null;
		QueueRedraw();
	}

	public void SelectTower(BaseTower tower)
	{
		if (IsInstanceValid(SelectedTower))
			SelectedTower.IsSelected = false;

		SelectedTower = tower;

		if (IsInstanceValid(SelectedTower))
			SelectedTower.IsSelected = true;

		SignalBus.Instance?.EmitSignal(SignalBus.SignalName.TowerSelected, tower);
		QueueRedraw();
	}

	public bool UpgradeSelectedTower()
	{
		if (!IsInstanceValid(SelectedTower))
			return false;

		bool ok = SelectedTower.TryUpgrade();
		if (ok)
		{
			SignalBus.Instance?.EmitSignal(SignalBus.SignalName.TowerSelected, SelectedTower);
		}
		return ok;
	}

	public void CycleSelectedTowerTargeting()
	{
		if (!IsInstanceValid(SelectedTower))
			return;

		SelectedTower.CycleTargetingMode();
		SignalBus.Instance?.EmitSignal(SignalBus.SignalName.TowerSelected, SelectedTower);
	}

	public void SellSelectedTower()
	{
		if (!IsInstanceValid(SelectedTower))
			return;

		int refund = SelectedTower.SellValue;
		Vector2I cell = SelectedTower.GridCell;
		Vector2 pos = SelectedTower.GlobalPosition;
		Color col = SelectedTower.Data.PrimaryColor;

		_grid.UnregisterTower(cell);
		SelectedTower.QueueFree();
		SelectTower(null);

		GameManager.Instance.AddVertices(refund);
		SoundManager.Instance?.PlaySell();
		GetTree().CurrentScene?.AddChild(BurstEffect.CreateExplosion(pos, col, 8, 24f, 0.25f));

		RecalculateResonantLinks();
	}

	public override void _Process(double delta)
	{
		if (_grid == null) return;

		Vector2 mousePos = GetGlobalMousePosition();
		_hoveredCell = _grid.WorldToCell(mousePos);
		_hoverValid = PendingBuildTower != null
			&& _grid.CanBuildAt(_hoveredCell)
			&& GameManager.Instance.CanAfford(PendingBuildTower.Cost);

		QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (GameManager.Instance.IsGameOver)
			return;

		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.Escape)
			{
				CancelBuildSelection();
				SelectTower(null);
			}
			else if (key.Keycode == Key.Key1 && _catalog.Count > 0) SetBuildSelection(_catalog[0]);
			else if (key.Keycode == Key.Key2 && _catalog.Count > 1) SetBuildSelection(_catalog[1]);
			else if (key.Keycode == Key.Key3 && _catalog.Count > 2) SetBuildSelection(_catalog[2]);
			else if (key.Keycode == Key.Key4 && _catalog.Count > 3) SetBuildSelection(_catalog[3]);
			return;
		}

		if (@event is InputEventMouseButton mb && mb.Pressed)
		{
			if (mb.ButtonIndex == MouseButton.Right)
			{
				CancelBuildSelection();
				SelectTower(null);
				return;
			}

			if (mb.ButtonIndex == MouseButton.Left)
			{
				Vector2I cell = _grid.WorldToCell(GetGlobalMousePosition());
				if (!_grid.IsInsideGrid(cell))
				{
					SelectTower(null);
					return;
				}

				if (PendingBuildTower != null)
				{
					TryPlaceTower(cell);
				}
				else
				{
					BaseTower existing = _grid.GetTowerAt(cell);
					SelectTower(existing);
				}
			}
		}
	}

	private void TryPlaceTower(Vector2I cell)
	{
		if (PendingBuildTower == null || !_grid.CanBuildAt(cell))
			return;

		if (!GameManager.Instance.SpendVertices(PendingBuildTower.Cost))
			return;

		var tower = new BaseTower();
		tower.GlobalPosition = _grid.CellToWorld(cell);
		tower.Initialize(PendingBuildTower, cell, _waveManager);
		_grid.AddChild(tower);
		_grid.RegisterTower(cell, tower);

		SoundManager.Instance?.PlayPlace();
		GetTree().CurrentScene?.AddChild(
			BurstEffect.CreateShockwave(tower.GlobalPosition, PendingBuildTower.PrimaryColor, 34f, 0.25f));

		RecalculateResonantLinks();
		SelectTower(tower);

		// Keep placing if player still has enough vertices and holds Shift, otherwise clear selection
		if (!Input.IsKeyPressed(Key.Shift))
		{
			PendingBuildTower = null;
		}
	}

	/// <summary>
	/// Links orthogonally or diagonally adjacent towers of the same polygon type
	/// into resonant fields (+10% fire rate per link, up to +30%).
	/// </summary>
	private void RecalculateResonantLinks()
	{
		_resonantLines.Clear();
		var towers = new List<BaseTower>(_grid.GetAllTowers());
		foreach (var t in towers)
			t.ResonantLinks = 0;

		for (int i = 0; i < towers.Count; i++)
		{
			for (int j = i + 1; j < towers.Count; j++)
			{
				var a = towers[i];
				var b = towers[j];
				if (a.Data.Type != b.Data.Type) continue;

				Vector2I diff = a.GridCell - b.GridCell;
				if (Mathf.Abs(diff.X) <= 1 && Mathf.Abs(diff.Y) <= 1)
				{
					a.ResonantLinks++;
					b.ResonantLinks++;
					_resonantLines.Add((a.GlobalPosition, b.GlobalPosition, a.Data.PrimaryColor));
				}
			}
		}
	}

	public override void _Draw()
	{
		// Draw resonant link beams between adjacent same-type towers
		foreach (var link in _resonantLines)
		{
			var c = link.Color;
			DrawLine(link.A, link.B, new Color(c.R, c.G, c.B, 0.25f), 5f, true);
			DrawLine(link.A, link.B, new Color(c.R * 1.3f, c.G * 1.3f, c.B * 1.3f, 0.75f), 1.6f, true);
		}

		// Draw translucent placement preview
		if (PendingBuildTower != null && _grid != null && _grid.IsInsideGrid(_hoveredCell))
		{
			Vector2 center = _grid.CellToWorld(_hoveredCell);
			Color tint = _hoverValid ? new Color(0.15f, 1.15f, 0.45f) : new Color(1.25f, 0.2f, 0.25f);

			// Tile highlight
			var tileRect = new Rect2(
				center - new Vector2(GridManager.CellSize * 0.5f, GridManager.CellSize * 0.5f),
				new Vector2(GridManager.CellSize, GridManager.CellSize));
			DrawRect(tileRect, new Color(tint.R, tint.G, tint.B, 0.16f), true);
			DrawRect(tileRect, new Color(tint.R, tint.G, tint.B, 0.8f), false, 1.8f);

			// Range preview
			DrawCircle(center, PendingBuildTower.Range, new Color(tint.R, tint.G, tint.B, 0.07f));
			DrawArc(center, PendingBuildTower.Range, 0f, Mathf.Tau, 64, new Color(tint.R, tint.G, tint.B, 0.55f), 1.5f, true);

			// Ghost polygon
			int sides = PendingBuildTower.Sides;
			var loop = new Vector2[sides + 1];
			for (int i = 0; i < sides; i++)
			{
				float a = -Mathf.Pi * 0.5f + i * Mathf.Tau / sides;
				loop[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 16f;
			}
			loop[sides] = loop[0];
			DrawPolyline(loop, new Color(tint.R, tint.G, tint.B, 0.9f), 2.2f, true);
		}
	}
}
