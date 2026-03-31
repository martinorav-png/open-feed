using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PSX
{
    public class Pixelation : VolumeComponent, IPostProcessComponent
    {
        //PIXELATION
        public FloatParameter widthPixelation = new FloatParameter(512);
        public FloatParameter heightPixelation = new FloatParameter(512);
        
        //COLOR PRECISION 
        public FloatParameter colorPrecision = new FloatParameter(32.0f);
        
        public bool IsActive() => active;
        public bool IsTileCompatible() => false;
    }
}