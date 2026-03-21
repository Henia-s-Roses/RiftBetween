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

    public static AudioManager Instance { get; private set; }


    private AudioSource _musicSource;       
    private AudioSource _musicSourceB;      
    private AudioSource _sfxSource;         

    // ── Volume ────────────────────────────────────────────────────────────────

    // pede pala tu
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    // ── Music Clips ───────────────────────────────────────────────────────────

    public AudioClip musicMainMenu;

    public AudioClip musicWorldA;

    public AudioClip musicWorldB;


    public AudioClip musicBoss;

    public AudioClip musicVictory;

    public AudioClip musicGameOver;

    // ── SFX — Player ──────────────────────────────────────────────────────────

    public AudioClip sfxMeleeHit;
    public AudioClip sfxProjectileFire;

    public AudioClip sfxPlayerHurt;

    public AudioClip sfxPlayerDeath;
    public AudioClip sfxPlayerRespawn;
    public AudioClip sfxJump;

    public AudioClip sfxLand;

    // ── SFX — Enemies ─────────────────────────────────────────────────────────


    public AudioClip sfxEnemyDeath;


    // ── SFX — World Swap ──────────────────────────────────────────────────────

    public AudioClip sfxWorldSwap;

    // ── SFX — UI ──────────────────────────────────────────────────────────────

    public AudioClip sfxButtonClick;

    public AudioClip sfxCharacterSelect;

    public AudioClip sfxGameStart;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // wont destroy on scene change

        MakeAudioSource( );
    }

    private void MakeAudioSource( )
    {
        _musicSource = gameObject.AddComponent<AudioSource>( );
        _musicSource.loop = true;
        _musicSource.volume = musicVolume;
        _musicSource.playOnAwake = false;

        _musicSourceB = gameObject.AddComponent<AudioSource>( );
        _musicSourceB.loop = true;
        _musicSourceB.volume = 0f;
        _musicSourceB.playOnAwake = false;
        
        
        
        _sfxSource = gameObject.AddComponent<AudioSource>( );
        _sfxSource.loop = false;
        _sfxSource.volume = sfxVolume;
        _sfxSource.playOnAwake = false;
    }

    // ── Music Play Methods ────────────────────────────────────────────────────

    public void PlayMusicMainMenu( ) => PlayMusic(musicMainMenu);
    public void PlayMusicWorldA( ) => PlayMusic(musicWorldA);
    public void PlayMusicWorldB( ) => PlayMusic(musicWorldB);
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

    // ── Enemey sfx────────────────────────────────────────────

    //public void PlayEnemyHurt( ) => PlaySFX(sfxEnemyHurt);
    public void PlayEnemyDeath( ) => PlaySFX(sfxEnemyDeath);
    //public void PlaySlimeSplit( ) => PlaySFX(sfxSlimeSplit


    public void PlayWorldSwap( ) => PlaySFX(sfxWorldSwap);


    //public void PlayHeal( ) => PlaySFX(sfxHeal);
    //public void PlayShieldActivate( ) => PlaySFX(sfxShieldActivate);
    //public void PlayShieldBreak( ) => PlaySFX(sfxShieldBreak);


    public void PlayButtonClick( ) => PlaySFX(sfxButtonClick);
    public void PlayCharacterSelect( ) => PlaySFX(sfxCharacterSelect);
    public void PlayGameStart( ) => PlaySFX(sfxGameStart);

    // ── BVOLUME ────────────────────────────────────────────────────────

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


    private void PlayMusic(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (_musicSource.clip == clip && _musicSource.isPlaying) return;

        _musicSource.clip = clip;
        _musicSource.volume = musicVolume;
        _musicSource.Play( );

        RiftLogger.Log($"Music playing: {clip.name}", this);
    }

    // play sfx
    private void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        _sfxSource.PlayOneShot(clip, sfxVolume);
    }
}