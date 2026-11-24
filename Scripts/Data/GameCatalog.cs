using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace VectorBastion;

/// <summary>
/// Central catalog constructing all GDD-defined Towers (Tier 1-3), Enemies, and Waves.
/// </summary>
public static class GameCatalog
{
	public static List<TowerData> CreateTowerCatalog()
	{
		// 1. Tri-Bolt (Triangle, 3 sides) - Rapid / Sniper piercing
		var triT3 = new TowerData
		{
			TowerName = "Tri-Bolt",
			RoleDescription = "Rapid / Piercing",
			Type = TowerType.TriBolt,
			Sides = 3,
			Tier = 3,
			Cost = 110,
			Range = 250f,
			FireRate = 2.8f,
			Damage = 36f,
			ProjectileSpeed = 720f,
			PierceCount = 2,
			PrimaryColor = new Color(0.0f, 0.95f, 1.0f)
		};
		var triT2 = new TowerData
		{
			TowerName = "Tri-Bolt",
			RoleDescription = "Rapid / Piercing",
			Type = TowerType.TriBolt,
			Sides = 3,
			Tier = 2,
			Cost = 70,
			Range = 215f,
			FireRate = 2.3f,
			Damage = 24f,
			ProjectileSpeed = 640f,
			PierceCount = 1,
			PrimaryColor = new Color(0.0f, 0.95f, 1.0f),
			NextUpgrade = triT3
		};
		var triT1 = new TowerData
		{
			TowerName = "Tri-Bolt",
			RoleDescription = "Rapid / Piercing",
			Type = TowerType.TriBolt,
			Sides = 3,
			Tier = 1,
			Cost = 50,
			Range = 185f,
			FireRate = 1.8f,
			Damage = 15f,
			ProjectileSpeed = 560f,
			PierceCount = 1,
			PrimaryColor = new Color(0.0f, 0.95f, 1.0f),
			NextUpgrade = triT2
		};

		// 2. Quad-Cannon (Square, 4 sides) - Heavy / AoE Splash
		var quadT3 = new TowerData
		{
			TowerName = "Quad-Cannon",
			RoleDescription = "Heavy / AoE Splash",
			Type = TowerType.QuadCannon,
			Sides = 4,
			Tier = 3,
			Cost = 150,
			Range = 215f,
			FireRate = 0.95f,
			Damage = 88f,
			ProjectileSpeed = 360f,
			SplashRadius = 92f,
			PrimaryColor = new Color(1.0f, 0.55f, 0.05f)
		};
		var quadT2 = new TowerData
		{
			TowerName = "Quad-Cannon",
			RoleDescription = "Heavy / AoE Splash",
			Type = TowerType.QuadCannon,
			Sides = 4,
			Tier = 2,
			Cost = 100,
			Range = 190f,
			FireRate = 0.80f,
			Damage = 56f,
			ProjectileSpeed = 330f,
			SplashRadius = 78f,
			PrimaryColor = new Color(1.0f, 0.55f, 0.05f),
			NextUpgrade = quadT3
		};
		var quadT1 = new TowerData
		{
			TowerName = "Quad-Cannon",
			RoleDescription = "Heavy / AoE Splash",
			Type = TowerType.QuadCannon,
			Sides = 4,
			Tier = 1,
			Cost = 80,
			Range = 165f,
			FireRate = 0.68f,
			Damage = 34f,
			ProjectileSpeed = 300f,
			SplashRadius = 64f,
			PrimaryColor = new Color(1.0f, 0.55f, 0.05f),
			NextUpgrade = quadT2
		};

		// 3. Penta-Pulse (Pentagon, 5 sides) - Crowd Control Slow
		var pentaT3 = new TowerData
		{
			TowerName = "Penta-Pulse",
			RoleDescription = "Crowd Control / Slow",
			Type = TowerType.PentaPulse,
			Sides = 5,
			Tier = 3,
			Cost = 135,
			Range = 175f,
			FireRate = 1.05f,
			Damage = 26f,
			SlowPercent = 0.50f,
			SlowDuration = 2.6f,
			PrimaryColor = new Color(0.78f, 0.32f, 1.0f)
		};
		var pentaT2 = new TowerData
		{
			TowerName = "Penta-Pulse",
			RoleDescription = "Crowd Control / Slow",
			Type = TowerType.PentaPulse,
			Sides = 5,
			Tier = 2,
			Cost = 90,
			Range = 152f,
			FireRate = 0.88f,
			Damage = 16f,
			SlowPercent = 0.42f,
			SlowDuration = 2.2f,
			PrimaryColor = new Color(0.78f, 0.32f, 1.0f),
			NextUpgrade = pentaT3
		};
		var pentaT1 = new TowerData
		{
			TowerName = "Penta-Pulse",
			RoleDescription = "Crowd Control / Slow",
			Type = TowerType.PentaPulse,
			Sides = 5,
			Tier = 1,
			Cost = 75,
			Range = 132f,
			FireRate = 0.72f,
			Damage = 9f,
			SlowPercent = 0.35f,
			SlowDuration = 1.9f,
			PrimaryColor = new Color(0.78f, 0.32f, 1.0f),
			NextUpgrade = pentaT2
		};

		// 4. Hex-Prism (Hexagon, 6 sides) - Continuous Ramping Beam
		var hexT3 = new TowerData
		{
			TowerName = "Hex-Prism",
			RoleDescription = "Ramping Laser Beam",
			Type = TowerType.HexPrism,
			Sides = 6,
			Tier = 3,
			Cost = 200,
			Range = 235f,
			FireRate = 1.0f,
			Damage = 48f, // Base DPS before ramp
			BeamRampPerSecond = 0.90f,
			BeamMaxMultiplier = 4.0f,
			PrimaryColor = new Color(0.22f, 1.0f, 0.25f)
		};
		var hexT2 = new TowerData
		{
			TowerName = "Hex-Prism",
			RoleDescription = "Ramping Laser Beam",
			Type = TowerType.HexPrism,
			Sides = 6,
			Tier = 2,
			Cost = 140,
			Range = 205f,
			FireRate = 1.0f,
			Damage = 32f,
			BeamRampPerSecond = 0.70f,
			BeamMaxMultiplier = 3.5f,
			PrimaryColor = new Color(0.22f, 1.0f, 0.25f),
			NextUpgrade = hexT3
		};
		var hexT1 = new TowerData
		{
			TowerName = "Hex-Prism",
			RoleDescription = "Ramping Laser Beam",
			Type = TowerType.HexPrism,
			Sides = 6,
			Tier = 1,
			Cost = 120,
			Range = 180f,
			FireRate = 1.0f,
			Damage = 20f,
			BeamRampPerSecond = 0.55f,
			BeamMaxMultiplier = 3.0f,
			PrimaryColor = new Color(0.22f, 1.0f, 0.25f),
			NextUpgrade = hexT2
		};

		return new List<TowerData> { triT1, quadT1, pentaT1, hexT1 };
	}

	public static EnemyData CreateSwarmEnemy() => new()
	{
		EnemyName = "Swarm",
		Shape = EnemyShape.SwarmCircle,
		MaxHealth = 20f,
		MoveSpeed = 165f,
		Reward = 5,
		CoreDamage = 1,
		Radius = 9f,
		SlowResistance = 0f,
		PrimaryColor = new Color(1.0f, 0.90f, 0.12f)
	};

	public static EnemyData CreateStandardEnemy() => new()
	{
		EnemyName = "Standard",
		Shape = EnemyShape.StandardDiamond,
		MaxHealth = 50f,
		MoveSpeed = 102f,
		Reward = 10,
		CoreDamage = 1,
		Radius = 13f,
		SlowResistance = 0f,
		PrimaryColor = new Color(1.0f, 0.22f, 0.42f)
	};

	public static EnemyData CreateTankEnemy() => new()
	{
		EnemyName = "Tank",
		Shape = EnemyShape.TankSquare,
		MaxHealth = 150f,
		MoveSpeed = 58f,
		Reward = 25,
		CoreDamage = 2,
		Radius = 17f,
		SlowResistance = 0.55f,
		PrimaryColor = new Color(1.0f, 0.26f, 0.14f)
	};

	public static EnemyData CreateSplitterEnemy() => new()
	{
		EnemyName = "Splitter",
		Shape = EnemyShape.SplitterOctagon,
		MaxHealth = 90f,
		MoveSpeed = 90f,
		Reward = 15,
		CoreDamage = 2,
		Radius = 16f,
		SlowResistance = 0.15f,
		SplitsOnDeath = true,
		PrimaryColor = new Color(0.22f, 0.72f, 1.0f)
	};

	public static EnemyData CreateFractalBoss() => new()
	{
		EnemyName = "Fractal Boss",
		Shape = EnemyShape.BossStar,
		MaxHealth = 1000f,
		MoveSpeed = 38f,
		Reward = 150,
		CoreDamage = 5,
		Radius = 25f,
		SlowResistance = 0.65f,
		ShieldsAllies = true,
		PrimaryColor = new Color(1.0f, 0.08f, 0.68f)
	};

	/// <summary>
	/// Builds 10 progressively escalating waves. WaveManager applies the GDD scaling formulas:
	/// HP(w) = HP_base * 1.15^(w - 1)
	/// Count(w) = Count_base + floor(1.5 * w)
	/// </summary>
	public static List<WaveData> CreateWaves()
	{
		var swarm = CreateSwarmEnemy();
		var standard = CreateStandardEnemy();
		var tank = CreateTankEnemy();
		var splitter = CreateSplitterEnemy();
		var boss = CreateFractalBoss();

		return new List<WaveData>
		{
			MakeWave(1, "First Contact",
				Group(standard, 5, 0.95f, 0f)),
			MakeWave(2, "Swarm Surge",
				Group(swarm, 6, 0.42f, 0f),
				Group(standard, 3, 0.85f, 2.0f)),
			MakeWave(3, "Diamond Phalanx",
				Group(standard, 6, 0.75f, 0f),
				Group(tank, 2, 1.8f, 1.8f, scaleCount: false)),
			MakeWave(4, "Mitosis",
				Group(splitter, 3, 1.35f, 0f, scaleCount: false),
				Group(swarm, 6, 0.35f, 1.5f)),
			MakeWave(5, "Armored Vanguard",
				Group(tank, 4, 1.45f, 0f, scaleCount: false),
				Group(standard, 5, 0.65f, 1.2f),
				Group(splitter, 3, 1.1f, 2.0f, scaleCount: false)),
			MakeWave(6, "Velocity Breach",
				Group(swarm, 10, 0.28f, 0f),
				Group(splitter, 4, 1.05f, 1.5f, scaleCount: false)),
			MakeWave(7, "Heavy Geometry",
				Group(tank, 5, 1.2f, 0f, scaleCount: false),
				Group(splitter, 5, 0.95f, 1.5f, scaleCount: false),
				Group(swarm, 8, 0.28f, 1.0f)),
			MakeWave(8, "Recursive Assault",
				Group(splitter, 6, 0.85f, 0f, scaleCount: false),
				Group(tank, 5, 1.1f, 1.2f, scaleCount: false),
				Group(standard, 8, 0.5f, 1.0f)),
			MakeWave(9, "Overload",
				Group(swarm, 12, 0.24f, 0f),
				Group(tank, 6, 1.0f, 1.0f, scaleCount: false),
				Group(splitter, 6, 0.8f, 1.0f, scaleCount: false)),
			MakeWave(10, "Fractal Singularity",
				Group(tank, 4, 1.1f, 0f, scaleCount: false),
				Group(boss, 1, 1.0f, 1.5f, scaleCount: false),
				Group(splitter, 5, 0.9f, 2.0f, scaleCount: false),
				Group(swarm, 12, 0.25f, 1.5f))
		};
	}

	private static WaveData MakeWave(int num, string title, params WaveSpawnGroup[] groups)
	{
		var arr = new Array<WaveSpawnGroup>();
		foreach (var g in groups) arr.Add(g);
		return new WaveData { WaveNumber = num, Title = title, Groups = arr };
	}

	private static WaveSpawnGroup Group(EnemyData enemy, int baseCount, float interval, float delay, bool scaleCount = true)
	{
		return new WaveSpawnGroup
		{
			Enemy = enemy,
			BaseCount = baseCount,
			SpawnInterval = interval,
			DelayBeforeGroup = delay,
			ScaleCountWithWave = scaleCount
		};
	}
}
