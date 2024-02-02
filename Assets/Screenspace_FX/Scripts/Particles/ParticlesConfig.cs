using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlesConfig : MonoBehaviour
{
    public float durationMin = 1.0f;
    public float durationMax = 1.0f;
    public float particleSpawnRate = 0.2f;
    public float minStartSpeed = 1.0f;
    public float maxStartSpeed = 1.2f;
    public bool enableGravityModifier = false;
    public float gravityModifier;
    public float minStartSize = 1.0f;
    public float maxStartSize = 1.2f;
    public AnimationCurve sizeOverLifetime;
    public AnimationCurve speedOverLifetime;
    public Gradient colorOverLifetime;
    public Transform particlesTarget;
    public float particlesTargetAttractionForce;

    [Space]
    public MeshRenderer meshRenderer;


    private Material[] mats;
    
    // Start is called before the first frame update
    void Start()
    {
        if (EditorUtils.IsInEditMode())
            mats = meshRenderer.sharedMaterials;
        else 
            mats = meshRenderer.materials;


        UpdateConfig();
    }


    public void UpdateConfig()
    {
        if (EditorUtils.IsInEditMode())
            mats = meshRenderer.sharedMaterials;

        int index_config = SSFX.ParticlesConfigHandler.NewConfig(gravityModifier, enableGravityModifier, colorOverLifetime, sizeOverLifetime, speedOverLifetime, particlesTarget, particlesTargetAttractionForce);
        foreach (var mat in mats)
        {
            mat.SetVector("_ParticleEmissionData", new Vector4(index_config, durationMin, durationMax, particleSpawnRate));
            mat.SetVector("_ParticleEmissionData2", new Vector4(minStartSize, maxStartSize, minStartSpeed, maxStartSpeed));
        }
    }

}
