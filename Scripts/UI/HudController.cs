using System.Collections.Generic;
using Godot;

namespace VectorBastion;

/// <summary>
/// Procedural neon HUD: Top bar (Vertices, Core, Wave, Start Wave, 1x/2x, Pause),
/// Bottom Build Dock (4 polygon towers), Right Context Panel (Upgrade, Target Mode, Sell),
/// and Victory/Defeat modal overlay.
/// </summary>
public partial class HudController : CanvasLayer
{
	private BuildManager _buildManager;
	private WaveManager _waveManager;
	private List<TowerData> _catalog;

	// Top bar widgets
	private Label _verticesLabel;
	private Label _coreLabel;
	private Label _waveLabel;
	private Label _statusLabel;
	private Button _startWaveBtn;
	private Button _speedBtn;
	private Button _pauseBtn;

	// Build dock widgets
	private readonly List<(Button Btn, TowerData Data)> _buildButtons = new();

	// Context panel widgets
	private PanelContainer _contextPanel;
	private Label _selTitleLabel;
	private Label _selStatsLabel;
	private Button _upgradeBtn;
	private Button _targetBtn;
	private Button _sellBtn;
	private BaseTower _selectedTower;

	// Game over overlay
	private ColorRect _gameOverOverlay;
	private Label _gameOverTitle;
	private Label _gameOverStats;

	public void Initialize(BuildManager buildManager, WaveManager waveManager, List<TowerData> catalog)
	{
		_buildManager = buildManager;
		_waveManager = waveManager;
		_catalog = catalog;
		ProcessMode = ProcessModeEnum.Always;

		BuildUI();
		ConnectSignals();
		RefreshAll();
	}

	private void ConnectSignals()
	{
		if (SignalBus.Instance == null) return;
		SignalBus.Instance.CurrencyChanged += OnCurrencyChanged;
		SignalBus.Instance.CoreDamaged += OnCoreDamaged;
		SignalBus.Instance.WaveStarted += OnWaveStarted;
		SignalBus.Instance.WaveCompleted += OnWaveCompleted;
		SignalBus.Instance.TowerSelected += OnTowerSelected;
		SignalBus.Instance.GameOver += OnGameOver;
		SignalBus.Instance.GameReset += OnGameReset;
	}

	public override void _ExitTree()
	{
		if (SignalBus.Instance == null) return;
		SignalBus.Instance.CurrencyChanged -= OnCurrencyChanged;
		SignalBus.Instance.CoreDamaged -= OnCoreDamaged;
		SignalBus.Instance.WaveStarted -= OnWaveStarted;
		SignalBus.Instance.WaveCompleted -= OnWaveCompleted;
		SignalBus.Instance.TowerSelected -= OnTowerSelected;
		SignalBus.Instance.GameOver -= OnGameOver;
		SignalBus.Instance.GameReset -= OnGameReset;
	}

	private void BuildUI()
	{
		var root = new Control
		{
			Name = "HudRoot",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		BuildTopBar(root);
		BuildBottomDock(root);
		BuildContextPanel(root);
		BuildGameOverModal(root);
	}

	private void BuildTopBar(Control root)
	{
		var topPanel = new PanelContainer
		{
			Position = new Vector2(0, 0),
			Size = new Vector2(1280, 56)
		};
		topPanel.AddThemeStyleboxOverride("panel", MakePanelStyle(
			new Color(0.06f, 0.075f, 0.12f, 0.94f),
			new Color(0.15f, 0.45f, 0.65f, 0.7f),
			borderBottom: 2));
		root.AddChild(topPanel);

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 18);
		topPanel.AddChild(hbox);

		var padLeft = new Control { CustomMinimumSize = new Vector2(8, 0) };
		hbox.AddChild(padLeft);

		var title = MakeLabel("VECTOR BASTION", 17, new Color(0.35f, 0.95f, 1.0f));
		hbox.AddChild(title);

		hbox.AddChild(MakeSeparator());

		_verticesLabel = MakeLabel("VERTICES: 150", 16, new Color(0.95f, 0.95f, 0.45f));
		hbox.AddChild(_verticesLabel);

		_coreLabel = MakeLabel("CORE: 20 / 20", 16, new Color(0.35f, 1.0f, 0.55f));
		hbox.AddChild(_coreLabel);

		_waveLabel = MakeLabel("WAVE: 0 / 10", 16, new Color(0.82f, 0.65f, 1.0f));
		hbox.AddChild(_waveLabel);

		_statusLabel = MakeLabel("PREP PHASE", 14, new Color(0.65f, 0.78f, 0.92f));
		hbox.AddChild(_statusLabel);

		var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		hbox.AddChild(spacer);

		_startWaveBtn = MakeActionButton("START WAVE 1", new Color(0.08f, 0.75f, 0.95f), 150);
		_startWaveBtn.Pressed += () => _waveManager?.StartNextWave();
		hbox.AddChild(_startWaveBtn);

		_speedBtn = MakeActionButton("SPEED: 1x", new Color(0.65f, 0.42f, 0.95f), 106);
		_speedBtn.Pressed += OnSpeedPressed;
		hbox.AddChild(_speedBtn);

		_pauseBtn = MakeActionButton("PAUSE", new Color(0.95f, 0.60f, 0.15f), 90);
		_pauseBtn.Pressed += OnPausePressed;
		hbox.AddChild(_pauseBtn);

		var padRight = new Control { CustomMinimumSize = new Vector2(8, 0) };
		hbox.AddChild(padRight);
	}

	private void BuildBottomDock(Control root)
	{
		var dock = new PanelContainer
		{
			Position = new Vector2(0, 616),
			Size = new Vector2(1280, 104)
		};
		dock.AddThemeStyleboxOverride("panel", MakePanelStyle(
			new Color(0.06f, 0.075f, 0.12f, 0.95f),
			new Color(0.15f, 0.45f, 0.65f, 0.7f),
			borderTop: 2));
		root.AddChild(dock);

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 14);
		dock.AddChild(hbox);

		hbox.AddChild(new Control { CustomMinimumSize = new Vector2(14, 0) });

		string[] shapeTags = { "[1] TRI", "[2] QUAD", "[3] PENTA", "[4] HEX" };
		for (int i = 0; i < _catalog.Count; i++)
		{
			TowerData data = _catalog[i];
			string tag = i < shapeTags.Length ? shapeTags[i] : $"[{i + 1}]";

			var btn = new Button
			{
				CustomMinimumSize = new Vector2(215, 76),
				Text = $"{tag}  {data.TowerName}\n{data.RoleDescription}  |  {data.Cost} V",
				Alignment = HorizontalAlignment.Left,
				FocusMode = Control.FocusModeEnum.None
			};
			btn.AddThemeFontSizeOverride("font_size", 13);
			StyleBuildButton(btn, data.PrimaryColor, selected: false);

			TowerData captured = data;
			btn.Pressed += () =>
			{
				if (_buildManager.PendingBuildTower == captured)
					_buildManager.CancelBuildSelection();
				else
					_buildManager.SetBuildSelection(captured);
				RefreshBuildButtons();
			};

			hbox.AddChild(btn);
			_buildButtons.Add((btn, data));
		}

		var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		hbox.AddChild(spacer);

		var hints = MakeLabel(
			"LMB: Place / Select   |   RMB / ESC: Cancel\nShift+Click: Multi-Place   |   Link same-shape towers (+10% SPD)",
			12,
			new Color(0.55f, 0.68f, 0.82f));
		hints.VerticalAlignment = VerticalAlignment.Center;
		hbox.AddChild(hints);

		hbox.AddChild(new Control { CustomMinimumSize = new Vector2(14, 0) });
	}

	private void BuildContextPanel(Control root)
	{
		_contextPanel = new PanelContainer
		{
			Position = new Vector2(1000, 355),
			Size = new Vector2(264, 246),
			Visible = false
		};
		_contextPanel.AddThemeStyleboxOverride("panel", MakePanelStyle(
			new Color(0.065f, 0.082f, 0.135f, 0.96f),
			new Color(0.25f, 0.85f, 1.0f, 0.85f),
			borderAll: 2,
			radius: 6));
		root.AddChild(_contextPanel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 14);
		margin.AddThemeConstantOverride("margin_right", 14);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		_contextPanel.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		margin.AddChild(vbox);

		_selTitleLabel = MakeLabel("TRI-BOLT  (TIER 1)", 16, new Color(0.35f, 0.95f, 1.0f));
		vbox.AddChild(_selTitleLabel);

		_selStatsLabel = MakeLabel("DMG: 15   RNG: 185\nRATE: 1.8/s   LINKS: 0", 13, new Color(0.82f, 0.88f, 0.96f));
		vbox.AddChild(_selStatsLabel);

		_upgradeBtn = MakeActionButton("UPGRADE (70 V)", new Color(0.15f, 0.90f, 0.45f), 220);
		_upgradeBtn.Pressed += () => _buildManager?.UpgradeSelectedTower();
		vbox.AddChild(_upgradeBtn);

		_targetBtn = MakeActionButton("TARGET: FIRST", new Color(0.35f, 0.72f, 1.0f), 220);
		_targetBtn.Pressed += () => _buildManager?.CycleSelectedTowerTargeting();
		vbox.AddChild(_targetBtn);

		_sellBtn = MakeActionButton("SELL (+35 V)", new Color(1.0f, 0.30f, 0.35f), 220);
		_sellBtn.Pressed += () => _buildManager?.SellSelectedTower();
		vbox.AddChild(_sellBtn);
	}

	private void BuildGameOverModal(Control root)
	{
		_gameOverOverlay = new ColorRect
		{
			Color = new Color(0.02f, 0.03f, 0.06f, 0.82f),
			Visible = false
		};
		_gameOverOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(_gameOverOverlay);

		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_gameOverOverlay.AddChild(center);

		var panel = new PanelContainer { CustomMinimumSize = new Vector2(440, 250) };
		panel.AddThemeStyleboxOverride("panel", MakePanelStyle(
			new Color(0.07f, 0.09f, 0.15f, 0.98f),
			new Color(0.25f, 0.90f, 1.0f, 0.9f),
			borderAll: 2,
			radius: 8));
		center.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 28);
		margin.AddThemeConstantOverride("margin_right", 28);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		panel.AddChild(margin);

		var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		vbox.AddThemeConstantOverride("separation", 16);
		margin.AddChild(vbox);

		_gameOverTitle = MakeLabel("VICTORY", 28, new Color(0.25f, 1.0f, 0.55f));
		_gameOverTitle.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_gameOverTitle);

		_gameOverStats = MakeLabel("Waves Survived: 10 / 10\nAnomalies Neutralized: 0", 16, new Color(0.85f, 0.90f, 0.98f));
		_gameOverStats.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_gameOverStats);

		var restartBtn = MakeActionButton("PLAY AGAIN", new Color(0.10f, 0.85f, 1.0f), 200);
		restartBtn.CustomMinimumSize = new Vector2(200, 44);
		restartBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		restartBtn.Pressed += () => GameManager.Instance?.ResetGame();
		vbox.AddChild(restartBtn);
	}

	public override void _Process(double delta)
	{
		RefreshBuildButtons();
	}

	private void RefreshAll()
	{
		OnCurrencyChanged(GameManager.Instance.Vertices);
		OnCoreDamaged(GameManager.Instance.CoreIntegrity, 0);
		UpdateWaveUI();
		RefreshBuildButtons();
		RefreshContextPanel();
	}

	private void OnCurrencyChanged(int newAmount)
	{
		_verticesLabel.Text = $"VERTICES: {newAmount}";
		RefreshBuildButtons();
		RefreshContextPanel();
	}

	private void OnCoreDamaged(int remaining, int amount)
	{
		_coreLabel.Text = $"CORE: {remaining} / {GameManager.StartingCoreIntegrity}";
		float ratio = (float)remaining / GameManager.StartingCoreIntegrity;
		_coreLabel.AddThemeColorOverride("font_color",
			ratio > 0.5f ? new Color(0.35f, 1.0f, 0.55f) :
			ratio > 0.25f ? new Color(1.0f, 0.85f, 0.2f) :
			new Color(1.0f, 0.28f, 0.35f));
	}

	private void OnWaveStarted(int waveIndex, int totalWaves)
	{
		UpdateWaveUI();
	}

	private void OnWaveCompleted(int waveIndex, int totalWaves)
	{
		UpdateWaveUI();
	}

	private void UpdateWaveUI()
	{
		if (_waveManager == null) return;
		_waveLabel.Text = $"WAVE: {_waveManager.CurrentWaveIndex} / {_waveManager.TotalWaves}";

		if (_waveManager.IsWaveActive)
		{
			_statusLabel.Text = "COMBAT ACTIVE";
			_statusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.45f, 0.55f));
			_startWaveBtn.Disabled = true;
			_startWaveBtn.Text = $"WAVE {_waveManager.CurrentWaveIndex} ACTIVE";
		}
		else if (_waveManager.CurrentWaveIndex >= _waveManager.TotalWaves)
		{
			_statusLabel.Text = "ALL WAVES CLEARED";
			_statusLabel.AddThemeColorOverride("font_color", new Color(0.35f, 1.0f, 0.55f));
			_startWaveBtn.Disabled = true;
			_startWaveBtn.Text = "COMPLETE";
		}
		else
		{
			_statusLabel.Text = $"PREP  •  NEXT: {_waveManager.NextWaveTitle.ToUpper()}";
			_statusLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.85f, 1.0f));
			_startWaveBtn.Disabled = false;
			_startWaveBtn.Text = $"START WAVE {_waveManager.CurrentWaveIndex + 1}";
		}
	}

	private void OnTowerSelected(Node node)
	{
		_selectedTower = node as BaseTower;
		RefreshContextPanel();
	}

	private void RefreshContextPanel()
	{
		if (!IsInstanceValid(_selectedTower))
		{
			_contextPanel.Visible = false;
			return;
		}

		_contextPanel.Visible = true;
		var d = _selectedTower.Data;
		_selTitleLabel.Text = $"{d.TowerName.ToUpper()}  (TIER {d.Tier}/3)";
		_selTitleLabel.AddThemeColorOverride("font_color", d.PrimaryColor);

		string dmgLabel = d.Type == TowerType.HexPrism ? $"DPS: {d.Damage:0} (x{d.BeamMaxMultiplier:0.#})" : $"DMG: {d.Damage:0}";
		string bonus = _selectedTower.ResonantLinks > 0 ? $" (+{Mathf.Min(_selectedTower.ResonantLinks, 3) * 10}%)" : "";
		_selStatsLabel.Text = $"{dmgLabel}   RNG: {d.Range:0}\nRATE: {d.FireRate * _selectedTower.SynergyMultiplier:0.0}/s{bonus}   LINKS: {_selectedTower.ResonantLinks}";

		if (_selectedTower.CanUpgrade)
		{
			int cost = d.NextUpgrade.Cost;
			_upgradeBtn.Text = $"UPGRADE TIER {d.Tier + 1} ({cost} V)";
			_upgradeBtn.Disabled = !GameManager.Instance.CanAfford(cost);
		}
		else
		{
			_upgradeBtn.Text = "MAX TIER";
			_upgradeBtn.Disabled = true;
		}

		_targetBtn.Text = $"TARGET: {_selectedTower.TargetMode.ToString().ToUpper()}";
		_sellBtn.Text = $"SELL (+{_selectedTower.SellValue} V)";
	}

	private void RefreshBuildButtons()
	{
		foreach (var (btn, data) in _buildButtons)
		{
			bool affordable = GameManager.Instance.CanAfford(data.Cost);
			bool selected = _buildManager?.PendingBuildTower == data;
			btn.Modulate = affordable ? Colors.White : new Color(0.55f, 0.55f, 0.62f, 0.85f);
			StyleBuildButton(btn, data.PrimaryColor, selected);
		}
	}

	private void OnSpeedPressed()
	{
		GameManager.Instance.ToggleFastForward();
		_speedBtn.Text = GameManager.Instance.IsFastForward ? "SPEED: 2x" : "SPEED: 1x";
	}

	private void OnPausePressed()
	{
		GameManager.Instance.TogglePause();
		_pauseBtn.Text = GameManager.Instance.IsPaused ? "RESUME" : "PAUSE";
	}

	private void OnGameOver(bool victory)
	{
		_gameOverOverlay.Visible = true;
		_gameOverTitle.Text = victory ? "VICTORY — CORE SECURED" : "DEFEAT — CORE BREACHED";
		_gameOverTitle.AddThemeColorOverride("font_color",
			victory ? new Color(0.25f, 1.0f, 0.55f) : new Color(1.0f, 0.25f, 0.35f));

		_gameOverStats.Text =
			$"Waves Reached: {GameManager.Instance.CurrentWave} / {GameManager.TotalWaves}\n" +
			$"Anomalies Neutralized: {GameManager.Instance.EnemiesDefeatedCount}\n" +
			$"Core Integrity Remaining: {GameManager.Instance.CoreIntegrity} / {GameManager.StartingCoreIntegrity}";
	}

	private void OnGameReset()
	{
		_gameOverOverlay.Visible = false;
		_speedBtn.Text = "SPEED: 1x";
		_pauseBtn.Text = "PAUSE";
		_selectedTower = null;
		RefreshAll();
	}

	private static Label MakeLabel(string text, int fontSize, Color color)
	{
		var lbl = new Label { Text = text, VerticalAlignment = VerticalAlignment.Center };
		lbl.AddThemeFontSizeOverride("font_size", fontSize);
		lbl.AddThemeColorOverride("font_color", color);
		return lbl;
	}

	private static Control MakeSeparator()
	{
		return new ColorRect
		{
			CustomMinimumSize = new Vector2(2, 28),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			Color = new Color(0.22f, 0.34f, 0.50f, 0.65f)
		};
	}

	private static Button MakeActionButton(string text, Color accent, float minWidth)
	{
		var btn = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(minWidth, 36),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			FocusMode = Control.FocusModeEnum.None
		};
		btn.AddThemeFontSizeOverride("font_size", 13);
		btn.AddThemeColorOverride("font_color", Colors.White);
		btn.AddThemeStyleboxOverride("normal", MakePanelStyle(
			new Color(accent.R * 0.22f, accent.G * 0.22f, accent.B * 0.22f, 0.92f), accent, borderAll: 2, radius: 5));
		btn.AddThemeStyleboxOverride("hover", MakePanelStyle(
			new Color(accent.R * 0.38f, accent.G * 0.38f, accent.B * 0.38f, 0.98f), accent, borderAll: 2, radius: 5));
		btn.AddThemeStyleboxOverride("pressed", MakePanelStyle(
			new Color(accent.R * 0.52f, accent.G * 0.52f, accent.B * 0.52f, 1.0f), Colors.White, borderAll: 2, radius: 5));
		btn.AddThemeStyleboxOverride("disabled", MakePanelStyle(
			new Color(0.10f, 0.11f, 0.15f, 0.75f), new Color(0.28f, 0.30f, 0.36f, 0.55f), borderAll: 1, radius: 5));
		return btn;
	}

	private static void StyleBuildButton(Button btn, Color accent, bool selected)
	{
		Color bg = selected
			? new Color(accent.R * 0.32f, accent.G * 0.32f, accent.B * 0.32f, 0.98f)
			: new Color(0.085f, 0.10f, 0.16f, 0.94f);
		Color border = selected ? Colors.White : accent;
		int bw = selected ? 3 : 2;
		btn.AddThemeStyleboxOverride("normal", MakePanelStyle(bg, border, borderAll: bw, radius: 6));
		btn.AddThemeStyleboxOverride("hover", MakePanelStyle(
			new Color(accent.R * 0.24f, accent.G * 0.24f, accent.B * 0.24f, 0.96f), accent, borderAll: 2, radius: 6));
		btn.AddThemeStyleboxOverride("pressed", MakePanelStyle(
			new Color(accent.R * 0.38f, accent.G * 0.38f, accent.B * 0.38f, 1.0f), Colors.White, borderAll: 3, radius: 6));
	}

	private static StyleBoxFlat MakePanelStyle(
		Color bg, Color border, int borderAll = 0, int borderTop = 0, int borderBottom = 0, int radius = 0)
	{
		return new StyleBoxFlat
		{
			BgColor = bg,
			BorderColor = border,
			BorderWidthLeft = borderAll,
			BorderWidthRight = borderAll,
			BorderWidthTop = borderAll > 0 ? borderAll : borderTop,
			BorderWidthBottom = borderAll > 0 ? borderAll : borderBottom,
			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius,
			CornerRadiusBottomRight = radius,
			ContentMarginLeft = 10,
			ContentMarginRight = 10,
			ContentMarginTop = 6,
			ContentMarginBottom = 6
		};
	}
}
