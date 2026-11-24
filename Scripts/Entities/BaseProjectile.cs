using System.Collections.Generic;
using Godot;

namespace VectorBastion;

/// <summary>
/// Procedural neon projectile supporting both piercing Tri-Bolt bolts and AoE Quad-Cannon shells.
/// </summary>
public partial class BaseProjectile : Node2D
{
    private Vector2 _velocity;
    private float _damage;
    private float _speed;
    private float _splashRadius;
    private int _pierceRemaining;
    private Color _color;
    private bool _isSplash;
    private BaseEnemy _homingTarget;
    private Vector2 _lastTargetPos;
    private WaveManager _waveManager;
    private float _lifetime = 2.5f;
    private readonly HashSet<ulong> _hitEnemyIds = new();

    public void Initialize(
        Vector2 startPos,
        BaseEnemy target,
        float damage,
        float speed,
        Color color,
        int pierceCount,
        float splashRadius,
        WaveManager waveManager)
    {
        GlobalPosition = startPos;
        _damage = damage;
        _speed = speed;
        _color = color;
        _pierceRemaining = pierceCount;
        _splashRadius = splashRadius;
        _isSplash = splashRadius > 0f;
        _homingTarget = target;
        _lastTargetPos = IsInstanceValid(target) ? target.GlobalPosition : startPos + Vector2.Right * 100f;
        _waveManager = waveManager;

        Vector2 dir = (_lastTargetPos - startPos).Normalized();
        if (dir.LengthSquared() < 0.001f) dir = Vector2.Right;
        _velocity = dir * _speed;
        Rotation = dir.Angle();
        ZIndex = 10;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _lifetime -= dt;
        if (_lifetime <= 0f)
        {
            QueueFree();
            return;
        }

        if (_isSplash)
        {
            UpdateSplash(dt);
        }
        else
        {
            UpdatePiercing(dt);
        }

        QueueRedraw();
    }

    private void UpdateSplash(float dt)
    {
        if (IsInstanceValid(_homingTarget) && !_homingTarget.IsDead)
            _lastTargetPos = _homingTarget.GlobalPosition;

        Vector2 toTarget = _lastTargetPos - GlobalPosition;
        float dist = toTarget.Length();
        float step = _speed * dt;

        if (dist <= step + 10f)
        {
            GlobalPosition = _lastTargetPos;
            DetonateSplash();
            return;
        }

        Vector2 dir = toTarget / dist;
        _velocity = dir * _speed;
        GlobalPosition += _velocity * dt;
        Rotation = dir.Angle();
    }

    private void DetonateSplash()
    {
        SoundManager.Instance?.PlayExplosion();
        GetTree().CurrentScene?.AddChild(
            BurstEffect.CreateExplosion(GlobalPosition, _color, 12, _splashRadius, 0.34f));

        if (_waveManager != null)
        {
            // Snapshot list since TakeDamage can trigger enemy death/split
            var snapshot = new List<BaseEnemy>(_waveManager.ActiveEnemies);
            foreach (var enemy in snapshot)
            {
                if (!IsInstanceValid(enemy) || enemy.IsDead) continue;
                float d = enemy.GlobalPosition.DistanceTo(GlobalPosition);
                if (d <= _splashRadius + enemy.Data.Radius)
                {
                    float falloff = Mathf.Lerp(1.0f, 0.55f, Mathf.Clamp(d / _splashRadius, 0f, 1f));
                    enemy.TakeDamage(_damage * falloff);
                }
            }
        }

        QueueFree();
    }

    private void UpdatePiercing(float dt)
    {
        GlobalPosition += _velocity * dt;

        if (_waveManager != null)
        {
            var snapshot = new List<BaseEnemy>(_waveManager.ActiveEnemies);
            foreach (var enemy in snapshot)
            {
                if (!IsInstanceValid(enemy) || enemy.IsDead) continue;
                ulong id = enemy.GetInstanceId();
                if (_hitEnemyIds.Contains(id)) continue;

                float hitRadius = enemy.Data.Radius + 7f;
                if (enemy.GlobalPosition.DistanceTo(GlobalPosition) <= hitRadius)
                {
                    _hitEnemyIds.Add(id);
                    enemy.TakeDamage(_damage);

                    if (_pierceRemaining <= 0)
                    {
                        QueueFree();
                        return;
                    }
                    _pierceRemaining--;
                }
            }
        }
    }

    public override void _Draw()
    {
        Color glow = new(_color.R * 1.45f, _color.G * 1.45f, _color.B * 1.45f, 1f);
        if (_isSplash)
        {
            // Heavy square shell
            DrawRect(new Rect2(-5f, -5f, 10f, 10f), glow, true);
            DrawRect(new Rect2(-6f, -6f, 12f, 12f), new Color(1.4f, 1.4f, 1.4f, 0.9f), false, 1.5f);
        }
        else
        {
            // Sleek neon dart with trail
            DrawLine(new Vector2(-14f, 0f), new Vector2(6f, 0f), new Color(_color.R, _color.G, _color.B, 0.45f), 4.5f, true);
            DrawLine(new Vector2(-10f, 0f), new Vector2(8f, 0f), glow, 2.2f, true);
            DrawCircle(new Vector2(6f, 0f), 2.5f, new Color(1.5f, 1.5f, 1.5f));
        }
    }
}
