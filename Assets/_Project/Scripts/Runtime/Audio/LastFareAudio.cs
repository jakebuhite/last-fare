using UnityEngine;

namespace LastFare.Audio
{
    /// <summary>
    /// Placeholder sound design generated in code, so the prototype needs no audio assets:
    /// engine hum, a dread drone, a heartbeat that quickens with tension, stingers and door thumps.
    /// Replace the clips with authored audio later; the public API can stay the same.
    /// </summary>
    public class LastFareAudio : MonoBehaviour
    {
        const int SampleRate = 44100;

        AudioSource engine;
        AudioSource drone;
        AudioSource heart;
        AudioSource oneShots;
        AudioClip stinger;
        AudioClip thud;
        AudioClip thump;

        float tension; // 0..1

        void Awake()
        {
            engine = MakeLoop("Engine", MakeEngineClip(), 0f);
            drone = MakeLoop("Drone", MakeDroneClip(), 0f);
            heart = MakeLoop("Heartbeat", MakeHeartClip(), 0f);

            oneShots = gameObject.AddComponent<AudioSource>();
            oneShots.spatialBlend = 0f;

            stinger = MakeStingerClip();
            thud = MakeThudClip(0.35f, 70f);
            thump = MakeThudClip(0.2f, 110f);
        }

        AudioSource MakeLoop(string name, AudioClip clip, float volume)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.volume = volume;
            src.spatialBlend = 0f;
            src.Play();
            return src;
        }

        /// <summary>Call every frame with the car's speed (m/s).</summary>
        public void SetEngine(float speed, float maxSpeed)
        {
            var s = Mathf.Clamp01(speed / Mathf.Max(maxSpeed, 0.01f));
            engine.pitch = 0.7f + s * 0.9f;
            engine.volume = 0.12f + s * 0.18f;
        }

        /// <summary>0 = calm, 1 = he is on top of you.</summary>
        public void SetTension(float value) => tension = Mathf.Clamp01(value);

        public void PlayStinger(float volume = 1f) => oneShots.PlayOneShot(stinger, volume);
        public void PlayDoor() => oneShots.PlayOneShot(thud, 0.9f);
        public void PlayBump() => oneShots.PlayOneShot(thump, 1f);

        void Update()
        {
            drone.volume = Mathf.Lerp(drone.volume, 0.05f + tension * 0.25f, Time.deltaTime * 2f);
            heart.volume = Mathf.Lerp(heart.volume, tension * 0.6f, Time.deltaTime * 2f);
            heart.pitch = 0.9f + tension * 0.9f;
        }

        // Every loop below uses whole cycles per clip length so it repeats without a click.

        static AudioClip MakeEngineClip()
        {
            const int length = SampleRate;
            var data = new float[length];
            var rng = new System.Random(1);
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                var v = 0.5f * Mathf.Sin(2f * Mathf.PI * 48f * t)
                        + 0.28f * Mathf.Sin(2f * Mathf.PI * 96f * t)
                        + 0.12f * Mathf.Sin(2f * Mathf.PI * 144f * t)
                        + 0.05f * ((float)rng.NextDouble() * 2f - 1f);
                data[i] = v * 0.6f;
            }

            return ToClip("EngineLoop", data);
        }

        static AudioClip MakeDroneClip()
        {
            const int length = SampleRate * 2;
            var data = new float[length];
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                data[i] = 0.5f * (Mathf.Sin(2f * Mathf.PI * 36f * t) + Mathf.Sin(2f * Mathf.PI * 37.5f * t)) * 0.5f
                          + 0.15f * Mathf.Sin(2f * Mathf.PI * 73f * t);
            }

            return ToClip("DroneLoop", data);
        }

        static AudioClip MakeHeartClip()
        {
            const int length = SampleRate;
            var data = new float[length];
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                data[i] = Thump(t, 0f) + 0.7f * Thump(t, 0.28f);
            }

            return ToClip("HeartLoop", data);
        }

        static float Thump(float t, float start)
        {
            var x = t - start;
            if (x < 0f) return 0f;
            return Mathf.Sin(2f * Mathf.PI * 52f * x) * Mathf.Exp(-x * 22f);
        }

        static AudioClip MakeStingerClip()
        {
            var length = (int)(SampleRate * 1.6f);
            var data = new float[length];
            var rng = new System.Random(7);
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 2.4f);
                var noise = (float)rng.NextDouble() * 2f - 1f;
                var shriek = Mathf.Sin(2f * Mathf.PI * (900f + 500f * Mathf.Sin(t * 14f)) * t);
                var boom = Mathf.Sin(2f * Mathf.PI * 45f * t);
                data[i] = Mathf.Clamp((noise * 0.45f + shriek * 0.25f + boom * 0.6f) * env, -1f, 1f);
            }

            return ToClip("Stinger", data);
        }

        static AudioClip MakeThudClip(float seconds, float frequency)
        {
            var length = (int)(SampleRate * seconds);
            var data = new float[length];
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 14f);
            }

            return ToClip("Thud", data);
        }

        static AudioClip ToClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
