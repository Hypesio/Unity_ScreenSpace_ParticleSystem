using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ParticlesTarget : MonoBehaviour
{
    public float _AttractionForce = 2.0f;
    void OnEnable() 
    {
        ParticlesTargetHandler.SetTargetState(true);
    }

    void OnDisable()
    {
        ParticlesTargetHandler.SetTargetState(false);
    }

    // Update is called once per frame
    void Update()
    {
        ParticlesTargetHandler.SetTargetPosition(transform.position, _AttractionForce);
    }
}
