using Godot;
using Godot.Collections;

namespace VectorBastion;

[GlobalClass]
public partial class WaveSpawnGroup : Resource
{
	[Export] public EnemyData Enemy { get; set; }
	[Export] public int BaseCount { get; set; } = 5;
	[Export] public float SpawnInterval { get; set; } = 0.7f;
	[Export] public float DelayBeforeGroup { get; set; } = 0f;
	[Export] public bool ScaleCountWithWave { get; set; } = true;
}

[GlobalClass]
public partial class WaveData : Resource
{
	[Export] public int WaveNumber { get; set; } = 1;
	[Export] public string Title { get; set; } = "Wave 1";
	[Export] public Array<WaveSpawnGroup> Groups { get; set; } = new();
}
