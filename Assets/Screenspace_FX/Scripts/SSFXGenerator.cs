using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using Unity.EditorCoroutines.Editor;
[ExecuteInEditMode]
#endif

public class SSFXGenerator : MonoBehaviour
{
    #if UNITY_EDITOR
    public bool DebugTestDisappear;
    public bool DebugResetMat;
    private EditorCoroutine _DebugEditorCoroutine;
    #endif
    public float DurationDisappear;
    public MeshRenderer MeshRenderer; 

    private Material[] mats;
    private float _TimeCounter;
    private float _DurationEffect;
    private Coroutine _routineDisappear;

    void Start()
    {
        if (!EditorUtils.IsInEditMode())
        {
            Material[] mats = MeshRenderer.materials;
            ResetDisappear();
        }
    }

#if UNITY_EDITOR
    // Update is called once per frame
    void Update()
    {
        if (DebugTestDisappear)
        {
            DebugTestDisappear = false;
            if (TryGetComponent<ParticlesConfig>(out ParticlesConfig conf))
            {
                conf.UpdateConfig();
            }
            StartDisappear();
        }

        if (DebugResetMat)
        {
            DebugResetMat = false;
            ResetDisappear();
        }
    }
#endif

    public void StartDisappear(float duration = -1)
    {
        _DurationEffect = duration < 0 ? DurationDisappear : duration;
        
        #if UNITY_EDITOR
        // Use sharedMaterials in editor to avoid memory leak
        mats = MeshRenderer.sharedMaterials;
        #endif 

        foreach (Material mat in mats )
        {
            mat.SetFloat("_DurationDisappear", _DurationEffect);
        }
        
        if (EditorUtils.IsInEditMode())
        {
            #if UNITY_EDITOR
            _DebugEditorCoroutine = EditorCoroutineUtility.StartCoroutine(IFadeProgress(), this);
            #endif
        }
        else 
            _routineDisappear = StartCoroutine(IFadeProgress());
    }
    
    public void ResetDisappear() 
    {   
        _TimeCounter = 0;
        if (mats == null)
            return;
        foreach (Material mat in mats )
        {
            mat.SetFloat("_TimeProgressDisappear", 0.0f);
        }

        if (_routineDisappear != null 
        #if UNITY_EDITOR 
        || _DebugEditorCoroutine != null 
        #endif
        )
        {
            
            if (EditorUtils.IsInEditMode())
            {
                #if UNITY_EDITOR
                EditorCoroutineUtility.StopCoroutine(_DebugEditorCoroutine);
                _DebugEditorCoroutine = null;
                #endif
            }
            else {
                StopCoroutine(_routineDisappear);
                _routineDisappear = null;
            }
            
        }
    }

    IEnumerator IFadeProgress()
    {
        while (_TimeCounter < _DurationEffect)
        {
            _TimeCounter += EditorUtils.GetDeltaTime();
            foreach (Material mat in mats)
            {
                mat.SetFloat("_TimeProgressDisappear", _TimeCounter);
            }
            yield return null;
        }
    }

}
