using System.Collections.Generic;
using Godot;

namespace VectorBastion;

/// <summary>
/// Root scene controller for Vector Bastion.
/// Sets up WorldEnvironment (Canvas Glow), Camera2D (screen shake), GridManager,
/// CoreEntity, WaveManager, BuildManager, and HudController.
/// </summary>
public partial class Main : Node2D
{
    private Camera2D _camera;
    private GridManager _gridManager;
    private CoreEntity _coreEntity;
    private WaveManager _waveManager;
    private BuildManager _buildManager;
    private HudController _hud;

    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeElapsed;
    private readonly Vector2 _baseCameraPos = new(640f, 360f);

    public override void _Ready()
    {
        SetupEnvironmentAndCamera();

        List<TowerData> catalog = GameCatalog.CreateTowerCatalog();

        _gridManager = new GridManager { Name = "GridManager" };
        AddChild(_gridManager);

        _coreEntity = new CoreEntity
        {
            Name = "CoreEntity",
            Position = _gridManager.CorePosition,
            ZIndex = 6
        };
        AddChild(_coreEntity);

        _waveManager = new WaveManager { Name = "WaveManager" };
        AddChild(_waveManager);
        _waveManager.Initialize(_gridManager);

        _buildManager = new BuildManager { Name = "BuildManager" };
        AddChild(_buildManager);
        _buildManager.Initialize(_gridManager, _waveManager, catalog);

        _hud = new HudController { Name = "HUD" };
        AddChild(_hud);
        _hud.Initialize(_buildManager, _waveManager, catalog);

        if (SignalBus.Instance != null)
        {
            SignalBus.Instance.ScreenShakeRequested += OnScreenShakeRequested;
            SignalBus.Instance.GameReset += OnGameReset;
        }
    }

    public override void _ExitTree()
    {
        if (SignalBus.Instance != null)
        {
            SignalBus.Instance.ScreenShakeRequested -= OnScreenShakeRequested;
            SignalBus.Instance.GameReset -= OnGameReset;
        }
    }

    private void SetupEnvironmentAndCamera()
    {
        // WorldEnvironment with Canvas background mode + Glow for crisp neon bloom
        var env = new Environment
        {
            BackgroundMode = Environment.BGMode.Canvas,
            GlowEnabled = true,
            GlowIntensity = 0.85f,
            GlowStrength = 1.05f,
            GlowBloom = 0.22f,
            GlowBlendMode = Environment.GlowBlendModeEnum.Additive,
            GlowHdrThreshold = 0.85f
        };

        var worldEnv = new WorldEnvironment
        {
            Name = "WorldEnvironment",
            Environment = env
        };
        AddChild(worldEnv);

        _camera = new Camera2D
        {
            Name = "MainCamera",
            Position = _baseCameraPos,
            Enabled = true
        };
        AddChild(_camera);
    }

    private void OnScreenShakeRequested(float intensity, float duration)
    {
        _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        _shakeDuration = Mathf.Max(_shakeDuration, duration);
        _shakeElapsed = 0f;
    }

    private void OnGameReset()
    {
        _gridManager?.ClearAllTowers();
        _waveManager?.ResetWaves();
        _buildManager?.ResetState();

        // Remove any lingering projectiles or effects
        foreach (Node child in GetChildren())
        {
            if (child is BaseProjectile || child is BurstEffect)
                child.QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        if (_shakeDuration > 0f && _camera != null)
        {
            _shakeElapsed += (float)delta;
            if (_shakeElapsed >= _shakeDuration)
            {
                _shakeDuration = 0f;
                _shakeIntensity = 0f;
                _camera.Offset = Vector2.Zero;
            }
            else
            {
                float decay = 1f - (_shakeElapsed / _shakeDuration);
                float mag = _shakeIntensity * decay;
                _camera.Offset = new Vector2(
                    (float)GD.RandRange(-mag, mag),
                    (float)GD.RandRange(-mag, mag));
            }
        }
    }
}
