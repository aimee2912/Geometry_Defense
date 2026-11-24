using Godot;

namespace VectorBastion;

public enum TowerType
{
	TriBolt,     // Triangle: Fast single-target piercing projectile
	QuadCannon,  // Square: Slow heavy projectile with radial splash
	PentaPulse,  // Pentagon: Radial shockwave slowing enemies
	HexPrism     // Hexagon: Continuous beam ramping damage over time
}

public enum TargetingMode
{
	First,
	Closest,
	Strongest
}

[GlobalClass]
public partial class TowerData : Resource
{
	[Export] public string TowerName { get; set; } = "Tri-Bolt";
	[Export] public string RoleDescription { get; set; } = "Rapid Piercing";
	[Export] public TowerType Type { get; set; } = TowerType.TriBolt;
	[Export] public int Sides { get; set; } = 3;
	[Export] public int Tier { get; set; } = 1;
	[Export] public int Cost { get; set; } = 50;
	[Export] public float Range { get; set; } = 180f;
	[Export] public float FireRate { get; set; } = 1.8f; // Shots per second
	[Export] public float Damage { get; set; } = 18f;
	[Export] public float ProjectileSpeed { get; set; } = 540f;
	[Export] public float SplashRadius { get; set; } = 0f;
	[Export] public float SlowPercent { get; set; } = 0f;
	[Export] public float SlowDuration { get; set; } = 0f;
	[Export] public int PierceCount { get; set; } = 0;
	[Export] public float BeamRampPerSecond { get; set; } = 0f;
	[Export] public float BeamMaxMultiplier { get; set; } = 3.0f;
	[Export] public Color PrimaryColor { get; set; } = new Color(0f, 0.96f, 1f);
	[Export] public TowerData NextUpgrade { get; set; }
}
