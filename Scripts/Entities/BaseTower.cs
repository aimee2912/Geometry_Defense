using System.Collections.Generic;
using Godot;

namespace VectorBastion;

/// <summary>
/// Polygon tower supporting Tri-Bolt, Quad-Cannon, Penta-Pulse, and Hex-Prism behaviors,
/// Tier 1-3 upgrades, targeting priorities, and resonant link synergy.
/// </summary>
public partial class BaseTower : Node2D
{
    public TowerData Data { get; private set; }
    public Vector2I GridCell { get; private set; }
    public int TotalInvested { get; private set; }
    public TargetingMode TargetMode { get; set; } = TargetingMode.First;
    public bool IsSelected { get; set; }
    public int ResonantLinks { get; set; }

    public float SynergyMultiplier => 1f + Mathf.Min(ResonantLinks, 3) * 0.10f;
    public int SellValue => Mathf.RoundToInt(TotalInvested * GameManager.SellRefundRatio);
    public bool CanUpgrade => Data?.NextUpgrade != null;

    private WaveManager _waveManager;
    private Area2D _rangeArea;
    private CollisionShape2D _rangeShape;
    private float _cooldown;
    private float _aimAngle = -Mathf.Pi * 0.5f;
    private float _visualTime;
    private float _recoil;

    // Hex-Prism beam state
    private BaseEnemy _beamTarget;
    private float _beamRampTimer;

    public void Initialize(TowerData data, Vector2I gridCell, WaveManager waveManager)
    {
        Data = data;
        GridCell = gridCell;
        TotalInvested = data.Cost;
        _waveManager = waveManager;
        ZIndex = 4;

        _rangeArea = new Area2D { Name = "RangeArea", CollisionLayer = 0, CollisionMask = 2 };
        _rangeShape = new CollisionShape2D { Shape = new CircleShape2D { Radius = data.Range } };
        _rangeArea.AddChild(_rangeShape);
        AddChild(_rangeArea);
    }

    public bool TryUpgrade()
    {
        if (!CanUpgrade)
            return false;

        int cost = Data.NextUpgrade.Cost;
        if (!GameManager.Instance.SpendVertices(cost))
            return false;

        TotalInvested += cost;
        Data = Data.NextUpgrade;

        if (_rangeShape?.Shape is CircleShape2D circle)
            circle.Radius = Data.Range;

        SoundManager.Instance?.PlayUpgrade();
        GetTree().CurrentScene?.AddChild(
            BurstEffect.CreateShockwave(GlobalPosition, Data.PrimaryColor, 42f, 0.30f));

        QueueRedraw();
        return true;
    }

    public void CycleTargetingMode()
    {
        TargetMode = TargetMode switch
        {
            TargetingMode.First => TargetingMode.Closest,
            TargetingMode.Closest => TargetingMode.Strongest,
            _ => TargetingMode.First
        };
    }

    public override void _Process(double delta)
    {
        if (Data == null || GameManager.Instance.IsGameOver)
            return;

        float dt = (float)delta;
        _visualTime += dt;
        if (_recoil > 0f)
            _recoil = Mathf.Max(0f, _recoil - dt * 5f);

        if (Data.Type == TowerType.HexPrism)
        {
            ProcessBeam(dt);
        }
        else
        {
            _beamTarget = null;
            _beamRampTimer = 0f;

            _cooldown -= dt;
            var target = AcquireTarget();
            if (target != null && Data.Type != TowerType.PentaPulse)
            {
                float desired = (target.GlobalPosition - GlobalPosition).Angle();
                _aimAngle = Mathf.LerpAngle(_aimAngle, desired, Mathf.Clamp(dt * 16f, 0f, 1f));
            }

            if (_cooldown <= 0f && target != null)
            {
                FireAt(target);
                _cooldown = 1f / (Data.FireRate * SynergyMultiplier);
            }
        }

        QueueRedraw();
    }

    private void ProcessBeam(float dt)
    {
        // Keep locked on current target if still valid and in range, otherwise acquire new
        if (!IsInstanceValid(_beamTarget) || _beamTarget.IsDead ||
            _beamTarget.GlobalPosition.DistanceTo(GlobalPosition) > Data.Range)
        {
            _beamTarget = AcquireTarget();
            _beamRampTimer = 0f;
        }

        if (_beamTarget != null)
        {
            float desired = (_beamTarget.GlobalPosition - GlobalPosition).Angle();
            _aimAngle = Mathf.LerpAngle(_aimAngle, desired, Mathf.Clamp(dt * 18f, 0f, 1f));

            _beamRampTimer += dt;
            float rampMult = Mathf.Min(Data.BeamMaxMultiplier, 1f + _beamRampTimer * Data.BeamRampPerSecond);
            float dps = Data.Damage * rampMult * SynergyMultiplier;
            _beamTarget.TakeDamage(dps * dt);
        }
    }

    private void FireAt(BaseEnemy target)
    {
        _recoil = 1f;

        switch (Data.Type)
        {
            case TowerType.TriBolt:
                SoundManager.Instance?.PlayShootBolt();
                SpawnProjectile(target);
                break;

            case TowerType.QuadCannon:
                SoundManager.Instance?.PlayShootCannon();
                SpawnProjectile(target);
                break;

            case TowerType.PentaPulse:
                SoundManager.Instance?.PlayPulse();
                EmitSlowPulse();
                break;
        }
    }

    private void SpawnProjectile(BaseEnemy target)
    {
        var proj = new BaseProjectile();
        Vector2 muzzle = GlobalPosition + new Vector2(Mathf.Cos(_aimAngle), Mathf.Sin(_aimAngle)) * 14f;
        proj.Initialize(
            muzzle,
            target,
            Data.Damage,
            Data.ProjectileSpeed,
            Data.PrimaryColor,
            Data.PierceCount,
            Data.SplashRadius,
            _waveManager
        );
        GetTree().CurrentScene?.AddChild(proj);
    }

    private void EmitSlowPulse()
    {
        GetTree().CurrentScene?.AddChild(
            BurstEffect.CreateShockwave(GlobalPosition, Data.PrimaryColor, Data.Range, 0.36f));

        if (_waveManager == null) return;

        var snapshot = new List<BaseEnemy>(_waveManager.ActiveEnemies);
        foreach (var enemy in snapshot)
        {
            if (!IsInstanceValid(enemy) || enemy.IsDead) continue;
            if (enemy.GlobalPosition.DistanceTo(GlobalPosition) <= Data.Range + enemy.Data.Radius)
            {
                enemy.ApplySlow(Data.SlowPercent, Data.SlowDuration);
                enemy.TakeDamage(Data.Damage);
            }
        }
    }

    private BaseEnemy AcquireTarget()
    {
        if (_waveManager == null || _waveManager.ActiveEnemies.Count == 0)
            return null;

        BaseEnemy best = null;
        float bestMetric = float.MinValue;

        foreach (var enemy in _waveManager.ActiveEnemies)
        {
            if (!IsInstanceValid(enemy) || enemy.IsDead)
                continue;

            float dist = enemy.GlobalPosition.DistanceTo(GlobalPosition);
            if (dist > Data.Range)
                continue;

            float metric = TargetMode switch
            {
                TargetingMode.First => enemy.PathProgress,
                TargetingMode.Closest => -dist,
                TargetingMode.Strongest => enemy.CurrentHealth + enemy.ShieldHealth,
                _ => enemy.PathProgress
            };

            if (metric > bestMetric)
            {
                bestMetric = metric;
                best = enemy;
            }
        }

        return best;
    }

    public override void _Draw()
    {
        if (Data == null) return;

        Color c = Data.PrimaryColor;
        Color glow = new(c.R * 1.35f, c.G * 1.35f, c.B * 1.35f, 1f);
        Color fill = new(c.R * 0.22f, c.G * 0.22f, c.B * 0.22f, 0.92f);

        // Selection range ring
        if (IsSelected)
        {
            DrawCircle(Vector2.Zero, Data.Range, new Color(c.R, c.G, c.B, 0.08f));
            DrawArc(Vector2.Zero, Data.Range, 0f, Mathf.Tau, 64, new Color(c.R * 1.2f, c.G * 1.2f, c.B * 1.2f, 0.72f), 1.8f, true);
        }

        // Hex-Prism continuous laser beam
        if (Data.Type == TowerType.HexPrism && IsInstanceValid(_beamTarget) && !_beamTarget.IsDead)
        {
            Vector2 targetLocal = ToLocal(_beamTarget.GlobalPosition);
            float ramp = Mathf.Clamp(_beamRampTimer * Data.BeamRampPerSecond / (Data.BeamMaxMultiplier - 1f), 0f, 1f);
            float beamWidth = Mathf.Lerp(2.2f, 6.5f, ramp);
            Color outerBeam = new(c.R * 1.3f, c.G * 1.3f, c.B * 1.3f, 0.45f + ramp * 0.35f);
            DrawLine(Vector2.Zero, targetLocal, outerBeam, beamWidth * 2.1f, true);
            DrawLine(Vector2.Zero, targetLocal, new Color(1.4f, 1.6f, 1.4f, 0.95f), beamWidth * 0.7f, true);
            DrawCircle(targetLocal, 4f + ramp * 5f, glow);
        }

        // Subtle base socket
        DrawCircle(Vector2.Zero, 19f, new Color(0.08f, 0.09f, 0.14f, 0.9f));

        // Tier 3 rotating outer ring
        if (Data.Tier >= 3)
        {
            DrawPolygonOutline(Data.Sides, 20.5f, -_visualTime * 1.2f, new Color(glow.R, glow.G, glow.B, 0.65f), 1.5f);
        }

        // Main polygon body
        float rot = Data.Type == TowerType.PentaPulse ? _visualTime * 0.6f : _aimAngle;
        float radius = 16.5f - _recoil * 2.0f;
        DrawFilledPolygon(Data.Sides, radius, rot, fill, glow, 2.4f);

        // Tier 2+ concentric inner polygon
        if (Data.Tier >= 2)
        {
            DrawPolygonOutline(Data.Sides, radius * 0.56f, rot, new Color(1.35f, 1.35f, 1.45f, 0.9f), 1.6f);
        }

        // Center energy core
        DrawCircle(Vector2.Zero, 3.2f + Data.Tier * 0.6f, glow);

        // Barrel/direction indicator for directional towers
        if (Data.Type == TowerType.TriBolt || Data.Type == TowerType.QuadCannon)
        {
            Vector2 tip = new Vector2(Mathf.Cos(_aimAngle), Mathf.Sin(_aimAngle)) * (radius + 4f - _recoil * 3f);
            DrawLine(Vector2.Zero, tip, new Color(1.4f, 1.4f, 1.5f, 0.9f), 2.0f, true);
        }

        // Tier pip dots at bottom
        for (int i = 0; i < Data.Tier; i++)
        {
            float px = (i - (Data.Tier - 1) * 0.5f) * 6.5f;
            DrawCircle(new Vector2(px, 21f), 2.0f, glow);
        }
    }

    private void DrawFilledPolygon(int sides, float radius, float rot, Color fill, Color outline, float width)
    {
        var poly = new Vector2[sides];
        var loop = new Vector2[sides + 1];
        for (int i = 0; i < sides; i++)
        {
            float a = rot + i * Mathf.Tau / sides;
            poly[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            loop[i] = poly[i];
        }
        loop[sides] = poly[0];
        DrawColoredPolygon(poly, fill);
        DrawPolyline(loop, outline, width, true);
    }

    private void DrawPolygonOutline(int sides, float radius, float rot, Color color, float width)
    {
        var loop = new Vector2[sides + 1];
        for (int i = 0; i < sides; i++)
        {
            float a = rot + i * Mathf.Tau / sides;
            loop[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }
        loop[sides] = loop[0];
        DrawPolyline(loop, color, width, true);
    }
}
