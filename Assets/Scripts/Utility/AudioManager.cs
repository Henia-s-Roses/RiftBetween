// AudioManager.cs
// Static-access audio manager for background music and SFX.
// Attach to a persistent GameObject in your first scene.
// All play methods are callable from anywhere via AudioManager.Instance.
//
// Setup:
//   1. Create an empty GameObject named "AudioManager" in your Lobby scene
//   2. Attach this script
//   3. Assign audio clips in the Inspector under each header
//   4. The GameObject persists across scene loads automatically

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static AudioManager Instance { get; private set; }

    // ── Audio Sources ─────────────────────────────────────────────────────────
    // Two sources for music so we can crossfade between tracks.
    // One dedicated source for SFX so music never interrupts effects.

    private AudioSource _musicSource;       // Currently playing music track
    private AudioSource _musicSourceB;      // Crossfade target
    private AudioSource _sfxSource;         // One-shot SFX

    // ── Volume ────────────────────────────────────────────────────────────────

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    // ── Music Clips ───────────────────────────────────────────────────────────

    [Header("Music — Background")]
    [Tooltip("Plays on the main menu and lobby screen")]
    public AudioClip musicMainMenu;

    [Tooltip("Plays during Stage 1 when World A is active")]
    public AudioClip musicWorldA;

    [Tooltip("Plays during Stage 1 when World B is active")]
    public AudioClip musicWorldB;

    [Tooltip("Plays during Stage 2")]
    public AudioClip musicStage2;

    [Tooltip("Plays during boss encounters")]
    public AudioClip musicBoss;

    [Tooltip("Plays on the win screen")]
    public AudioClip musicVictory;

    [Tooltip("Plays on the game over screen")]
    public AudioClip musicGameOver;

    // ── SFX — Player ──────────────────────────────────────────────────────────

    [Header("SFX — Player")]
    [Tooltip("Plays when a player lands a melee hit")]
    public AudioClip sfxMeleeHit;

    [Tooltip("Plays when a player fires an orb projectile")]
    public AudioClip sfxProjectileFire;

    [Tooltip("Plays when a player takes damage")]
    public AudioClip sfxPlayerHurt;

    [Tooltip("Plays when a player dies and enters respawn countdown")]
    public AudioClip sfxPlayerDeath;

    [Tooltip("Plays when a player respawns")]
    public AudioClip sfxPlayerRespawn;

    [Tooltip("Plays when a player jumps")]
    public AudioClip sfxJump;

    [Tooltip("Plays when a player lands on the ground")]
    public AudioClip sfxLand;

    // ── SFX — Enemies ─────────────────────────────────────────────────────────

    [Header("SFX — Enemies")]
    [Tooltip("Plays when any enemy takes damage")]
    public AudioClip sfxEnemyHurt;

    [Tooltip("Plays when any enemy dies")]
    public AudioClip sfxEnemyDeath;

    [Tooltip("Plays when the slime splits into mini slimes")]
    public AudioClip sfxSlimeSplit;

    [Tooltip("Plays when the mage fires an orb")]
    public AudioClip sfxMageFire;

    [Tooltip("Plays when the boss takes damage")]
    public AudioClip sfxBossHurt;

    [Tooltip("Plays when the boss dies")]
    public AudioClip sfxBossDeath;

    // ── SFX — World Swap ──────────────────────────────────────────────────────

    [Header("SFX — World")]
    [Tooltip("Plays at the moment the world swaps")]
    public AudioClip sfxWorldSwap;

    // ── SFX — Items ───────────────────────────────────────────────────────────

    [Header("SFX — Items")]
    [Tooltip("Plays when a crate is broken")]
    public AudioClip sfxCrateBreak;

    [Tooltip("Plays when any item is picked up")]
    public AudioClip sfxItemPickup;

    [Tooltip("Plays when a heal item is used")]
    public AudioClip sfxHeal;

    [Tooltip("Plays when the Pixel Shield activates")]
    public AudioClip sfxShieldActivate;

    [Tooltip("Plays when the Pixel Shield absorbs a hit and breaks")]
    public AudioClip sfxShieldBreak;

    [Tooltip("Plays when the Rift Shard buff activates")]
    public AudioClip sfxRiftShardActivate;

    // ── SFX — UI ──────────────────────────────────────────────────────────────

    [Header("SFX — UI")]
    [Tooltip("Plays when a UI button is clicked")]
    public AudioClip sfxButtonClick;

    [Tooltip("Plays when a character is selected in the lobby")]
    public AudioClip sfxCharacterSelect;

    [Tooltip("Plays when the game starts from the lobby")]
    public AudioClip sfxGameStart;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        // Singleton — one AudioManager persists for the entire session
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Survives scene loads

        BuildAudioSources( );
    }

    private void BuildAudioSources( )
    {
        // Music source A — primary
        _musicSource = gameObject.AddComponent<AudioSource>( );
        _musicSource.loop = true;
        _musicSource.volume = musicVolume;
        _musicSource.playOnAwake = false;

        // Music source B — crossfade target
        _musicSourceB = gameObject.AddComponent<AudioSource>( );
        _musicSourceB.loop = true;
        _musicSourceB.volume = 0f;
        _musicSourceB.playOnAwake = false;

        // SFX source — non-looping, plays one-shots
        _sfxSource = gameObject.AddComponent<AudioSource>( );
        _sfxSource.loop = false;
        _sfxSource.volume = sfxVolume;
        _sfxSource.playOnAwake = false;
    }

    // ── Music Play Methods ────────────────────────────────────────────────────

    public void PlayMusicMainMenu( ) => PlayMusic(musicMainMenu);
    public void PlayMusicWorldA( ) => PlayMusic(musicWorldA);
    public void PlayMusicWorldB( ) => PlayMusic(musicWorldB);
    public void PlayMusicStage2( ) => PlayMusic(musicStage2);
    public void PlayMusicBoss( ) => PlayMusic(musicBoss);
    public void PlayMusicVictory( ) => PlayMusic(musicVictory);
    public void PlayMusicGameOver( ) => PlayMusic(musicGameOver);

    public void StopMusic( )
    {
        _musicSource.Stop( );
        _musicSourceB.Stop( );
    }

    // ── SFX Play Methods — Player ─────────────────────────────────────────────

    public void PlayMeleeHit( ) => PlaySFX(sfxMeleeHit);
    public void PlayProjectileFire( ) => PlaySFX(sfxProjectileFire);
    public void PlayPlayerHurt( ) => PlaySFX(sfxPlayerHurt);
    public void PlayPlayerDeath( ) => PlaySFX(sfxPlayerDeath);
    public void PlayPlayerRespawn( ) => PlaySFX(sfxPlayerRespawn);
    public void PlayJump( ) => PlaySFX(sfxJump);
    public void PlayLand( ) => PlaySFX(sfxLand);

    // ── SFX Play Methods — Enemies ────────────────────────────────────────────

    public void PlayEnemyHurt( ) => PlaySFX(sfxEnemyHurt);
    public void PlayEnemyDeath( ) => PlaySFX(sfxEnemyDeath);
    public void PlaySlimeSplit( ) => PlaySFX(sfxSlimeSplit);
    public void PlayMageFire( ) => PlaySFX(sfxMageFire);
    public void PlayBossHurt( ) => PlaySFX(sfxBossHurt);
    public void PlayBossDeath( ) => PlaySFX(sfxBossDeath);

    // ── SFX Play Methods — World ──────────────────────────────────────────────

    public void PlayWorldSwap( ) => PlaySFX(sfxWorldSwap);

    // ── SFX Play Methods — Items ──────────────────────────────────────────────

    public void PlayCrateBreak( ) => PlaySFX(sfxCrateBreak);
    public void PlayItemPickup( ) => PlaySFX(sfxItemPickup);
    public void PlayHeal( ) => PlaySFX(sfxHeal);
    public void PlayShieldActivate( ) => PlaySFX(sfxShieldActivate);
    public void PlayShieldBreak( ) => PlaySFX(sfxShieldBreak);
    public void PlayRiftShardActivate( ) => PlaySFX(sfxRiftShardActivate);

    // ── SFX Play Methods — UI ─────────────────────────────────────────────────

    public void PlayButtonClick( ) => PlaySFX(sfxButtonClick);
    public void PlayCharacterSelect( ) => PlaySFX(sfxCharacterSelect);
    public void PlayGameStart( ) => PlaySFX(sfxGameStart);

    // ── Volume Control ────────────────────────────────────────────────────────

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        _musicSource.volume = musicVolume;
        _musicSourceB.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        _sfxSource.volume = sfxVolume;
    }

    // ── Internal Helpers ──────────────────────────────────────────────────────

    // Plays a music clip — skips if already playing the same track.
    // Instant switch — add a crossfade coroutine here later if desired.
    private void PlayMusic(AudioClip clip)
    {
        if (clip == null)
        {
            RiftLogger.Warn($"AudioManager: music clip not assigned", this);
            return;
        }

        // Skip if this track is already playing — prevents restart on re-call
        if (_musicSource.clip == clip && _musicSource.isPlaying) return;

        _musicSource.clip = clip;
        _musicSource.volume = musicVolume;
        _musicSource.Play( );

        RiftLogger.Log($"Music playing: {clip.name}", this);
    }

    // Plays a one-shot SFX clip — multiple SFX can overlap.
    private void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            RiftLogger.Warn($"AudioManager: SFX clip not assigned", this);
            return;
        }

        // PlayOneShot allows overlapping sounds on the same source
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }
}