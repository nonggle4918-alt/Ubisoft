using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Central sound-effect hub. Persists across scene loads (title <-> in-game) so the
// same instance can play UI click sounds on both screens. Leave a clip slot empty
// in the Inspector to fall back to a generated placeholder tone (see ToneGenerator).
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Enemy")]
    [SerializeField] private AudioClip enemyHitClip;
    [SerializeField] private AudioClip enemyDestroyedClip;
    [SerializeField] private AudioClip enemySpawnedClip;

    [Header("Ally")]
    [SerializeField] private AudioClip unitPurchasedClip;
    [SerializeField] private AudioClip unitMovedClip;

    [Header("Rarity / Promotion")]
    [SerializeField] private AudioClip tier4Clip;
    [SerializeField] private AudioClip tier5Clip;
    [SerializeField] private AudioClip heroClip;

    [Header("UI")]
    [SerializeField] private AudioClip buttonClickClip;

    [Header("Unit Attack (per attack type)")]
    [SerializeField] private AudioClip attackDirectClip;
    [SerializeField] private AudioClip attackProjectileClip;
    [SerializeField] private AudioClip attackDiagonalProjectileClip;
    [SerializeField] private AudioClip attackLaserClip;
    [SerializeField] private AudioClip attackHomingClip;
    [SerializeField] private AudioClip attackSplashClip;
    [SerializeField] private AudioClip attackPegasusClip;
    [SerializeField] private AudioClip attackDragonClip;
    [SerializeField] private AudioClip attackCannonClip;
    [SerializeField] private AudioClip attackMeteorClip;
    [SerializeField] private AudioClip attackAlchemyClip;

    // Base mix level tuned in the Inspector. The options slider scales this rather than
    // replacing it, so raising the slider to 100% never exceeds the intended mix.
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    public const string EffectsVolumeKey = "EffectsVolume";

    private float userVolume = 1f;

    // PlayOneShot layers voices and overlapping copies sum their amplitude, so an area
    // attack that damages every enemy in range within a single frame would otherwise
    // spike far above a single hit and then drop out once Unity's real-voice budget
    // (32, see ProjectSettings/AudioManager) is exhausted. These limits keep the
    // perceived loudness steady regardless of how many events land at once.
    private const float SameClipCooldown = 0.06f;
    private const float VoiceWindow = 0.12f;
    private const int MaxConcurrentVoices = 8;

    private AudioSource source;
    private Dictionary<AttackType, AudioClip> attackClips;
    private readonly HashSet<Button> boundButtons = new HashSet<Button>();
    private readonly Dictionary<AudioClip, float> lastPlayTimes = new Dictionary<AudioClip, float>();
    private readonly Queue<float> recentPlayTimes = new Queue<float>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;

        // The options slider lives in UIManager, which only runs once its scene is loaded;
        // read the saved level here so sounds before that already use the chosen volume.
        userVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsVolumeKey, 1f));

        FillMissingClipsWithPlaceholders();
        BuildAttackClipMap();
    }

    private void Start()
    {
        BindAllButtonsInScene();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BindAllButtonsInScene();

    private void BuildAttackClipMap()
    {
        attackClips = new Dictionary<AttackType, AudioClip>
        {
            { AttackType.Direct, attackDirectClip },
            { AttackType.Projectile, attackProjectileClip },
            { AttackType.DiagonalProjectile, attackDiagonalProjectileClip },
            { AttackType.Laser, attackLaserClip },
            { AttackType.Homing, attackHomingClip },
            { AttackType.Splash, attackSplashClip },
            { AttackType.Pegasus, attackPegasusClip },
            { AttackType.Dragon, attackDragonClip },
            { AttackType.Cannon, attackCannonClip },
            { AttackType.Meteor, attackMeteorClip },
            { AttackType.Alchemy, attackAlchemyClip },
        };
    }

    public void PlayEnemyHit() => Play(enemyHitClip);
    public void PlayEnemyDestroyed() => Play(enemyDestroyedClip);
    public void PlayEnemySpawned() => Play(enemySpawnedClip);
    public void PlayUnitPurchased() => Play(unitPurchasedClip);
    public void PlayUnitMoved() => Play(unitMovedClip);
    public void PlayTier4() => Play(tier4Clip);
    public void PlayTier5() => Play(tier5Clip);
    public void PlayHero() => Play(heroClip);
    public void PlayButtonClick() => Play(buttonClickClip);

    public void PlayUnitAttack(AttackType type)
    {
        if (attackClips != null && attackClips.TryGetValue(type, out AudioClip clip))
            Play(clip);
    }

    // Called when a gacha pull resolves to a rare tier; no-ops for tiers without a dedicated sting.
    public void PlayTierReward(int tier)
    {
        if (tier == 5) PlayTier5();
        else if (tier == 4) PlayTier4();
    }

    public float Volume => userVolume;

    public void SetVolume(float value) => userVolume = Mathf.Clamp01(value);

    private void Play(AudioClip clip)
    {
        if (clip == null || source == null) return;

        // Time.unscaledTime so throttling still advances while the game is paused.
        float now = Time.unscaledTime;

        // Copies of one clip started together are sample-aligned, so they sum into a single
        // much louder sound rather than a discernible second hit. Collapse the burst.
        if (lastPlayTimes.TryGetValue(clip, out float lastTime) && now - lastTime < SameClipCooldown)
            return;

        while (recentPlayTimes.Count > 0 && now - recentPlayTimes.Peek() > VoiceWindow)
            recentPlayTimes.Dequeue();

        if (recentPlayTimes.Count >= MaxConcurrentVoices) return;

        lastPlayTimes[clip] = now;
        recentPlayTimes.Enqueue(now);

        // n overlapping voices sum to roughly sqrt(n) times the power of one, so scale the
        // new voice down by that factor to hold the mix at a stable level.
        source.PlayOneShot(clip, volume * userVolume / Mathf.Sqrt(recentPlayTimes.Count));
    }

    // Buttons created at runtime (after this manager's scene scan already ran) should
    // call this directly. Idempotent, so double-binding the same button is harmless.
    public void BindButtonClickSound(Button button)
    {
        if (button == null || !boundButtons.Add(button)) return;
        button.onClick.AddListener(PlayButtonClick);
    }

    private void BindAllButtonsInScene()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var button in buttons)
            BindButtonClickSound(button);
    }

    private void FillMissingClipsWithPlaceholders()
    {
        enemyHitClip ??= ToneGenerator.Create("SFX_EnemyHit", 220f, 0.07f, ToneGenerator.Wave.Square, 0.5f);
        enemyDestroyedClip ??= ToneGenerator.Create("SFX_EnemyDestroyed", 150f, 0.28f, ToneGenerator.Wave.Sawtooth, 0.6f);
        enemySpawnedClip ??= ToneGenerator.Create("SFX_EnemySpawned", 330f, 0.12f, ToneGenerator.Wave.Triangle, 0.4f);

        unitPurchasedClip ??= ToneGenerator.Create("SFX_UnitPurchased", 660f, 0.15f, ToneGenerator.Wave.Sine, 0.5f);
        unitMovedClip ??= ToneGenerator.Create("SFX_UnitMoved", 500f, 0.06f, ToneGenerator.Wave.Sine, 0.35f);

        tier4Clip ??= ToneGenerator.Create("SFX_Tier4", 523f, 0.35f, ToneGenerator.Wave.Sine, 0.6f);
        tier5Clip ??= ToneGenerator.Create("SFX_Tier5", 660f, 0.5f, ToneGenerator.Wave.Sine, 0.7f);
        heroClip ??= ToneGenerator.Create("SFX_Hero", 784f, 0.7f, ToneGenerator.Wave.Sine, 0.8f);

        buttonClickClip ??= ToneGenerator.Create("SFX_ButtonClick", 900f, 0.045f, ToneGenerator.Wave.Square, 0.3f);

        attackDirectClip ??= ToneGenerator.Create("SFX_AttackDirect", 300f, 0.08f, ToneGenerator.Wave.Square, 0.45f);
        attackProjectileClip ??= ToneGenerator.Create("SFX_AttackProjectile", 400f, 0.09f, ToneGenerator.Wave.Sine, 0.4f);
        attackDiagonalProjectileClip ??= ToneGenerator.Create("SFX_AttackDiagonal", 420f, 0.09f, ToneGenerator.Wave.Sine, 0.4f);
        attackLaserClip ??= ToneGenerator.Create("SFX_AttackLaser", 900f, 0.22f, ToneGenerator.Wave.Sawtooth, 0.5f);
        attackHomingClip ??= ToneGenerator.Create("SFX_AttackHoming", 500f, 0.1f, ToneGenerator.Wave.Triangle, 0.4f);
        attackSplashClip ??= ToneGenerator.Create("SFX_AttackSplash", 200f, 0.18f, ToneGenerator.Wave.Sawtooth, 0.55f);
        attackPegasusClip ??= ToneGenerator.Create("SFX_AttackPegasus", 700f, 0.08f, ToneGenerator.Wave.Triangle, 0.4f);
        attackDragonClip ??= ToneGenerator.Create("SFX_AttackDragon", 250f, 0.16f, ToneGenerator.Wave.Sawtooth, 0.5f);
        attackCannonClip ??= ToneGenerator.Create("SFX_AttackCannon", 150f, 0.2f, ToneGenerator.Wave.Square, 0.6f);
        attackMeteorClip ??= ToneGenerator.Create("SFX_AttackMeteor", 180f, 0.24f, ToneGenerator.Wave.Sawtooth, 0.6f);
        attackAlchemyClip ??= ToneGenerator.Create("SFX_AttackAlchemy", 550f, 0.1f, ToneGenerator.Wave.Triangle, 0.4f);
    }
}
