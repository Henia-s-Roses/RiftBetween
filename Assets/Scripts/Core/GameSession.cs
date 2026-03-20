// GameSession.cs


// to hold and track game stat and other game info for access of any scripts

using System.Collections.Generic;

public static class GameSession
{
    // -- Current game state ----------------------------------------------------
    public static GamePhase CurrentPhase = GamePhase.Menu;
    public static WorldState ActiveWorld = WorldState.WorldA; 
    public static bool IsStage2 = false;
    public static bool BossDefeated = false;

    public static Dictionary<int, CharacterChoice> PlayerChoices
    = new Dictionary<int, CharacterChoice>( ); // key = playerIndex, value = choice

    public static Dictionary<int, string> PlayerPrefabNames
        = new Dictionary<int, string>( );

    // -- score tracking --------------------------------------------------------
    public static int TotalScore = 0;
    public static int KillScore = 0;
    public static int SwapScore = 0;
    public static int ReviveScore = 0;
    public static int BonusScore = 0;

    // -- runtime stats (for win screen calcs) ---------------------------------
    public static int TotalKills = 0;
    public static int SwapsSurvived = 0;
    public static int SuccessfulRevives = 0;
    public static bool CompletedNoWipe = true;  // Flipped to false on any Game Over attempt

    // -- RESET GAME STATES -----------------------------------
    public static void Reset( )
    {
        CurrentPhase = GamePhase.Stage1;
        ActiveWorld = WorldState.WorldA;
        IsStage2 = false;
        BossDefeated = false;
        TotalScore = 0;
        KillScore = 0;
        SwapScore = 0;
        ReviveScore = 0;
        BonusScore = 0;
        TotalKills = 0;
        SwapsSurvived = 0;
        SuccessfulRevives = 0;
        CompletedNoWipe = true;
        PlayerChoices.Clear( );
        PlayerPrefabNames.Clear( );
    }

    // -- OTHERZS ---------------------------
    // to add all points for total score and category-specific scores
    public static void AddScore(int points, ScoreCategory category)
    {
        TotalScore += points;

        switch (category)
        {
            case ScoreCategory.Kill: KillScore += points; break;
            case ScoreCategory.Swap: SwapScore += points; break;
            case ScoreCategory.Revive: ReviveScore += points; break;
            case ScoreCategory.Bonus: BonusScore += points; break;
        }
    }
}

// Score categories
// kill
// swap
// revive
// bonus (misc pts)
public enum ScoreCategory { Kill, Swap, Revive, Bonus }