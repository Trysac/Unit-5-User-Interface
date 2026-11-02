using TargetToss.Core;
using UnityEngine;

namespace TargetToss.Game
{
    /// <summary>
    /// An orthographic view that keeps the play area on screen at any aspect ratio, with
    /// trauma-based shake and a small kick away from a blast.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        const float HalfWidth = Field.LandHalfWidth + 1.2f;
        const float HalfHeight = 5f;

        Camera view;
        float trauma;
        Vector2 kick;

        /// <summary>Scales every shake (the "screen shake" setting turns it off with 0).</summary>
        public float ShakeScale { get; set; } = 1f;

        public Camera View => view != null ? view : view = GetComponent<Camera>();

        void Awake()
        {
            view = GetComponent<Camera>();
            view.orthographic = true;
        }

        /// <summary>Adds screen shake; 1 is the strongest.</summary>
        public void Shake(float amount) => trauma = Mathf.Clamp01(trauma + amount * ShakeScale);

        /// <summary>Nudges the view a little along <paramref name="direction"/>.</summary>
        public void Kick(Vector2 direction, float amount) => kick += direction.normalized * (amount * ShakeScale);

        public void Calm()
        {
            trauma = 0f;
            kick = Vector2.zero;
        }

        void LateUpdate()
        {
            view.orthographicSize = Mathf.Max(HalfHeight, HalfWidth / Mathf.Max(view.aspect, 0.1f));
            trauma = Mathf.Max(trauma - Time.unscaledDeltaTime * 1.6f, 0f);
            kick = Vector2.Lerp(kick, Vector2.zero, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f));
            float strength = trauma * trauma;
            float t = Time.unscaledTime * 30f;
            var shake = new Vector2(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f) * (0.8f * strength);
            float roll = (Mathf.PerlinNoise(t * 0.6f, 4.2f) - 0.5f) * 3f * strength;
            Vector2 offset = shake + kick;
            transform.SetPositionAndRotation(new Vector3(offset.x, offset.y, -10f), Quaternion.Euler(0f, 0f, roll));
        }
    }
}
