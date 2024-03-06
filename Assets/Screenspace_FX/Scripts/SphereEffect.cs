using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SphereEffect : MonoBehaviour
{
    public List<SSFXParticleSystem> particleSystems;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (particleSystems == null)
            particleSystems = new List<SSFXParticleSystem>();

        foreach (SSFXParticleSystem system in particleSystems)
        {
            if (system != null)
            {
                system.spherePosition = transform.position;
                system.sphereRadius = transform.localScale.x;
            }
        }
    }
}
