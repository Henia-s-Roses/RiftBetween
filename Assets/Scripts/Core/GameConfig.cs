// GameConfig.cs


// static class for all game constantss
// para di maghanap sa indivudual scripts, here na lahat

public static class GameConfig
{
    // ── World Swap Timers ─────────────────────────────────────────────────────
    public const float SWAP_MIN_1 = 20f;   // Stage 1: minimum seconds before a swap
    public const float SWAP_MAX_1 = 30f;   // Stage 1: maximum seconds before a swap

    public const float SWAP_MIN_2 = 10f;   // Stage 2: mas mabilis min
    public const float SWAP_MAX_2 = 20f;   // Stage 2: 
    
    public const float SWAP_INVINCIBILITY = 0.5f;  // invincible for very short time so no instadeath on swap

    // ── Player ────────────────────────────────────────────────────────────────
    public const float PLAYER_MAX_HP = 100f;        // 100% hp
    public const float PLAYER_MOVE_SPEED = 5f;      
    public const float PLAYER_SPRINT_SPEED = 8f;    
    public const float PLAYER_JUMP_FORCE = 12f;

    // ── Revive ────────────────────────────────────────────────────────────────
    public const float DOWNED_DURATION = 15f;   // bago mamatay completely after downed when not revived
    public const float REVIVE_DURATION = 3f;    // gano katagal i-hold ang revive
    public const float REVIVE_HP_PCT = 0.3f;  // HP percentage restored on revive (30%)
    public const float REVIVE_INVINCIBILITY = 2f;    // invinc duration after being revived
    public const float REVIVE_RANGE = 1.2f;  // radius or range of revive zone

    // ── Boss ──────────────────────────────────────────────────────────────────
    public const float BOSS_PHASE2_PCT = 0.5f;  // bosses enters phase 2 at 50% HP
    public const float BOSS_PHASE3_PCT = 0.3f;  // final boss only: phase 3 at 30% HP

    // ── Items ─────────────────────────────────────────────────────────────────
    public const float ITEM_PICKUP_RANGE = 0.8f;
    public const float PASSIVE_DURATION = 25f;

    // ── Score ─────────────────────────────────────────────────────────────────
    public const int SCORE_ENEMY_KILL = 10;         // score per basic enemy killed
    public const int SCORE_BOSS_MINION = 25;
    public const int SCORE_SWAP_SURVIVED = 15;
    public const int SCORE_REVIVE = 50;
    public const int SCORE_BOSS_KILL = 200;
    public const int SCORE_NO_GAMEOVER = 100;   // Bonus for completing run without a game over
}