using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LerpTransform : MonoBehaviour
{
    public Transform targetTransform;
    public float durationEffect;

    public Vector3 targetScale;

    public AnimationCurve progressCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 1) });

    public bool playOnAwake;
    [Header("optionnal")]
    public float targetKillRadiusTarget;
    public SSFXParticleSystem particleSystem;

    private Vector3 _startScale;
    private float _timeCounter = 0;
    private bool _effectPlaying = false;
    private float _startTargetKillRadius;

    // Start is called before the first frame update
    void Start()
    {
        if (playOnAwake)
            PlayEffect();



    }

    // Update is called once per frame
    void Update()
    {
        if (_effectPlaying)
        {
            float lerpForce = progressCurve.Evaluate(_timeCounter / durationEffect);
            targetTransform.localScale = Vector3.Lerp(_startScale, targetScale, lerpForce);
            _timeCounter += Time.deltaTime;

            if (particleSystem != null)
            {
                particleSystem.targetKillRadius = Mathf.Lerp(_startTargetKillRadius, targetKillRadiusTarget, lerpForce);
            }

            if (_timeCounter > durationEffect)
            {
                targetTransform.localScale = targetScale;
                _effectPlaying = false;
            }

            if (targetTransform.TryGetComponent<SplineCreator>(out SplineCreator spline))
            {
                spline.isDirty = true;
            }
        }
    }

    public void PlayEffect()
    {
        _startScale = targetTransform.localScale;
        _effectPlaying = true;
        _timeCounter = 0;
        if (particleSystem != null)
            _startTargetKillRadius = particleSystem.targetKillRadius;
    }
}
