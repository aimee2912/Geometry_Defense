using Godot;

namespace VectorBastion;

public enum EnemyShape
{
	SwarmCircle,
	StandardDiamond,
	TankSquare,
	SplitterOctagon,
	BossStar
}

[GlobalClass]
public partial class EnemyData : Resource
{
	[Export] public string EnemyName { get; set; } = "Standard";
	[Export] public EnemyShape Shape { get; set; } = EnemyShape.StandardDiamond;
	[Export] public float MaxHealth { get; set; } = 50f;
	[Export] public float MoveSpeed { get; set; } = 100f;
	[Export] public int Reward { get; set; } = 10;
	[Export] public int CoreDamage { get; set; } = 1;
	[Export] public float Radius { get; set; } = 14f;
	[Export] public float SlowResistance { get; set; } = 0f; // 0 = normal, 0.5 = 50% resistant
	[Export] public bool SplitsOnDeath { get; set; } = false;
	[Export] public bool ShieldsAllies { get; set; } = false;
	[Export] public Color PrimaryColor { get; set; } = new Color(1f, 0.22f, 0.39f);
}
