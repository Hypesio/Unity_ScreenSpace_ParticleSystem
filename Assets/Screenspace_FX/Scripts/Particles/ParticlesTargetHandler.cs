using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using UnityEditor.PackageManager;
using UnityEngine;

public static class ParticlesTargetHandler
{
    private static Vector3 _ParticleTargetPosition;
    private static float _AttractionForce;
    private static bool _IsActive;
    public static void SetTargetState(bool active)
    {
        _IsActive = active;
    }

    public static void SetTargetPosition(Vector3 pos, float attractionForce)
    {
        _ParticleTargetPosition = pos;
        _AttractionForce = attractionForce;
    }

    public static Vector4 GetTarget()
    {
        if (_IsActive)
            return new Vector4(_ParticleTargetPosition.x, _ParticleTargetPosition.y, _ParticleTargetPosition.z, _AttractionForce);
        return Vector4.zero;
    }

}



namespace SSFX {

    enum ParticlesConfigFlags 
    {
        GravityModifier = 1 << 1,
        ColorOverLifetime = 1 << 2,
        AlphaOverLifetime = 1 << 3,
        SizeOverLifetime = 1 << 4,
        Target = 1 << 5,
        SpeedOverLifetime = 1 << 6,
    }

    unsafe public struct ParticlesConfig {
        // used to tell in shader what to use or not
        public int flagsFeature;
        public float gravity;

        // grouped by 4 - xyz = color,w = timestamp
        public fixed float grad_colorOverLifetime[10 * 4];
        // grouped by 2 - x = size, y = timestamp
        public fixed float grad_alphaOverLifetime[10 * 2];
        // grouped by 2 - x = size, y = timestamp
        public fixed float grad_sizeOverLifetime[10 * 2];
        // grouped by 2 - x = size, y = timestamp
        public fixed float grad_speedOverLifetime[10 * 2];

        // xyz - TargetPosition, w - Attraction Force
        public Vector4 targetDatas;
    }

    public static class ParticlesConfigHandler
    {
        private static int MAX_GRADIENT_KEYS = 10;
        private static List<ParticlesConfig> _configs;
        private static List<Transform> _configsTargets;
        private static ComputeBuffer _configsBuffer;

        private static int AddConfig(ParticlesConfig config)
        {
            if (_configs == null)
            {
                _configs = new List<ParticlesConfig>();
            }

            _configs.Add(config);
            return _configs.Count() - 1;
        }
        unsafe private static bool SequenceEqual(float* a, float* b, int size)
        {
            if (a == null && b == null)
                return true;
            if ((a == null && b != null) || (b == null && a != null))
                return false;
            
            for(int i = 0; i < size; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }
        unsafe private static bool ConfigEquality(ParticlesConfig a, ParticlesConfig b) {
            return a.gravity == b.gravity &&
            SequenceEqual(a.grad_colorOverLifetime,b.grad_colorOverLifetime, MAX_GRADIENT_KEYS * 4) &&
            SequenceEqual(a.grad_sizeOverLifetime,b.grad_sizeOverLifetime, MAX_GRADIENT_KEYS * 2) &&
            a.targetDatas == b.targetDatas
            && SequenceEqual(a.grad_speedOverLifetime,b.grad_speedOverLifetime, MAX_GRADIENT_KEYS * 2);
        }

        private static int FindConfig(ParticlesConfig config) {
            if (_configs == null)
                return -1;
            for(int i = 0; i < _configs.Count; i++) {
                ParticlesConfig act = _configs[i];
                if (ConfigEquality(config, act))
                    return i;
            }
            return -1;
        }

        private static void WarningMaxGradKeys(int number) {
            if (number > MAX_GRADIENT_KEYS) {
                Debug.LogWarning("[SSFX] Your gradient will be truncated, it have to much keys. Max is 10");
            }
        }

        // Return the index of the particle config
        // Will create a new config only if it doesn't already exist
        unsafe public static int NewConfig(float gravity, bool gravityModifierEnable, Gradient colorOverLifetime, AnimationCurve sizeOverLifetime, AnimationCurve speedOverLifetime, Transform targetTransform, float attractionForce)
        {
            ParticlesConfig config = new ParticlesConfig{};

            if (gravityModifierEnable) {
                config.gravity = gravity;
                config.flagsFeature &= (int)ParticlesConfigFlags.GravityModifier;
            }
            
            if (targetTransform != null) {
                Vector3 targetPos = targetTransform.position;
                config.targetDatas = new Vector4(targetPos.x, targetPos.y, targetPos.z, attractionForce);
                config.flagsFeature &= (int)ParticlesConfigFlags.Target;
            }
            
            if (colorOverLifetime.alphaKeys.Count() > 0)
            {
                // Color gradient
                WarningMaxGradKeys(colorOverLifetime.colorKeys.Count());
                //config.grad_colorOverLifetime = new float[MAX_GRADIENT_KEYS];
                int index_key = 0;
                foreach(var keys in colorOverLifetime.colorKeys) {
                    if (index_key > MAX_GRADIENT_KEYS)
                        break;
                    config.grad_colorOverLifetime[index_key * 4] = keys.color.r;
                    config.grad_colorOverLifetime[index_key * 4 + 1] = keys.color.g;
                    config.grad_colorOverLifetime[index_key * 4 + 2] = keys.color.b;
                    config.grad_colorOverLifetime[index_key * 4 + 3] = keys.time;

                    index_key ++;
                } 

                config.flagsFeature &= (int)ParticlesConfigFlags.ColorOverLifetime;
            }

            if (colorOverLifetime.alphaKeys.Count() > 0)
            {
                // Alpha gradient
                WarningMaxGradKeys(colorOverLifetime.alphaKeys.Count());
                //config.grad_alphaOverLifetime = new float[MAX_GRADIENT_KEYS];
                int index_key = 0;
                foreach(var keys in colorOverLifetime.alphaKeys) {
                    if (index_key > MAX_GRADIENT_KEYS)
                        break;
                    config.grad_alphaOverLifetime[index_key * 2] = keys.alpha;
                    config.grad_alphaOverLifetime[index_key * 2 + 1] = keys.time;

                    index_key ++;
                } 
                config.flagsFeature &= (int)ParticlesConfigFlags.AlphaOverLifetime;
            }

            if (sizeOverLifetime.keys.Count() > 0)
            {
                // Size gradient
                WarningMaxGradKeys(sizeOverLifetime.keys.Count());
                //config.grad_sizeOverLifetime = new float[MAX_GRADIENT_KEYS];
                int index_key = 0;
                foreach(var keys in sizeOverLifetime.keys) {
                    if (index_key > MAX_GRADIENT_KEYS)
                        break;
                    config.grad_sizeOverLifetime[index_key * 2] = keys.value;
                    config.grad_sizeOverLifetime[index_key * 2 + 1] = keys.time;

                    index_key ++;
                } 
                config.flagsFeature &= (int)ParticlesConfigFlags.SizeOverLifetime;
            }

            if (speedOverLifetime.keys.Count() > 0)
            {
                // Size gradient
                WarningMaxGradKeys(speedOverLifetime.keys.Count());
                //config.grad_speedOverLifetime = new float[MAX_GRADIENT_KEYS];
                int index_key = 0;
                foreach(var keys in speedOverLifetime.keys) {
                    if (index_key > MAX_GRADIENT_KEYS)
                        break;
                    config.grad_speedOverLifetime[index_key * 2] = keys.value;
                    config.grad_speedOverLifetime[index_key * 2 + 1] = keys.time;

                    index_key ++;
                } 
                config.flagsFeature &= (int)ParticlesConfigFlags.SpeedOverLifetime;
            }

            int index_config = FindConfig(config);
            if (index_config == -1) {
                index_config = AddConfig(config);
            }

            {
                if (_configsTargets == null)
                    _configsTargets = new List<Transform>();
                    
                if (index_config >= _configsTargets.Count())
                    _configsTargets.Add(targetTransform);
                else 
                    _configsTargets[index_config] = targetTransform;
            }

            return index_config;
        }

        // This should be done only once per game session
        public static ComputeBuffer CreateConfigsComputeBuffer()
        {
            if (_configsBuffer != null)
            {
                _configsBuffer.Release();
            }
            if (_configs != null)
            {
                _configsBuffer = new ComputeBuffer(_configs.Count(), Marshal.SizeOf(typeof(ParticlesConfig)), ComputeBufferType.Structured);
                Debug.Log("Size of struct: " + Marshal.SizeOf(typeof(ParticlesConfig)));
                return _configsBuffer;
            }
            return null;
        }

        // This should be done as rarely as possible 
        public static ComputeBuffer UpdateConfigsComputeBuffer()
        {
            if (_configsBuffer == null)
                if (CreateConfigsComputeBuffer() == null)
                    return null;

            
            
            for (int i = 0; i < _configs.Count(); i++)
            {
                if (_configsTargets != null && _configsTargets[i] != null) 
                {
                    Vector3 tarPos = _configsTargets[i].position;
                    ParticlesConfig conf = _configs[i];
                    conf.targetDatas = new Vector4(tarPos.x, tarPos.y, tarPos.z, _configs[i].targetDatas.w);
                    _configs[i] = conf;
                }
            }
            
            _configsBuffer.SetData(_configs);

            return _configsBuffer;
        }

    }

}