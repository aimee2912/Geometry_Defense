using System.Collections.Generic;
using Godot;

namespace VectorBastion;

/// <summary>
/// Orchestrates wave spawning, GDD scaling formulas, active enemy tracking, and wave completion.
/// </summary>
public partial class WaveManager : Node
{
	public List<BaseEnemy> ActiveEnemies { get; } = new();
	public bool IsWaveActive { get; private set; }
	public int CurrentWaveIndex { get; private set; } // 1-based once started
	public int TotalWaves => _waves.Count;
	public string NextWaveTitle => CurrentWaveIndex < _waves.Count ? _waves[CurrentWaveIndex].Title : "Complete";

	private GridManager _grid;
	private List<WaveData> _waves = new();

	private struct PendingSpawn
	{
		public EnemyData Enemy;
		public float Delay;
	}

	private readonly Queue<PendingSpawn> _spawnQueue = new();
	private float _spawnTimer;

	public void Initialize(GridManager grid)
	{
		_grid = grid;
		_waves = GameCatalog.CreateWaves();
	}

	public void ResetWaves()
	{
		_spawnQueue.Clear();
		foreach (var e in ActiveEnemies)
		{
			if (IsInstanceValid(e))
				e.QueueFree();
		}
		ActiveEnemies.Clear();
		IsWaveActive = false;
		CurrentWaveIndex = 0;
		_spawnTimer = 0f;
	}

	public bool StartNextWave()
	{
		if (IsWaveActive || GameManager.Instance.IsGameOver)
			return false;

		if (CurrentWaveIndex >= _waves.Count)
			return false;

		WaveData wave = _waves[CurrentWaveIndex];
		CurrentWaveIndex++;
		IsWaveActive = true;
		_spawnQueue.Clear();

		foreach (WaveSpawnGroup group in wave.Groups)
		{
			// GDD Formula: Count(w) = Count_base + floor(1.5 * w)
			int count = group.ScaleCountWithWave
				? group.BaseCount + Mathf.FloorToInt(1.5f * CurrentWaveIndex)
				: group.BaseCount;

			for (int i = 0; i < count; i++)
			{
				float delay = (i == 0) ? group.DelayBeforeGroup : group.SpawnInterval;
				_spawnQueue.Enqueue(new PendingSpawn { Enemy = group.Enemy, Delay = delay });
			}
		}

		_spawnTimer = _spawnQueue.Count > 0 ? _spawnQueue.Peek().Delay : 0f;
		SignalBus.Instance?.EmitSignal(SignalBus.SignalName.WaveStarted, CurrentWaveIndex, TotalWaves);
		return true;
	}

	public override void _Process(double delta)
	{
		if (!IsWaveActive || GameManager.Instance.IsGameOver)
			return;

		if (_spawnQueue.Count > 0)
		{
			_spawnTimer -= (float)delta;
			while (_spawnQueue.Count > 0 && _spawnTimer <= 0f)
			{
				var next = _spawnQueue.Dequeue();
				SpawnEnemy(next.Enemy, 0f);
				if (_spawnQueue.Count > 0)
					_spawnTimer += _spawnQueue.Peek().Delay;
			}
		}
		else if (ActiveEnemies.Count == 0)
		{
			IsWaveActive = false;
			SignalBus.Instance?.EmitSignal(SignalBus.SignalName.WaveCompleted, CurrentWaveIndex, TotalWaves);
		}
	}

	private void SpawnEnemy(EnemyData data, float startProgress)
	{
		if (_grid?.EnemyPath == null || data == null)
			return;

		var enemy = new BaseEnemy();
		enemy.Initialize(data, CurrentWaveIndex, this, startProgress);
		_grid.EnemyPath.AddChild(enemy);
		ActiveEnemies.Add(enemy);
	}

	public void SpawnSplitChildren(float parentProgress, int waveNumber)
	{
		if (_grid?.EnemyPath == null)
			return;

		var std = GameCatalog.CreateStandardEnemy();
		float[] offsets = { -14f, 14f };
		foreach (float off in offsets)
		{
			var child = new BaseEnemy();
			child.Initialize(std, waveNumber, this, Mathf.Max(0f, parentProgress + off));
			_grid.EnemyPath.AddChild(child);
			ActiveEnemies.Add(child);
		}
	}

	public void NotifyEnemyRemoved(BaseEnemy enemy)
	{
		ActiveEnemies.Remove(enemy);
	}
}
