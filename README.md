# Unity_Screenspace_FX

FX system that spawn particles using fragment shaders of meshes. 

Inspired by God Of War technique describe in [*Disintegrating Meshes with Particles in 'God of War'*](https://www.youtube.com/watch?v=ajNSrTprWsg&t=711s)

### Unity version support
This table will be updated when i'll have time to test versions and based on your feedback.

| Version         | URP     | HDRP |
|--------------|-----------|------------|
| 2022.3 - | ❔      | ❌        |
| 2022.3.10 | ✅      | ❌        |
| 2022.3 +      | ❔  | ❌       |


### Import steps

1. Enable ‘**Allow Unsafe Code**’ in Project Settings > Player > Other Settings
2. Download **Editor Coroutines** using Package Manager (*com.unity.editorcoroutines*)
3. Import package
4. In Project Settings > Graphics, set ‘Scriptable Render Pipeline Settings’ asset to URP-SSFX-HighFidelity (or just reproduce the custom pass in it in your own URP renderer asset)
5. Do the same in Project Settings > Quality on the quality level of your choice.
6. Enjoy 🥳