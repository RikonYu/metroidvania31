using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioMaster : MonoBehaviour
{
    public static AudioMaster instance;

    [Header("Runtime Audio Library")]
    public Dictionary<string, AudioClip> audios = new Dictionary<string, AudioClip>();

    [Header("Walk Settings")]
    [SerializeField] private Vector2 walkPitchRange = new Vector2(0.94f, 1.06f);

    [Header("Bullet Settings")]
    [SerializeField] private Vector2 bulletPitchRange = new Vector2(0.95f, 1.08f);
    [SerializeField] private float enemyBulletMinInterval = 0.05f;

    private AudioSource oneShotSource;
    private AudioSource chargingLoopSource;
    private AudioSource walkLoopSource;
    private AudioSource enemyLaserLoopSource;
    private bool isPlayerWalking;
    private float lastEnemyBulletPlayTime = -100f;
    private int lastEnemyBulletPlayFrame = -1;
    private int enemyLaserLoopCount;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        LoadAllAudioClips();
        EnsureAudioSources();
    }

    public void SetPlayerWalking(bool walking)
    {
        if (isPlayerWalking == walking)
        {
            return;
        }

        isPlayerWalking = walking;
        if (walking)
        {
            StartWalkLoop();
        }
        else
        {
            StopWalkLoop();
        }
    }

    public void PlayInteract()
    {
        Play("interact");
    }

    public void PlayFall()
    {
        Play("fall");
    }

    public void PlayBullet()
    {
        PlayWithRandomPitch("bullet", bulletPitchRange);
    }

    public void PlayEnemyBullet()
    {
        if (lastEnemyBulletPlayFrame == Time.frameCount)
        {
            return;
        }

        if (Time.unscaledTime - lastEnemyBulletPlayTime < Mathf.Max(0f, enemyBulletMinInterval))
        {
            return;
        }

        lastEnemyBulletPlayFrame = Time.frameCount;
        lastEnemyBulletPlayTime = Time.unscaledTime;
        Play("enemybullet");
    }

    public void StartEnemyLaserBeam()
    {
        EnsureAudioSources();
        enemyLaserLoopCount++;

        AudioClip laser = GetAudio("laserbeam");
        if (laser == null || enemyLaserLoopSource == null)
        {
            return;
        }

        if (enemyLaserLoopSource.isPlaying && enemyLaserLoopSource.clip == laser)
        {
            return;
        }

        enemyLaserLoopSource.Stop();
        enemyLaserLoopSource.pitch = 1f;
        enemyLaserLoopSource.clip = laser;
        enemyLaserLoopSource.loop = true;
        enemyLaserLoopSource.Play();
    }

    public void StopEnemyLaserBeam()
    {
        if (enemyLaserLoopCount > 0)
        {
            enemyLaserLoopCount--;
        }

        if (enemyLaserLoopCount > 0)
        {
            return;
        }

        enemyLaserLoopCount = 0;
        if (enemyLaserLoopSource != null && enemyLaserLoopSource.isPlaying)
        {
            enemyLaserLoopSource.Stop();
        }
    }

    public void PlayChargeRelease()
    {
        StopChargingLoop();
        Play("chargebullet");
    }

    public void StartChargingLoop()
    {
        EnsureAudioSources();
        AudioClip charging = GetAudio("charging");
        if (charging == null || chargingLoopSource == null)
        {
            return;
        }

        if (chargingLoopSource.isPlaying && chargingLoopSource.clip == charging)
        {
            return;
        }

        chargingLoopSource.Stop();
        chargingLoopSource.pitch = 1f;
        chargingLoopSource.clip = charging;
        chargingLoopSource.loop = true;
        chargingLoopSource.Play();
    }

    public void StopChargingLoop()
    {
        if (chargingLoopSource != null && chargingLoopSource.isPlaying)
        {
            chargingLoopSource.Stop();
        }
    }

    public void PlayDash()
    {
        Play("dash");
    }

    public void PlayHurt()
    {
        Play("hurt");
    }

    private void LoadAllAudioClips()
    {
        audios.Clear();
        LoadFromResourcePath("audios");
        LoadFromResourcePath("Audio");
    }

    private void LoadFromResourcePath(string path)
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>(path);
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            string key = clip.name.ToLowerInvariant();
            if (!audios.ContainsKey(key))
            {
                audios.Add(key, clip);
            }
        }
    }

    private void EnsureAudioSources()
    {
        if (oneShotSource == null)
        {
            oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
            oneShotSource.loop = false;
            oneShotSource.spatialBlend = 0f;
        }

        if (chargingLoopSource == null)
        {
            chargingLoopSource = gameObject.AddComponent<AudioSource>();
            chargingLoopSource.playOnAwake = false;
            chargingLoopSource.loop = true;
            chargingLoopSource.spatialBlend = 0f;
        }

        if (walkLoopSource == null)
        {
            walkLoopSource = gameObject.AddComponent<AudioSource>();
            walkLoopSource.playOnAwake = false;
            walkLoopSource.loop = true;
            walkLoopSource.spatialBlend = 0f;
        }

        if (enemyLaserLoopSource == null)
        {
            enemyLaserLoopSource = gameObject.AddComponent<AudioSource>();
            enemyLaserLoopSource.playOnAwake = false;
            enemyLaserLoopSource.loop = true;
            enemyLaserLoopSource.spatialBlend = 0f;
        }
    }

    private AudioClip GetAudio(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        audios.TryGetValue(key.ToLowerInvariant(), out AudioClip clip);
        return clip;
    }

    private void Play(string key)
    {
        EnsureAudioSources();
        AudioClip clip = GetAudio(key);
        if (clip == null || oneShotSource == null)
        {
            return;
        }

        oneShotSource.pitch = 1f;
        oneShotSource.PlayOneShot(clip);
    }

    private void PlayWithRandomPitch(string key, Vector2 pitchRange)
    {
        EnsureAudioSources();
        AudioClip clip = GetAudio(key);
        if (clip == null || oneShotSource == null)
        {
            return;
        }

        float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
        float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);
        oneShotSource.pitch = Random.Range(minPitch, maxPitch);
        oneShotSource.PlayOneShot(clip);
        oneShotSource.pitch = 1f;
    }

    private void StartWalkLoop()
    {
        EnsureAudioSources();

        AudioClip walk = GetAudio("walk");
        if (walk == null || walkLoopSource == null)
        {
            return;
        }

        if (walkLoopSource.isPlaying && walkLoopSource.clip == walk)
        {
            return;
        }

        float minPitch = Mathf.Min(walkPitchRange.x, walkPitchRange.y);
        float maxPitch = Mathf.Max(walkPitchRange.x, walkPitchRange.y);

        walkLoopSource.Stop();
        walkLoopSource.clip = walk;
        walkLoopSource.pitch = Random.Range(minPitch, maxPitch);
        walkLoopSource.loop = true;
        walkLoopSource.Play();
    }

    private void StopWalkLoop()
    {
        if (walkLoopSource != null && walkLoopSource.isPlaying)
        {
            walkLoopSource.Stop();
        }
    }

    private void OnDisable()
    {
        StopChargingLoop();
        StopWalkLoop();
        if (enemyLaserLoopSource != null && enemyLaserLoopSource.isPlaying)
        {
            enemyLaserLoopSource.Stop();
        }
        enemyLaserLoopCount = 0;
        isPlayerWalking = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
