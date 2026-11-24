using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeadLine
{
    // Local review lighting for the two water mechanisms. The optical room keeps its level profile.
    public sealed class OriginalWaterRoomLighting : MonoBehaviour
    {
        public Transform sceneCamera;
        public MeshRenderer[] chamberWaterSurfaces;
        public Material waterSurfaceHighlight;
        private Material originalWaterMaterial;
        private Volume exposure;
        private VolumeProfile adjustments;

        private void Awake()
        {
            // All three original moving discs share a grey trigger material with other level objects.
            // Only these three discs use the local emissive variant during play.
            var waterAppearance = new MaterialPropertyBlock();
            foreach (var surface in chamberWaterSurfaces ?? System.Array.Empty<MeshRenderer>())
            {
                if (!surface) continue;
                if (waterSurfaceHighlight)
                {
                    originalWaterMaterial = surface.sharedMaterial;
                    surface.sharedMaterial = waterSurfaceHighlight;
                }
                surface.GetPropertyBlock(waterAppearance);
                waterAppearance.SetColor("_BaseColor", new Color(.025f,1.76f,1.64f,.98f));
                waterAppearance.SetFloat("_Smoothness",.77f);
                surface.SetPropertyBlock(waterAppearance);
            }
            adjustments = ScriptableObject.CreateInstance<VolumeProfile>();
            var grading = adjustments.Add<ColorAdjustments>(false);
            grading.postExposure.Override(1.58f);
            grading.contrast.Override(13f);
            // Keep the original main-light shafts but avoid a bright veil from the new work lamps.
            var fog = adjustments.Add<VolumetricFogVolumeComponent>(false);
            fog.enableAdditionalLightsContribution.Override(false);
            var edges = adjustments.Add<Vignette>(false);
            edges.intensity.Override(.16f);
            exposure = gameObject.AddComponent<Volume>();
            exposure.isGlobal = true;
            exposure.priority = 100;
            exposure.sharedProfile = adjustments;
            UpdateWeight();
        }

        private void LateUpdate() => UpdateWeight();

        private void UpdateWeight()
        {
            if (!sceneCamera) return;
            float x = sceneCamera.position.x;
            exposure.weight = Mathf.Max(RoomWeight(x,-24f,14f),RoomWeight(x,109f,141f));
        }

        static float RoomWeight(float x,float first,float last)
        {
            return Mathf.Min(Mathf.InverseLerp(first-4f,first,x),Mathf.InverseLerp(last+4f,last,x));
        }

        private void OnDestroy()
        {
            if (originalWaterMaterial)
                foreach (var surface in chamberWaterSurfaces ?? System.Array.Empty<MeshRenderer>())
                    if (surface && surface.sharedMaterial == waterSurfaceHighlight) surface.sharedMaterial = originalWaterMaterial;
            if (!adjustments) return;
            foreach (var component in adjustments.components) if (component) Destroy(component);
            Destroy(adjustments);
        }
    }
}
