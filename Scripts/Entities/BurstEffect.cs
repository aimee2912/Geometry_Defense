using System.Collections.Generic;
using Godot;

namespace VectorBastion;

/// <summary>
/// Procedural geometric particle burst and expanding shockwave ring.
/// Used for enemy destruction, splash impacts, and Penta-Pulse shockwaves.
/// </summary>
public partial class BurstEffect : Node2D
{
    private struct Shard
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public float Spin;
        public float Size;
        public int Sides;
    }

    private readonly List<Shard> _shards = new();
    private Color _color;
    private float _duration;
    private float _elapsed;
    private float _ringMaxRadius;
    private bool _hasRing;

    public static BurstEffect CreateExplosion(Vector2 pos, Color color, int shardCount = 10, float ringRadius = 28f, float duration = 0.35f)
    {
        var fx = new BurstEffect
        {
            GlobalPosition = pos,
            _color = color,
            _duration = duration,
            _ringMaxRadius = ringRadius,
            _hasRing = ringRadius > 0f,
            ZIndex = 15
        };

        for (int i = 0; i < shardCount; i++)
        {
            float angle = (float)GD.RandRange(0.0, Mathf.Tau);
            float speed = (float)GD.RandRange(65.0, 190.0);
            fx._shards.Add(new Shard
            {
                Position = Vector2.Zero,
                Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                Rotation = (float)GD.RandRange(0.0, Mathf.Tau),
                Spin = (float)GD.RandRange(-8.0, 8.0),
                Size = (float)GD.RandRange(3.5, 7.5),
                Sides = (int)GD.RandRange(3, 5)
            });
        }
        return fx;
    }

    public static BurstEffect CreateShockwave(Vector2 pos, Color color, float maxRadius, float duration = 0.32f)
    {
        return new BurstEffect
        {
            GlobalPosition = pos,
            _color = color,
            _duration = duration,
            _ringMaxRadius = maxRadius,
            _hasRing = true,
            ZIndex = 12
        };
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _elapsed += dt;
        if (_elapsed >= _duration)
        {
            QueueFree();
            return;
        }

        for (int i = 0; i < _shards.Count; i++)
        {
            var s = _shards[i];
            s.Position += s.Velocity * dt;
            s.Velocity *= Mathf.Pow(0.08f, dt);
            s.Rotation += s.Spin * dt;
            _shards[i] = s;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        float t = Mathf.Clamp(_elapsed / _duration, 0f, 1f);
        float alpha = 1f - t;

        if (_hasRing)
        {
            float r = Mathf.Lerp(6f, _ringMaxRadius, 1f - (1f - t) * (1f - t));
            var ringColor = new Color(_color.R * 1.25f, _color.G * 1.25f, _color.B * 1.25f, alpha * 0.85f);
            DrawArc(Vector2.Zero, r, 0f, Mathf.Tau, 36, ringColor, Mathf.Lerp(3.5f, 1.0f, t), true);
            DrawCircle(Vector2.Zero, r, new Color(_color.R, _color.G, _color.B, alpha * 0.12f));
        }

        var shardColor = new Color(_color.R * 1.3f, _color.G * 1.3f, _color.B * 1.3f, alpha);
        foreach (var s in _shards)
        {
            var pts = new Vector2[s.Sides + 1];
            float sz = s.Size * (1f - t * 0.45f);
            for (int i = 0; i < s.Sides; i++)
            {
                float a = s.Rotation + i * Mathf.Tau / s.Sides;
                pts[i] = s.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sz;
            }
            pts[s.Sides] = pts[0];
            DrawPolyline(pts, shardColor, 1.6f, true);
        }
    }
}
