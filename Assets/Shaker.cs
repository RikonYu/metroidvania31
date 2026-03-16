using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class Shaker : MonoBehaviour
{
    [System.Serializable]
    public class ShakeProfile
    {
        public float maxAmplitude = 0.2f;
        public float decaySpeed = 2.5f;
        public float noiseFrequency = 24f;
    }

    private class ActiveShake
    {
        public float amplitude;
        public float decaySpeed;
        public float noiseFrequency;
        public float seedX;
        public float seedY;
    }

    public static Shaker instance;

    [Header("Profiles")]
    public ShakeProfile chargeShot = new ShakeProfile { maxAmplitude = 0.16f, decaySpeed = 3.6f, noiseFrequency = 34f };
    public ShakeProfile playerHurt = new ShakeProfile { maxAmplitude = 0.22f, decaySpeed = 5.0f, noiseFrequency = 28f };
    public ShakeProfile bossDefeat = new ShakeProfile { maxAmplitude = 0.58f, decaySpeed = 1.1f, noiseFrequency = 16f };
    public ShakeProfile bossIntro = new ShakeProfile { maxAmplitude = 0.26f, decaySpeed = 0f, noiseFrequency = 20f };
    public ShakeProfile specialDoorOpen = new ShakeProfile { maxAmplitude = 0.2f, decaySpeed = 2.6f, noiseFrequency = 24f };
    public ShakeProfile maxSpeedLanding = new ShakeProfile { maxAmplitude = 0.3f, decaySpeed = 4.2f, noiseFrequency = 22f };

    private readonly List<ActiveShake> activeShakes = new List<ActiveShake>();
    private Transform camTransform;
    private bool bossIntroShakeActive;
    private ActiveShake bossIntroShake;

    private void Awake()
    {
        Transform cameraChild = transform.Find("Main Camera");
        if (cameraChild != null)
        {
            camTransform = cameraChild;
        }
        else if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        instance = this;
    }

    // Backward-compatible simple shake call.
    public void Shake(float amplitude)
    {
        Trigger(customProfile: null, Mathf.Clamp01(amplitude), amplitude, 3.2f, 24f);
    }

    public void ShakeChargeShot(float chargeRatio)
    {
        Trigger(chargeShot, Mathf.Clamp01(chargeRatio));
    }

    public void ShakePlayerHurt(float damage, float maxHealth)
    {
        float normalized = maxHealth > 0f ? damage / maxHealth : 1f;
        float intensity = Mathf.Clamp01(0.45f + normalized * 2.2f);
        Trigger(playerHurt, intensity);
    }

    public void ShakeBossDefeat()
    {
        Trigger(bossDefeat, 1f);
    }

    public void ShakeSpecialDoorOpen()
    {
        Trigger(specialDoorOpen, 1f);
    }

    public void ShakeMaxSpeedLanding(float impactSpeed, float maxFallSpeed)
    {
        float t = maxFallSpeed > 0f ? impactSpeed / maxFallSpeed : 1f;
        Trigger(maxSpeedLanding, Mathf.Clamp01(t));
    }

    public void StartBossIntroShake()
    {
        if (bossIntro == null)
        {
            return;
        }

        bossIntroShakeActive = true;
        bossIntroShake = new ActiveShake
        {
            amplitude = Mathf.Max(0f, bossIntro.maxAmplitude),
            decaySpeed = 0f,
            noiseFrequency = Mathf.Max(1f, bossIntro.noiseFrequency),
            seedX = Random.Range(0f, 1024f),
            seedY = Random.Range(0f, 1024f)
        };
    }

    public void StopBossIntroShake()
    {
        bossIntroShakeActive = false;
    }

    private void LateUpdate()
    {
        if (camTransform == null || (activeShakes.Count == 0 && !bossIntroShakeActive))
        {
            return;
        }

        float time = Time.time;
        Vector2 totalOffset = Vector2.zero;

        for (int i = activeShakes.Count - 1; i >= 0; i--)
        {
            ActiveShake active = activeShakes[i];
            active.amplitude = Mathf.MoveTowards(active.amplitude, 0f, active.decaySpeed * Time.deltaTime);
            if (active.amplitude <= 0.0001f)
            {
                activeShakes.RemoveAt(i);
                continue;
            }

            float nx = Mathf.PerlinNoise(active.seedX, time * active.noiseFrequency) - 0.5f;
            float ny = Mathf.PerlinNoise(time * active.noiseFrequency, active.seedY) - 0.5f;
            Vector2 noise = new Vector2(nx, ny) * 2f;
            totalOffset += noise * active.amplitude;
        }

        if (bossIntroShakeActive)
        {
            float nx = Mathf.PerlinNoise(bossIntroShake.seedX, time * bossIntroShake.noiseFrequency) - 0.5f;
            float ny = Mathf.PerlinNoise(time * bossIntroShake.noiseFrequency, bossIntroShake.seedY) - 0.5f;
            Vector2 noise = new Vector2(nx, ny) * 2f;
            totalOffset += noise * bossIntroShake.amplitude;
        }

        camTransform.position += (Vector3)totalOffset;
    }

    private void Trigger(ShakeProfile customProfile, float intensity)
    {
        if (customProfile == null)
        {
            return;
        }

        intensity = Mathf.Clamp01(intensity);
        if (intensity <= 0.0001f)
        {
            return;
        }

        ActiveShake shake = new ActiveShake
        {
            amplitude = customProfile.maxAmplitude * intensity,
            decaySpeed = customProfile.decaySpeed,
            noiseFrequency = customProfile.noiseFrequency,
            seedX = Random.Range(0f, 1024f),
            seedY = Random.Range(0f, 1024f)
        };

        activeShakes.Add(shake);
    }

    private void Trigger(ShakeProfile customProfile, float intensity, float fallbackAmplitude, float fallbackDecay, float fallbackFrequency)
    {
        if (customProfile != null)
        {
            Trigger(customProfile, intensity);
            return;
        }

        intensity = Mathf.Clamp01(intensity);
        if (intensity <= 0.0001f)
        {
            return;
        }

        ActiveShake shake = new ActiveShake
        {
            amplitude = Mathf.Abs(fallbackAmplitude) * intensity,
            decaySpeed = Mathf.Max(0.01f, fallbackDecay),
            noiseFrequency = Mathf.Max(1f, fallbackFrequency),
            seedX = Random.Range(0f, 1024f),
            seedY = Random.Range(0f, 1024f)
        };

        activeShakes.Add(shake);
    }
}
