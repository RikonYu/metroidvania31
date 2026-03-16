using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioMaster : MonoBehaviour
{
    public static AudioMaster instance;

    [Header("Runtime Audio Library")]
    public Dictionary<string, AudioClip> audios = new Dictionary<string, AudioClip>();

    [Header("Walk Settings")]
    [SerializeField] private float walkInterval = 0.3f;
    [SerializeField] private Vector2 walkPitchRange = new Vector2(0.94f, 1.06f);

    [Header("Bullet Settings")]
    [SerializeField] private Vector2 bulletPitchRange = new Vector2(0.95f, 1.08f);

    private AudioSource oneShotSource;
    private AudioSource chargingLoopSource;
    private bool isPlayerWalking;
    private float walkTimer;

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

    void Update()
    {
        if (!isPlayerWalking)
        {
            return;
        }

        walkTimer -= Time.deltaTime;
        if (walkTimer > 0f)
        {
            return;
        }

        PlayWithRandomPitch("walk", walkPitchRange);
        walkTimer = Mathf.Max(0.05f, walkInterval);
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
            walkTimer = 0f;
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

    private void OnDisable()
    {
        StopChargingLoop();
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
