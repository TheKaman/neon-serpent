namespace NeonSerpent.Core
{
    /// <summary>Game-wide state enum. GameManager is the only class that changes this.</summary>
    public enum GameState
    {
        Boot,
        MainMenu,
        Playing,
        Paused,
        GameOver,
        LevelComplete
    }

    /// <summary>Available game modes, passed to GameManager.StartGame().</summary>
    public enum GameMode
    {
        ClassicEndless,
        TimeAttack,
        Campaign
    }
}
