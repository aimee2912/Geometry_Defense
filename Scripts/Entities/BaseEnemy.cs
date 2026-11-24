using System;
using Godot;

namespace VectorBastion;

/// <summary>
/// Geometric enemy moving along a Path2D via PathFollow2D.
/// Supports hit-flash, slow resistance, Splitter mitosis, and Fractal Boss ally shielding.
/// </summary>
public partial class BaseEnemy : PathFollow2D
{
    public EnemyData Data { get; private set; }
    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }
    public float ShieldHealth { get; private set; }
    public float MaxShieldHealth { get; private set; }
    public bool IsDead { get; private set; }
    public float PathProgress => Progress;

    private WaveManager _waveManager;
    private float _slowFactor = 1f; // 1.0 = normal speed, 0.65 = 35% slowed
    private float _slowTimer;
    private float _hitFlashTimer;
    private float _visualTime;
    private float _bossShieldCooldown = 3.5f;
    private int _waveNumber = 1;

    public void Initialize(EnemyData data, int waveNumber, WaveManager waveManager, float startProgress = 0f)
    {
        Data = data;
        _waveNumber = waveNumber;
        _waveManager = waveManager;

        // GDD Formula: HP(w) = HP_base * 1.15^(w - 1)
        float hpScale = Mathf.Pow(1.15f, Mathf.Max(0, waveNumber - 1));
        MaxHealth = Mathf.Round(data.MaxHealth * hpScale);
        CurrentHealth = MaxHealth;

        Loop = false;
        Rotates = false;
        Progress = startProgress;
        ZIndex = 5;

        // Area2D hitbox for Godot physics/range detection compatibility
        var area = new Area2D { Name = "Hitbox", CollisionLayer = 2, CollisionMask = 0 };
        var shape = new CollisionShape2D { Shape = new CircleShape2D { Radius = data.Radius } };
        area.AddChild(shape);
        AddChild(area);
    }

    public override void _Process(double delta)
    {
        if (IsDead || Data == null)
            return;

        float dt = (float)delta;
        _visualTime += dt;

        if (_hitFlashTimer > 0f)
            _hitFlashTimer = Mathf.Max(0f, _hitFlashTimer - dt);

        if (_slowTimer > 0f)
        {
            _slowTimer -= dt;
            if (_slowTimer <= 0f)
                _slowFactor = 1f;
        }

        // Boss ability: periodically shields nearby allies
        if (Data.ShieldsAllies)
        {
            _bossShieldCooldown -= dt;
            if (_bossShieldCooldown <= 0f)
            {
                _bossShieldCooldown = 4.5f;
                CastBossShieldPulse();
            }
        }

        Progress += Data.MoveSpeed * _slowFactor * dt;

        if (ProgressRatio >= 0.998f)
        {
            ReachCore();
            return;
        }

        QueueRedraw();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        _hitFlashTimer = 0.08f;

        if (ShieldHealth > 0f)
        {
            float absorbed = Mathf.Min(ShieldHealth, amount);
            ShieldHealth -= absorbed;
            amount -= absorbed;
        }

        if (amount > 0f)
        {
            CurrentHealth -= amount;
        }

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        if (IsDead || Data == null)
            return;

        float effectiveSlow = slowPercent * (1f - Data.SlowResistance);
        float targetFactor = Mathf.Clamp(1f - effectiveSlow, 0.25f, 1f);
        if (targetFactor <= _slowFactor || _slowTimer <= 0f)
        {
            _slowFactor = targetFactor;
            _slowTimer = Mathf.Max(_slowTimer, duration);
        }
    }

    public void ApplyShield(float amount)
    {
        if (IsDead) return;
        MaxShieldHealth = Mathf.Max(MaxShieldHealth, amount);
        ShieldHealth = Mathf.Min(MaxShieldHealth, ShieldHealth + amount);
    }

    private void CastBossShieldPulse()
    {
        if (_waveManager == null) return;

        var parent = GetTree().CurrentScene;
        parent?.AddChild(BurstEffect.CreateShockwave(GlobalPosition, new Color(0.35f, 0.95f, 1.3f), 160f, 0.45f));

        foreach (var enemy in _waveManager.ActiveEnemies)
        {
            if (!IsInstanceValid(enemy) || enemy.IsDead) continue;
            if (enemy.GlobalPosition.DistanceTo(GlobalPosition) <= 160f)
            {
                enemy.ApplyShield(enemy.MaxHealth * 0.25f);
            }
        }
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        SoundManager.Instance?.PlayEnemyPop();

        var sceneRoot = GetTree().CurrentScene;
        if (sceneRoot != null)
        {
            int shards = Data.Shape == EnemyShape.BossStar ? 22 : 9;
            float ring = Data.Shape == EnemyShape.BossStar ? 70f : Data.Radius * 2.1f;
            sceneRoot.AddChild(BurstEffect.CreateExplosion(GlobalPosition, Data.PrimaryColor, shards, ring, 0.36f));
        }

        if (Data.SplitsOnDeath && _waveManager != null)
        {
            _waveManager.SpawnSplitChildren(Progress, _waveNumber);
        }

        _waveManager?.NotifyEnemyRemoved(this);
        SignalBus.Instance?.EmitSignal(SignalBus.SignalName.EnemyDefeated, Data.Reward);
        QueueFree();
    }

    private void ReachCore()
    {
        if (IsDead) return;
        IsDead = true;

        _waveManager?.NotifyEnemyRemoved(this);
        GameManager.Instance?.DamageCore(Data.CoreDamage);
        QueueFree();
    }

    public override void _Draw()
    {
        if (Data == null) return;

        Color c = Data.PrimaryColor;
        if (_slowTimer > 0f)
            c = c.Lerp(new Color(0.65f, 0.35f, 1.25f), 0.45f);
        if (_hitFlashTimer > 0f)
            c = new Color(1.8f, 1.8f, 1.8f);

        Color fill = new Color(c.R * 0.28f, c.G * 0.28f, c.B * 0.28f, 0.92f);
        Color glowOutline = new Color(c.R * 1.35f, c.G * 1.35f, c.B * 1.35f, 1f);
        float r = Data.Radius;

        switch (Data.Shape)
        {
            case EnemyShape.SwarmCircle:
                DrawCircle(Vector2.Zero, r, fill);
                DrawArc(Vector2.Zero, r, 0f, Mathf.Tau, 20, glowOutline, 2.2f, true);
                DrawCircle(Vector2.Zero, r * 0.35f, glowOutline);
                break;

            case EnemyShape.StandardDiamond:
                DrawRegPoly(4, r, _visualTime * 1.5f, fill, glowOutline, 2.4f);
                break;

            case EnemyShape.TankSquare:
                DrawRegPoly(4, r, Mathf.Pi * 0.25f, fill, glowOutline, 3.0f);
                DrawRegPoly(4, r * 0.55f, Mathf.Pi * 0.25f + _visualTime * 0.9f, new Color(0, 0, 0, 0), glowOutline, 1.8f);
                break;

            case EnemyShape.SplitterOctagon:
                DrawRegPoly(8, r, _visualTime * 0.8f, fill, glowOutline, 2.4f);
                DrawLine(new Vector2(-r * 0.55f, 0), new Vector2(r * 0.55f, 0), glowOutline, 1.5f);
                DrawLine(new Vector2(0, -r * 0.55f), new Vector2(0, r * 0.55f), glowOutline, 1.5f);
                break;

            case EnemyShape.BossStar:
                DrawStar(8, r, r * 0.52f, _visualTime * 1.1f, fill, glowOutline, 2.8f);
                DrawRegPoly(4, r * 0.42f, -_visualTime * 2.0f, new Color(0, 0, 0, 0), new Color(1.4f, 1.4f, 1.5f), 2f);
                break;
        }

        // Shield aura
        if (ShieldHealth > 0f)
        {
            DrawArc(Vector2.Zero, r + 5f, 0f, Mathf.Tau, 24, new Color(0.35f, 1.15f, 1.45f, 0.9f), 2.0f, true);
        }

        // Health bar (shown when damaged or boss)
        if (CurrentHealth < MaxHealth || ShieldHealth > 0f || Data.Shape == EnemyShape.BossStar)
        {
            float barW = r * 2.4f;
            float barH = 4f;
            Vector2 barPos = new(-barW * 0.5f, -r - 11f);
            DrawRect(new Rect2(barPos, new Vector2(barW, barH)), new Color(0.08f, 0.08f, 0.12f, 0.9f), true);

            float hpRatio = Mathf.Clamp(CurrentHealth / MaxHealth, 0f, 1f);
            Color hpColor = hpRatio > 0.5f ? new Color(0.25f, 1.1f, 0.45f) :
                            hpRatio > 0.25f ? new Color(1.1f, 0.85f, 0.15f) :
                            new Color(1.2f, 0.2f, 0.25f);
            DrawRect(new Rect2(barPos, new Vector2(barW * hpRatio, barH)), hpColor, true);

            if (ShieldHealth > 0f && MaxShieldHealth > 0f)
            {
                float sRatio = Mathf.Clamp(ShieldHealth / MaxShieldHealth, 0f, 1f);
                DrawRect(new Rect2(barPos + new Vector2(0, -3f), new Vector2(barW * sRatio, 2.5f)), new Color(0.3f, 1.1f, 1.4f), true);
            }
        }
    }

    private void DrawRegPoly(int sides, float radius, float rot, Color fill, Color outline, float width)
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
        if (fill.A > 0.01f)
            DrawColoredPolygon(poly, fill);
        DrawPolyline(loop, outline, width, true);
    }

    private void DrawStar(int points, float outerR, float innerR, float rot, Color fill, Color outline, float width)
    {
        int total = points * 2;
        var poly = new Vector2[total];
        var loop = new Vector2[total + 1];
        for (int i = 0; i < total; i++)
        {
            float a = rot + i * Mathf.Tau / total;
            float r = (i % 2 == 0) ? outerR : innerR;
            poly[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            loop[i] = poly[i];
        }
        loop[total] = poly[0];
        DrawColoredPolygon(poly, fill);
        DrawPolyline(loop, outline, width, true);
    }
}
