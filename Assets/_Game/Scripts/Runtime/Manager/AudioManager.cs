using System.Collections.Generic;
using DG.Tweening;
using Gustom.Attributes;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Game.Manager
{
    public enum EAudioLayer
    {
        Music,
        Sound
    }
    
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviourSingleton<AudioManager>
    {
        [Header("Settings")]
        [SerializeField] private float musicFadeInTime = 0.5f;
        [SerializeField] private float musicFadeOutTime = 0.5f;
        
        [AudioParameter(nameof(audioMixer))]
        [SerializeField] private string masterAudioParameter = "masterVolume";
        
        [AudioParameter(nameof(audioMixer))]
        [SerializeField] private string musicAudioParameter = "musicVolume";
        
        [AudioParameter(nameof(audioMixer))]
        [SerializeField] private string soundAudioParameter = "soundVolume";
        
        [Header("References")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup soundMixerGroup;
        [field: SerializeField] public Transform poolTransform { get; private set; }

        private readonly Dictionary<int, AudioEntry> _active = new();
        private readonly Stack<AudioEntry> _pool = new();
        private readonly List<int> _toReclaim = new();
        private int nextID;

        protected override void Awake()
        {
            base.Awake();
            if (!poolTransform) poolTransform = transform;
        }
        
        public float MasterVolume
        {
            get
            {
                bool hasValue = audioMixer.GetFloat(masterAudioParameter, out float volume);
                if (!hasValue)
                {
                    Debug.LogWarning($"{masterAudioParameter} parameter in audio mixer is not exposed.");
                    return 0f;
                }
                return DecibelToLinear(volume);
            }
            set
            {
                float dB = LinearToDecibel(value);
                bool hasValue = audioMixer.SetFloat(masterAudioParameter, dB);
                if (!hasValue)
                {
                    Debug.LogWarning($"{masterAudioParameter} parameter in audio mixer is not exposed.");
                    return;
                }
                Debug.Log($"{masterAudioParameter} changed to {value} ({dB} dB)");
            }
        }

        public float MusicVolume
        {
            get
            {
                bool hasValue = audioMixer.GetFloat(musicAudioParameter, out float volume);
                if (!hasValue)
                {
                    Debug.LogWarning($"{musicAudioParameter} parameter in audio mixer is not exposed.");
                    return 0f;
                }
                return DecibelToLinear(volume);
            }
            set
            {
                float dB = LinearToDecibel(value);
                bool hasValue = audioMixer.SetFloat(musicAudioParameter, dB);
                if (!hasValue)
                {
                    Debug.LogWarning($"{musicAudioParameter} parameter in audio mixer is not exposed.");
                    return;
                }
                Debug.Log($"{musicAudioParameter} changed to {value} ({dB} dB)");
            }
        }
        
        public float SoundVolume
        {
            get
            {
                bool hasValue = audioMixer.GetFloat(soundAudioParameter, out float volume);
                if (!hasValue)
                {
                    Debug.LogWarning($"{soundAudioParameter} parameter in audio mixer is not exposed.");
                    return 0f;
                }
                return DecibelToLinear(volume);
            }
            set
            {
                float dB = LinearToDecibel(value);
                bool hasValue = audioMixer.SetFloat(soundAudioParameter, dB);
                if (!hasValue)
                {
                    Debug.LogWarning($"{soundAudioParameter} parameter in audio mixer is not exposed.");
                    return;
                }
                Debug.Log($"{soundAudioParameter} changed to {value} ({dB} dB)");
            }
        }
        
        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnDestroy();
        }
        
        
        private void Update()
        {
            foreach ((int id, AudioEntry entry) in _active)
            {
                if (entry.IsDone())
                {
                    _toReclaim.Add(id);
                }
            }
 
            foreach (int id in _toReclaim)
            {
                ReturnToPool(id);
            }
            _toReclaim.Clear();
        }
 
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach ((int id, AudioEntry entry) in _active)
            {
                if (!entry.Persist)
                {
                    _toReclaim.Add(id);
                }
            }
 
            foreach (int id in _toReclaim)
            {
                ReturnToPool(id);
            }
            _toReclaim.Clear();
        }
 
        private AudioEntry SpawnEntry()
        {
            GameObject go = new GameObject("Pooled Audio Source");
            go.transform.SetParent(poolTransform, false);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return new AudioEntry(src);
        }
 
        private AudioEntry RentEntry()
        {
            while (_pool.Count > 0)
            {
                AudioEntry entry = _pool.Pop();
                if (entry.HasSource) return entry;
            }
            //Debug.LogWarning("[AudioManager] Pool exhausted — expanding. Consider increasing poolSize in inspector.");
            return SpawnEntry();
        }
 
        public void ReturnToPool(int id)
        {
            if (!_active.TryGetValue(id, out var entry)) return;
            entry.Deactivate();
            _active.Remove(id);
            if (entry.HasSource) _pool.Push(entry);
        }
        
        public AudioHandle Play(AudioConfig cfg)
        {
            if (!cfg.Clip)
            {
                Debug.LogWarning("AudioClip is null - did you forget to assign it in static data?");
                return AudioHandle.Invalid;
            }
 
            // Fall back to inspector-assigned mixer group if caller didn't specify one
            if (!cfg.MixerGroup)
            {
                cfg.MixerGroup = cfg.Layer switch
                {
                    EAudioLayer.Music => musicMixerGroup,
                    EAudioLayer.Sound => soundMixerGroup,
                    _ => null,
                };
            }
 
            AudioEntry entry = RentEntry();
            int id = nextID++;
            entry.Activate(id, cfg);
            _active[id] = entry;
 
            return new AudioHandle(id);
        }

        public AudioHandle PlayMusic(AudioClip music, bool persist = false)
        {
            StopAll(EAudioLayer.Music, musicFadeOutTime);
            return Play(
            AudioConfig.Music(music)
                .WithFadeIn(musicFadeInTime)
                .WithFadeOut(musicFadeOutTime)
                .WithPersist(persist)
            );
        }
        
        public AudioHandle PlaySound(AudioClip sound, float volume = 1f, bool loop = false, Transform follow = null)
        {
            AudioConfig cfg = 
                AudioConfig.Sound(sound)
                .WithVolume(volume)
                .WithLoop(loop);
 
            if (follow) cfg = cfg.WithFollow(follow).With3D();
            return Play(cfg);
        }
 
        public void Stop(AudioHandle handle, float fadeOut = -1f)
        {
            if (TryGet(handle, out AudioEntry entry)) entry.Stop(fadeOut);
        }
 
        public void Pause(AudioHandle handle)
        {
            if (TryGet(handle, out AudioEntry entry)) entry.Pause();
        }
 
        public void Resume(AudioHandle handle)
        {
            if (TryGet(handle, out AudioEntry entry)) entry.Resume();
        }
 
        public void SetVolume(AudioHandle handle, float volume, float fade = 0f)
        {
            if (TryGet(handle, out AudioEntry entry)) entry.SetVolume(volume, fade);
        }
 
        public bool IsPlaying(AudioHandle handle)
        {
            return TryGet(handle, out AudioEntry entry) && entry.IsPlaying;
        }
 
        public void StopAll(EAudioLayer layer, float fadeOut = 0f)
        {
            _toReclaim.Clear();
            foreach ((int id, AudioEntry entry) in _active)
            {
                if (entry.Layer == layer)
                {
                    _toReclaim.Add(id);
                }
            }

            foreach (int id in _toReclaim)
            {
                if (_active.TryGetValue(id, out AudioEntry entry)) entry.Stop(fadeOut);
            }
            _toReclaim.Clear();
        }
 
        public void PauseAll(EAudioLayer layer)
        {
            foreach (AudioEntry entry in _active.Values)
            {
                if (entry.Layer == layer)
                {
                    entry.Pause();
                }
            }
        }
 
        public void ResumeAll(EAudioLayer layer)
        {
            foreach (AudioEntry entry in _active.Values)
            {
                if (entry.Layer == layer)
                {
                    entry.Resume();
                }
            }
        }
 
        private bool TryGet(AudioHandle handle, out AudioEntry entry)
        {
            entry = null;
            return handle.IsValid && _active.TryGetValue(handle.ID, out entry);
        }

        public static float LinearToDecibel(float linear)
        {
            linear = Mathf.Clamp01(linear);
            if (linear <= 0.0001f) return -80f;
            return 20f * Mathf.Log10(linear);
        }
        
        public static float DecibelToLinear(float dB)
        {
            if (dB <= -80f) return 0f;
            return Mathf.Pow(10f, dB / 20f);
        }
    }
    
    public class AudioConfig
    {
        public AudioClip Clip;
        public EAudioLayer Layer;

        public float Volume;
        public float Pitch;
        public float SpatialBlend; // 0 = 2D, 1 = 3D
        public bool Loop;
        public bool Persist;
        public float FadeIn;
        public float FadeOut;
        public Transform FollowTransform;
        public AudioMixerGroup   MixerGroup;

        public static AudioConfig Music(AudioClip clip) => new()
        {
            Clip = clip,
            Layer = EAudioLayer.Music,
            Volume = 1f,
            Pitch = 1f,
            SpatialBlend = 0f,
            Loop = true,
            FadeIn = 1f,
            FadeOut = 1f,
        };

        public static AudioConfig Sound(AudioClip clip) => new()
        {
            Clip = clip,
            Layer = EAudioLayer.Sound,
            Volume = 1f,
            Pitch = 1f,
            SpatialBlend = 0f,
        };

        public AudioConfig WithVolume(float v)
        {
            Volume = Mathf.Clamp01(v); 
            return this;
        }
        
        public AudioConfig WithPitch(float p)
        { 
            Pitch = Mathf.Clamp(p, -3f, 3f); 
            return this; 
        }

        public AudioConfig WithLoop(bool l = true)
        {
            Loop = l; 
            return this;
        }

        public AudioConfig WithPersist(bool p = true)
        {
            Persist = p; 
            return this;
        }

        public AudioConfig WithFadeIn(float s)
        {
            FadeIn = Mathf.Max(0f, s); 
            return this;
        }

        public AudioConfig WithFadeOut(float s)
        {
            FadeOut = Mathf.Max(0f, s); 
            return this;
        }

        public AudioConfig With3D(float blend = 1f)
        {
            SpatialBlend = Mathf.Clamp01(blend);
            return this;
        }

        public AudioConfig WithFollow(Transform t)
        {
            FollowTransform = t; 
            return this;
        }

        public AudioConfig WithMixerGroup(AudioMixerGroup g)
        {
            MixerGroup = g;
            return this;
        }
    }
    
    public class AudioEntry
    {
        public int ID { get; private set; }
        public EAudioLayer Layer { get; private set; }
        public AudioClip Clip { get; private set; }

        public bool IsActive { get; private set; }
        public bool HasSource => source;
        public bool IsPlaying => source && source.isPlaying;
        public bool IsPaused { get; private set; }
        public bool Persist { get; set; }

        public AudioSource Source => source;
        private readonly AudioSource source;

        private float defaultFadeOut;
        private Tween fade;
        private static readonly System.Action<float, AudioSource> setSourceVolume = SetSourceVolume;

        public AudioEntry(AudioSource audioSource)
        {
            source = audioSource;
        }

        public void Activate(int id, AudioConfig cfg)
        {
            ID = id;
            Layer = cfg.Layer;
            Clip = cfg.Clip;
            Persist = cfg.Persist;
            IsActive = true;
            IsPaused = false;
            defaultFadeOut = cfg.FadeOut;

            source.clip = cfg.Clip;
            source.loop = cfg.Loop;
            source.pitch = cfg.Pitch;
            source.spatialBlend = cfg.SpatialBlend;
            source.outputAudioMixerGroup = cfg.MixerGroup;
            source.volume = cfg.FadeIn > 0f ? 0f : cfg.Volume;

            if (cfg.FollowTransform) source.transform.SetParent(cfg.FollowTransform, false);
            source.Play();
            if (cfg.FadeIn > 0f) FadeTo(cfg.Volume, cfg.FadeIn);
        }

        public void Deactivate()
        {
            CancelFade();
            if (source)
            {
                source.Stop();
                source.clip = null;
                source.transform.SetParent(AudioManager.Instance.poolTransform, false);
                source.transform.localPosition = Vector3.zero;
                source.transform.localRotation = Quaternion.identity;
            }

            Clip = null;
            IsActive = false;
            IsPaused = false;
            Persist  = false;
        }

        public void Stop(float fadeOut)
        {
            if (!IsActive) return;
            float duration = fadeOut >= 0f ? fadeOut : defaultFadeOut;
            if (duration > 0f)
            {
                int id = ID;
                FadeTo(0f, duration, () => AudioManager.Instance.ReturnToPool(id));
            }
            else AudioManager.Instance.ReturnToPool(ID);
        }

        public void Pause()
        {
            if (!IsActive || IsPaused) return;
            CancelFade();
            source.Pause();
            IsPaused = true;
        }

        public void Resume()
        {
            if (!IsActive || !IsPaused) return;
            source.UnPause();
            IsPaused = false;
        }

        public void SetVolume(float volume, float fadeSeconds)
        {
            if (!IsActive) return;
            if (fadeSeconds > 0f) FadeTo(Mathf.Clamp01(volume), fadeSeconds);
            else
            {
                CancelFade();
                source.volume = Mathf.Clamp01(volume);
            }
        }

        public bool IsDone() => IsActive && (!source || (!IsPaused && !source.loop && !source.isPlaying));

        private void FadeTo(float to, float duration, System.Action onComplete = null)
        {
            if (!source) return;
            CancelFade();
            if (duration <= 0f)
            {
                source.volume = to;
                onComplete?.Invoke();
                return;
            }

            fade = source.DOFade(to, duration);
            fade.SetEase(Ease.Linear);
            fade.SetUpdate(true);
            fade.SetLink(source.gameObject);
            fade.OnComplete(() =>
            {
                fade = null;
                onComplete?.Invoke();
            });
        }

        private void CancelFade()
        {
            fade?.Kill();
            fade = null;
        }

        private static void SetSourceVolume(float volume, AudioSource audioSource)
        {
            if (audioSource) audioSource.volume = volume;
        }
    }
    
    public readonly struct AudioHandle
    {
        public static readonly AudioHandle Invalid = new(-1);

        public readonly int ID;
        public bool IsValid => ID >= 0;

        internal AudioHandle(int id) => ID = id;

        public void Stop(float fadeOut = -1f) => AudioManager.Instance.Stop(this, fadeOut);
        public void Pause() => AudioManager.Instance.Pause(this);
        public void Resume() => AudioManager.Instance.Resume(this);
        public void SetVolume(float vol, float fade = 0f) => AudioManager.Instance.SetVolume(this, vol, fade);
        public bool IsPlaying() => AudioManager.Instance.IsPlaying(this);
    }
}