using System.Collections;
using System.Collections.Generic;
using SSFX;
using UnityEngine;
using UnityEditor.PackageManager.Requests;
using UnityEditor;
using UnityEngine.Rendering;

[ExecuteAlways]
public class SSFXParticleSystem : MonoBehaviour
{
    public enum StartSpeedType
    {
        Normal = 0,
        FromMeshCenter = 1,
        ToMeshCenter = 2,
        StartDirection = 3,
        None = 3,
    }

    public Renderer[] renderers;
    [Space]


    //[Header("Materials")]
    public float durationEffect;

    //[Header("Particles")]
    public float durationMin = 1.0f;
    public float durationMax = 1.0f;
    public float particleSpawnRate = 0.2f;
    public StartSpeedType startSpeedType = StartSpeedType.FromMeshCenter;
    [Tooltip("Taken into account only if startSpeedType is set to StartDirection")]
    public Vector3 startDirection;
    public float startSpeedMin = 1.0f;
    public float startSpeedMax = 1.2f;
    public bool enableGravityModifier = false;
    public float gravityModifier = 0;
    public float startSizeMin = 0.01f;
    public float startSizeMax = 0.01f;
    public bool enableSizeOverLifetime = false;
    public AnimationCurve sizeOverLifetime;
    public bool enableSpeedOverLifetime = false;
    public AnimationCurve speedOverLifetime;
    public bool enableColorOverLifetime = false;
    public Gradient colorOverLifetime;
    public bool enableTarget = false;
    public Transform particlesTarget;
    [Tooltip("Max Value is 100.0f")]
    public float particlesTargetAttractionForce;
    public bool particleDieWhenReachingTarget = true;
    [Tooltip("Max Value is 20.0f")]
    public float targetKillRadius = 0.2f;

    [HideInInspector]
    public bool isPlaying
    {
        get; private set;
    }
    private List<Material> _mats;
    private float _durationEffect;
    private float _timeCounter;
    private int _indexConfig = 0;
    private bool _reseted = false;

    // Start is called before the first frame update
    void Start()
    {
        SetMats();
        UpdateConfig();
    }

    void SetMats()
    {
        _mats = new List<Material>();
        if (renderers == null)
            return;
        foreach (Renderer rend in renderers)
        {
            if (EditorUtils.IsInEditMode())
                _mats.AddRange(rend.sharedMaterials);
            else
                _mats.AddRange(rend.materials);
        }
    }

    void Update()
    {
        if (isPlaying)
        {
            UpdateEffect();
        }
    }

    public void UpdateEffect()
    {
        if (_timeCounter >= (_durationEffect + durationMax))
        {
            isPlaying = false;
            //_timeCounter = 0;
        }

        _timeCounter += EditorUtils.GetDeltaTime();

        foreach (Material mat in _mats)
        {
            mat.SetFloat("_TimeProgressEffect", _timeCounter);
        }
    }

    unsafe
    public SSFXParticleConfig BuildConfig()
    {
        SSFXParticleConfig config = new SSFXParticleConfig { };

        if (enableGravityModifier)
        {
            config.gravity = gravityModifier;
            config.flagsFeature |= (int)SSFXParticleSystemFlags.GravityModifier;
        }

        if (enableTarget && particlesTarget != null)
        {
            float datas4 = SSFXParticleSystemHandler.Pack2Float(particlesTargetAttractionForce, 100.0f, targetKillRadius, 20.0f);
            if (particleDieWhenReachingTarget)
            {
                config.flagsFeature |= (int)SSFXParticleSystemFlags.TargetDieOnReach;
            }

            Vector3 targetPos = particlesTarget.position;
            config.targetDatas = new Vector4(targetPos.x, targetPos.y, targetPos.z, datas4);

            config.flagsFeature |= (int)SSFXParticleSystemFlags.Target;
        }

        SSFXParticleSystemHandler.SetArrayFromColorGradient(colorOverLifetime, config.grad_colorOverLifetime, config.grad_alphaOverLifetime);
        if (enableColorOverLifetime)
            config.flagsFeature |= (int)SSFXParticleSystemFlags.ColorOverLifetime;
        if (enableColorOverLifetime)
            config.flagsFeature |= (int)SSFXParticleSystemFlags.AlphaOverLifetime;

        SSFXParticleSystemHandler.SetArrayFromAnimationCurve(sizeOverLifetime, config.grad_sizeOverLifetime);
        if (enableSizeOverLifetime)
            config.flagsFeature |= (int)SSFXParticleSystemFlags.SizeOverLifetime;

        SSFXParticleSystemHandler.SetArrayFromAnimationCurve(speedOverLifetime, config.grad_speedOverLifetime);
        if (enableSpeedOverLifetime)
            config.flagsFeature |= (int)SSFXParticleSystemFlags.SpeedOverLifetime;

        return config;
    }

    public void UpdateConfig()
    {
        if (EditorUtils.IsInEditMode())
            SetMats();

        SSFXParticleConfig config = BuildConfig();

        if (_indexConfig == 0)
            _indexConfig = SSFXParticleSystemHandler.NewConfig(config, particlesTarget);
        else
            SSFXParticleSystemHandler.UpdateConfig(_indexConfig, config, particlesTarget);

        foreach (var mat in _mats)
        {
            mat.SetVector("_ParticleEmissionData", new Vector4(_indexConfig, durationMin, durationMax, particleSpawnRate));
            mat.SetVector("_ParticleEmissionData2", new Vector4(startSizeMin, startSizeMax, startSpeedMin, startSpeedMax));
            mat.SetVector("_ParticleEmissionData3", new Vector4((float)startSpeedType, startDirection.x, startDirection.y, startDirection.z));
        }
    }

    public void StartEffect(float duration = -1)
    {
        if (_indexConfig == 0 || _reseted)
        {
            _reseted = false;
            UpdateConfig();
        }

        _durationEffect = duration < 0 ? durationEffect : duration;

        // Use sharedMaterials in editor to avoid memory leak
        if (EditorUtils.IsInEditMode())
            SetMats();

        foreach (Material mat in _mats)
        {
            mat.SetFloat("_durationEffect", _durationEffect);
            mat.SetFloat("_PauseParticleSystem", 0);
        }
        isPlaying = true;
    }

    public void PauseEffect()
    {
        if (!isPlaying)
            return;

        foreach (var mat in _mats)
        {
            mat.SetFloat("_PauseParticleSystem", 1);
        }
    }

    public void PlayEffect()
    {
        if (!isPlaying)
        {
            StartEffect();
            return;
        }

        foreach (var mat in _mats)
        {
            mat.SetFloat("_PauseParticleSystem", 0);
        }
    }

    public void ResetEffect()
    {
        if (_mats == null)
            return;
        _reseted = true;
        _timeCounter = 0;
        SSFXParticleSystemHandler.ClearConfigParticles(_indexConfig);
        UpdateEffect();
        isPlaying = false;
    }

    public float GetEffectProgress()
    {
        return _timeCounter / (_durationEffect + durationMax);
    }
}
