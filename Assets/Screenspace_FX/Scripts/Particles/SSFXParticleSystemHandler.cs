using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Rendering;
using UnityEngine;

namespace SSFX {

    // If values are added here, changes should be copied in ParticlesCommons.cginc
    enum SSFXParticleSystemFlags 
    {
        GravityModifier = 1 << 1,
        ColorOverLifetime = 1 << 2,
        AlphaOverLifetime = 1 << 3,
        SizeOverLifetime = 1 << 4,
        Target = 1 << 5,
        SpeedOverLifetime = 1 << 6,
        KillAll = 1 << 7,
        TargetDieOnReach = 1 << 8,
    }

    unsafe public struct SSFXParticleConfig {
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

    public static class SSFXParticleSystemHandler
    {
        private static int MAX_GRADIENT_KEYS = 10;
        private static int INVALID_FLOAT = 65536;
        private static List<SSFXParticleConfig> _configs = null;
        private static List<Transform> _configsTargets = null;
        private static ComputeBuffer _configsBuffer = null;

        // Used to know when _configs data need to be updated on GPU
        // Set to true on _configs changes
        private static bool _dirtyConfigs = false;

        private static int AddConfig(SSFXParticleConfig config)
        {
            if (_configs == null)
            {
                _configs = new List<SSFXParticleConfig>();
                _configs.Add(new SSFXParticleConfig{});

                _configsTargets = new List<Transform>();
                _configsTargets.Add(null);
            }

            _configs.Add(config);
            _configsTargets.Add(null);
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
        unsafe private static bool ConfigEquality(SSFXParticleConfig a, SSFXParticleConfig b) {
            return a.gravity == b.gravity &&
            SequenceEqual(a.grad_colorOverLifetime,b.grad_colorOverLifetime, MAX_GRADIENT_KEYS * 4) &&
            SequenceEqual(a.grad_sizeOverLifetime,b.grad_sizeOverLifetime, MAX_GRADIENT_KEYS * 2) &&
            a.targetDatas == b.targetDatas
            && SequenceEqual(a.grad_speedOverLifetime,b.grad_speedOverLifetime, MAX_GRADIENT_KEYS * 2);
        }

        private static int FindConfig(SSFXParticleConfig config) {
            if (_configs == null)
                return -1;
            for(int i = 0; i < _configs.Count; i++) {
                SSFXParticleConfig act = _configs[i];
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

        public static void ClearConfigParticles(int configID)
        {
            if (configID >= _configs.Count() && configID >= 0)
            {
                Debug.LogWarning("[SSFX] You are trying to clear an out of bound config");
                return;
            }

            SSFXParticleConfig conf = _configs[configID];

            conf.flagsFeature |= (int)SSFXParticleSystemFlags.KillAll;
            
            _configs[configID] = conf;
            _dirtyConfigs = true;
        }


        unsafe public static void UpdateConfig(int configID, float gravity, bool gravityModifierEnable, Gradient colorOverLifetime, AnimationCurve sizeOverLifetime, AnimationCurve speedOverLifetime, Transform targetTransform, float attractionForce, bool dieOnReach)
        {
            SSFXParticleConfig config = new SSFXParticleConfig{};

            if (gravityModifierEnable) {
                config.gravity = gravity;
                config.flagsFeature &= (int)SSFXParticleSystemFlags.GravityModifier;
            }
            

            // Target 
            if (targetTransform != null) {
                Vector3 targetPos = targetTransform.position;
                config.targetDatas = new Vector4(targetPos.x, targetPos.y, targetPos.z, attractionForce);
                
                config.flagsFeature |= (int)SSFXParticleSystemFlags.Target;
                _configsTargets[configID] = targetTransform;
                if (dieOnReach)
                    config.flagsFeature |= (int)SSFXParticleSystemFlags.TargetDieOnReach;
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
                for (int i = index_key; i < MAX_GRADIENT_KEYS; i++)
                {
                    config.grad_colorOverLifetime[index_key * 4 + 3] = INVALID_FLOAT;
                }

                config.flagsFeature |= (int)SSFXParticleSystemFlags.ColorOverLifetime;
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
                for (int i = index_key; i < MAX_GRADIENT_KEYS; i++)
                {
                    config.grad_alphaOverLifetime[index_key * 2 + 1] = INVALID_FLOAT;
                }

                config.flagsFeature |= (int)SSFXParticleSystemFlags.AlphaOverLifetime;
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
                for (int i = index_key; i < MAX_GRADIENT_KEYS; i++)
                {
                    config.grad_sizeOverLifetime[index_key * 2 + 1] = INVALID_FLOAT;
                }
                config.flagsFeature |= (int)SSFXParticleSystemFlags.SizeOverLifetime;
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
                for (int i = index_key; i < MAX_GRADIENT_KEYS; i++)
                {
                    config.grad_speedOverLifetime[index_key * 2 + 1] = INVALID_FLOAT;
                }
                config.flagsFeature |= (int)SSFXParticleSystemFlags.SpeedOverLifetime;
            }

            _configs[configID] = config;
            _dirtyConfigs = true;
        }

        // Return the index of the particle config
        // Will create a new config only if it doesn't already exist
        unsafe public static int NewConfig(float gravity, bool gravityModifierEnable, Gradient colorOverLifetime, AnimationCurve sizeOverLifetime, AnimationCurve speedOverLifetime, Transform targetTransform, float attractionForce, bool dieOnReach)
        {
            SSFXParticleConfig config = new SSFXParticleConfig{};
            int index_config = AddConfig(config);
            UpdateConfig(index_config, gravity, gravityModifierEnable, colorOverLifetime, sizeOverLifetime, speedOverLifetime, targetTransform, attractionForce, dieOnReach);

            return index_config;
        }

        // Create or update size of compute buffer if needed
        public static ComputeBuffer CreateConfigsComputeBuffer()
        {
            if (_configsBuffer != null && _configs != null && _configs.Count >= _configsBuffer.count)
                _configsBuffer.Release();
            else if (_configsBuffer != null && (_configs == null || _configs.Count < _configsBuffer.count))
                return _configsBuffer;

            if (_configs != null)
                _configsBuffer = new ComputeBuffer(_configs.Count() * 2, Marshal.SizeOf(typeof(SSFXParticleConfig)), ComputeBufferType.Structured);
            else 
                _configsBuffer = new ComputeBuffer(10, Marshal.SizeOf(typeof(SSFXParticleConfig)), ComputeBufferType.Structured);

            return _configsBuffer;
        }

        // This should be done as rarely as possible 
        public static ComputeBuffer UpdateConfigsComputeBuffer()
        {
            _configsBuffer = CreateConfigsComputeBuffer();
                
            if (_configs == null)
                return _configsBuffer;
            
            // Update target position if needed
            for (int i = 1; i < _configs.Count(); i++)
            {
                if (_configsTargets != null && _configsTargets[i] != null) 
                {
                    Vector3 tarPos = _configsTargets[i].position;
                    SSFXParticleConfig conf = _configs[i];
                    if (tarPos != (Vector3)conf.targetDatas)
                    {
                        _dirtyConfigs = true;
                        conf.targetDatas = new Vector4(tarPos.x, tarPos.y, tarPos.z, _configs[i].targetDatas.w);
                        _configs[i] = conf;
                    }
                }
            }

            if(_dirtyConfigs)
            {
                _configsBuffer.SetData(_configs);
                _dirtyConfigs = false;
            }

            return _configsBuffer;
        }

    }

}