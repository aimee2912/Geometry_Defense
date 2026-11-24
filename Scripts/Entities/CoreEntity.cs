using Godot;

namespace VectorBastion;

/// <summary>
/// The geometric Core at the end of the path that the player defends.
/// </summary>
public partial class CoreEntity : Node2D
{
    private float _time;
    private float _hitFlashTimer;
    private float _healthRatio = 1f;

    public override void _Ready()
    {
        if (SignalBus.Instance != null)
        {
            SignalBus.Instance.CoreDamaged += OnCoreDamaged;
            SignalBus.Instance.GameReset += OnGameReset;
        }
    }

    public override void _ExitTree()
    {
        if (SignalBus.Instance != null)
        {
            SignalBus.Instance.CoreDamaged -= OnCoreDamaged;
            SignalBus.Instance.GameReset -= OnGameReset;
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        if (_hitFlashTimer > 0f)
            _hitFlashTimer = Mathf.Max(0f, _hitFlashTimer - (float)delta);

        QueueRedraw();
    }

    private void OnCoreDamaged(int remaining, int amount)
    {
        _healthRatio = Mathf.Clamp((float)remaining / GameManager.StartingCoreIntegrity, 0f, 1f);
        if (amount > 0)
            _hitFlashTimer = 0.25f;
    }

    private void OnGameReset()
    {
        _healthRatio = 1f;
        _hitFlashTimer = 0f;
    }

    public override void _Draw()
    {
        Color baseColor = new Color(0.15f, 1.15f, 1.35f).Lerp(new Color(1.35f, 0.2f, 0.28f), 1f - _healthRatio);
        if (_hitFlashTimer > 0f)
            baseColor = new Color(1.6f, 1.6f, 1.6f);

        float pulse = 1f + 0.08f * Mathf.Sin(_time * 4f);

        // Outer halo
        DrawCircle(Vector2.Zero, 26f * pulse, new Color(baseColor.R, baseColor.G, baseColor.B, 0.14f));

        // Rotating outer octagon
        DrawPolygonOutline(8, 21f * pulse, _time * 0.7f, baseColor, 2.5f);

        // Counter-rotating inner square
        DrawPolygonOutline(4, 13f, -_time * 1.3f, new Color(1.3f, 1.3f, 1.4f, 0.95f), 2.0f);

        // Center spark
        DrawCircle(Vector2.Zero, 5f * pulse, baseColor);
    }

    private void DrawPolygonOutline(int sides, float radius, float rotation, Color color, float width)
    {
        var pts = new Vector2[sides + 1];
        for (int i = 0; i < sides; i++)
        {
            float angle = rotation + i * Mathf.Tau / sides;
            pts[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        pts[sides] = pts[0];
        DrawPolyline(pts, color, width, true);
    }
}
