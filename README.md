# Unity_Screenspace_FX

FX system that spawn particles using fragment shaders of meshes. 

Inspired by God Of War technique describe in [*Disintegrating Meshes with Particles in 'God of War'*](https://www.youtube.com/watch?v=ajNSrTprWsg&t=711s)

This system isn't highly optimised. My main goal is rather to test things than make them 100% reliable. Don't consider it as production ready.

### Unity version support
This table will be updated when i'll have time to test versions and based on your feedback.

| Version         | URP     | HDRP |
|--------------|-----------|------------|
| 2022.3 - | ❔      | ❌        |
| 2022.3.10 | ✅      | ❌        |
| 2022.3 +      | ❔  | ❌       |


### Import steps

1. Enable ‘**Allow Unsafe Code**’ in Project Settings > Player > Other Settings
2. Import package
3. In Project Settings > Graphics, set ‘Scriptable Render Pipeline Settings’ asset to URP-SSFX-HighFidelity (or just reproduce the custom pass in it in your own URP renderer asset)
4. Do the same in Project Settings > Quality on the quality level of your choice.
5. Enjoy 🥳

### Advices

#### Easier Lit material setup
If you want to use the `SSFX/Lit Generator` shader, I recommend setting values through `Universal Render Pipeline/Lit`. It will allows you to use the custom editor used by URP lit shaders. Then when it is ready just switch to `SSFX/Lit Generator` and it will keep all the properties you've set.