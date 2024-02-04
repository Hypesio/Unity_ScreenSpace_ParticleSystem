using System.Collections;
using System.Collections.Generic;
using SSFX;
using UnityEngine;

public class SSFXParticleSystem : MonoBehaviour
{
    [Header("Materials")]
    public float durationEffect;

    [Header("Particles")]
    public float durationMin = 1.0f;
    public float durationMax = 1.0f;
    public float particleSpawnRate = 0.2f;
    public float minStartSpeed = 1.0f;
    public float maxStartSpeed = 1.2f;
    public bool enableGravityModifier = false;
    public float gravityModifier = 0;
    public float minStartSize = 0.01f;
    public float maxStartSize = 0.01f;
    public AnimationCurve sizeOverLifetime;
    public AnimationCurve speedOverLifetime;
    public Gradient colorOverLifetime;
    public Transform particlesTarget;
    public float particlesTargetAttractionForce;

    [Space]
    public MeshRenderer meshRenderer;

    [HideInInspector]
    public bool isPlaying {
        get; private set;
    }
    private Material[] _mats;    
    private float _durationEffect;
    private float _timeCounter;
    private bool _paused = false;
    private int _indexConfig = -1;

    // Start is called before the first frame update
    void Start()
    {
        if (EditorUtils.IsInEditMode())
            _mats = meshRenderer.sharedMaterials;
        else 
            _mats = meshRenderer.materials;


        UpdateConfig();
    }

    void Update()
    {
        if (isPlaying)
            UpdateEffect();
    }

    void UpdateEffect()
    {
        if (_paused)
            return;
        if (_timeCounter >= _durationEffect)
        {
            isPlaying = false;
            _timeCounter = 0;
        }

        _timeCounter += EditorUtils.GetDeltaTime();
        foreach (Material mat in _mats)
        {
            mat.SetFloat("_TimeProgressEffect", _timeCounter);
        }            
    }

    public void UpdateConfig()
    {
        if (EditorUtils.IsInEditMode())
            _mats = meshRenderer.sharedMaterials;
        if (_indexConfig == -1)
            _indexConfig =  SSFXParticleSystemHandler.NewConfig(gravityModifier, enableGravityModifier, colorOverLifetime, sizeOverLifetime, speedOverLifetime, particlesTarget, particlesTargetAttractionForce);
        else 
            SSFXParticleSystemHandler.UpdateConfig(_indexConfig, gravityModifier, enableGravityModifier, colorOverLifetime, sizeOverLifetime, speedOverLifetime, particlesTarget, particlesTargetAttractionForce);
        foreach (var mat in _mats)
        {
            mat.SetVector("_ParticleEmissionData", new Vector4(_indexConfig, durationMin, durationMax, particleSpawnRate));
            mat.SetVector("_ParticleEmissionData2", new Vector4(minStartSize, maxStartSize, minStartSpeed, maxStartSpeed));
        }
    }

    public void StartEffect(float duration = -1)
    {
        if (_indexConfig == -1)
            UpdateConfig();

        _paused = false;
        _durationEffect = duration < 0 ? durationEffect : duration;
        
        #if UNITY_EDITOR
        // Use sharedMaterials in editor to avoid memory leak
        _mats = meshRenderer.sharedMaterials;
        #endif 

        foreach (Material mat in _mats )
        {
            mat.SetFloat("_durationEffect", _durationEffect);
            mat.SetFloat("_PauseParticleSystem", 0);
        }
        SSFXParticleSystemHandler.SetConfigPauseState(_indexConfig, false);
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
        SSFXParticleSystemHandler.SetConfigPauseState(_indexConfig, true);
        _paused = true;
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
        SSFXParticleSystemHandler.SetConfigPauseState(_indexConfig, false);
        _paused = false;
    }
    
    public void ResetEffect() 
    {   
        if (_mats == null)
            return;
        _timeCounter = durationEffect;
        UpdateEffect();
    }

    public float GetEffectProgress()
    {
        return _timeCounter / _durationEffect;
    }


}
