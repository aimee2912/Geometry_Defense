using Godot;

namespace VectorBastion;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	public const int StartingVertices = 150;
	public const int StartingCoreIntegrity = 20;
	public const float SellRefundRatio = 0.70f;
	public const int TotalWaves = 10;

	public int Vertices { get; private set; } = StartingVertices;
	public int CoreIntegrity { get; private set; } = StartingCoreIntegrity;
	public int CurrentWave { get; private set; } = 0;
	public int EnemiesDefeatedCount { get; private set; } = 0;
	public bool IsGameOver { get; private set; } = false;
	public bool IsFastForward { get; private set; } = false;
	public bool IsPaused { get; private set; } = false;

	public override void _EnterTree()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Ready()
	{
		if (SignalBus.Instance != null)
		{
			SignalBus.Instance.EnemyDefeated += OnEnemyDefeated;
			SignalBus.Instance.WaveStarted += OnWaveStarted;
			SignalBus.Instance.WaveCompleted += OnWaveCompleted;
		}
	}

	public void ResetGame()
	{
		Vertices = StartingVertices;
		CoreIntegrity = StartingCoreIntegrity;
		CurrentWave = 0;
		EnemiesDefeatedCount = 0;
		IsGameOver = false;
		IsFastForward = false;
		IsPaused = false;
		Engine.TimeScale = 1.0f;
		GetTree().Paused = false;

		SignalBus.Instance?.EmitSignal(SignalBus.SignalName.CurrencyChanged, Vertices);
		SignalBus.Instance?.EmitSignal(SignalBus.SignalName.CoreDamaged, CoreIntegrity, 0);
		SignalBus.Instance?.EmitSignal(SignalBus.SignalName.GameReset);
	}

	public bool CanAfford(int cost) => Vertices >= cost;

	public bool SpendVertices(int amount)
	{
		if (amount < 0 || Vertices < amount)
			return false;

		Vertices -= amount;
		SignalBus.Instance?.EmitSignal(SignalBus.SignalName.CurrencyChanged, Vertices);
		return true;
	}

	public void AddVertices(int amount)
	{
		if (amount <= 0)
			return;

		Vertices += amount;
		SignalBus.Instance?.EmitSignal(SignalBus.SignalName.CurrencyChanged, Vertices);
	}

	public void DamageCore(int amount)
	{
		if (IsGameOver || amount <= 0)
			return;

		CoreIntegrity = Mathf.Max(0, CoreIntegrity - amount);
		SignalBus.Instance?.EmitSignal(SignalBus.SignalName.CoreDamaged, CoreIntegrity, amount);
		SignalBus.Instance?.EmitSignal(SignalBus.SignalName.ScreenShakeRequested, 8.0f + amount * 2.0f, 0.25f);
		SoundManager.Instance?.PlayCoreHit();

		if (CoreIntegrity <= 0)
		{
			TriggerGameOver(false);
		}
	}

	public void ToggleFastForward()
	{
		IsFastForward = !IsFastForward;
		Engine.TimeScale = IsFastForward ? 2.0f : 1.0f;
	}

	public void TogglePause()
	{
		if (IsGameOver)
			return;

		IsPaused = !IsPaused;
		GetTree().Paused = IsPaused;
	}

	private void OnEnemyDefeated(int reward)
	{
		EnemiesDefeatedCount++;
		AddVertices(reward);
	}

	private void OnWaveStarted(int waveIndex, int totalWaves)
	{
		CurrentWave = waveIndex;
	}

	private void OnWaveCompleted(int waveIndex, int totalWaves)
	{
		// Bonus vertices for clearing a wave
		AddVertices(15 + waveIndex * 5);
		SoundManager.Instance?.PlayWaveClear();

		if (waveIndex >= totalWaves && !IsGameOver)
		{
			TriggerGameOver(true);
		}
	}

	private void TriggerGameOver(bool victory)
	{
		IsGameOver = true;
		Engine.TimeScale = 1.0f;
		IsFastForward = false;

		if (victory)
			SoundManager.Instance?.PlayVictory();
		else
			SoundManager.Instance?.PlayDefeat();

		SignalBus.Instance?.EmitSignal(SignalBus.SignalName.GameOver, victory);
	}
}
