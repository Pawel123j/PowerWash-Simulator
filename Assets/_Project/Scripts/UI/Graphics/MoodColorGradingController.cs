using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Zmywak.Core.Services;
using Zmywak.Meta.Progress;

namespace Zmywak.UI.Graphics
{
    /// <summary>
    /// "Color as emotion": drifts the global color grade cold as Homesickness rises and warm as
    /// Integration rises, per the design doc. Uses URP's Volume/ColorAdjustments directly (hue shift +
    /// saturation) rather than a baked LUT texture - same idea, no texture asset required.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class MoodColorGradingController : MonoBehaviour
    {
        [SerializeField] private float maxHueShiftCold = -15f; // drifts towards blue
        [SerializeField] private float maxHueShiftWarm = 10f; // drifts towards orange
        [SerializeField] private float maxSaturationDrop = -20f; // homesickness desaturates

        private Volume _volume;
        private ColorAdjustments _colorAdjustments;
        private MetaProgressController _progress;

        private void Awake()
        {
            _volume = GetComponent<Volume>();
        }

        private void OnEnable()
        {
            if (_volume.profile != null && !_volume.profile.TryGet(out _colorAdjustments))
            {
                _colorAdjustments = _volume.profile.Add<ColorAdjustments>(true);
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.hueShift.overrideState = true;
                _colorAdjustments.saturation.overrideState = true;
            }

            ServiceLocator.TryGet(out _progress);
            if (_progress != null)
            {
                _progress.OnProgressChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_progress != null)
            {
                _progress.OnProgressChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            if (_colorAdjustments == null || _progress == null)
            {
                return;
            }

            float homesickness01 = Mathf.Clamp01(_progress.Stats.Homesickness / 100f);
            float integration01 = Mathf.Clamp01(_progress.Stats.Integration / 100f);
            float mood = integration01 - homesickness01; // -1 (cold/lonely) .. +1 (warm/settled)

            float hueShift = mood >= 0f
                ? Mathf.Lerp(0f, maxHueShiftWarm, mood)
                : Mathf.Lerp(0f, maxHueShiftCold, -mood);

            _colorAdjustments.hueShift.value = hueShift;
            _colorAdjustments.saturation.value = Mathf.Lerp(0f, maxSaturationDrop, homesickness01);
        }
    }
}
