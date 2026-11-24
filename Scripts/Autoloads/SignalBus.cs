using Godot;

namespace VectorBastion;

public partial class SignalBus : Node
{
    public static SignalBus Instance { get; private set; }

    [Signal] public delegate void EnemyDefeatedEventHandler(int reward);
    [Signal] public delegate void CoreDamagedEventHandler(int remainingHealth, int damageAmount);
    [Signal] public delegate void CurrencyChangedEventHandler(int newAmount);
    [Signal] public delegate void WaveStartedEventHandler(int waveIndex, int totalWaves);
    [Signal] public delegate void WaveCompletedEventHandler(int waveIndex, int totalWaves);
    [Signal] public delegate void TowerSelectedEventHandler(Node tower);
    [Signal] public delegate void GameOverEventHandler(bool victory);
    [Signal] public delegate void GameResetEventHandler();
    [Signal] public delegate void ScreenShakeRequestedEventHandler(float intensity, float duration);

    public override void _EnterTree()
    {
        Instance = this;
    }
}
