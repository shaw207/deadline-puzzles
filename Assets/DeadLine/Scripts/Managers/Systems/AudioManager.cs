using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeadLine
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Serializable]
        public class Sound
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.1f, 3f)] public float pitch = 1f;
            public bool loop = false;
            [Header("Pitch Variation")]
            public bool randomizePitch = false;
            [Range(0f, 1f)] public float pitchVariation = 0.1f;
            [HideInInspector] public AudioSource source;
        }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private int poolSize = 10;

        [Header("Sound Library")]
        [SerializeField] private SoundLibrary[] soundLibraries;
        [SerializeField] private Sound[] sounds; // Mantener para compatibilidad

        [Header("Volume Settings")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

        private Dictionary<string, Sound> soundDictionary;
        private List<AudioSource> audioSourcePool;
        private Queue<AudioSource> availableSources;

        private Sound _currentMusicSound;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // The standalone scenes each carry their original input and sound configuration.
                InitializeSoundManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSoundManager()
        {
            soundDictionary = new Dictionary<string, Sound>(StringComparer.Ordinal);
            
            // Cargar sonidos desde SoundLibraries (ScriptableObjects)
            if (soundLibraries != null)
            {
                foreach (var library in soundLibraries)
                {
                    if (library == null || library.Sounds == null) continue;
                    
                    foreach (var libSound in library.Sounds)
                    {
                        if (libSound == null) continue;
                        if (string.IsNullOrWhiteSpace(libSound.name)) continue;
                        
                        // Convertir SoundLibrary.Sound a SoundManager.Sound
                        var sound = new Sound
                        {
                            name = libSound.name,
                            clip = libSound.clip,
                            volume = libSound.volume,
                            pitch = libSound.pitch,
                            loop = libSound.loop,
                            randomizePitch = libSound.randomizePitch,
                            pitchVariation = libSound.pitchVariation
                        };
                        
                        if (!soundDictionary.ContainsKey(sound.name))
                            soundDictionary.Add(sound.name, sound);
                        else
                            Debug.LogWarning($"[SoundManager] Sonido duplicado encontrado: '{sound.name}' - usando el primero");
                    }
                }
            }
            
            // Cargar sonidos del array tradicional (compatibilidad con versiones antiguas)
            if (sounds != null)
            {
                foreach (var s in sounds)
                {
                    if (s == null) continue;
                    if (string.IsNullOrWhiteSpace(s.name)) continue;
                    if (!soundDictionary.ContainsKey(s.name))
                        soundDictionary.Add(s.name, s);
                }
            }

            audioSourcePool = new List<AudioSource>(Mathf.Max(1, poolSize));
            availableSources = new Queue<AudioSource>(Mathf.Max(1, poolSize));

            int count = Mathf.Max(1, poolSize);
            for (int i = 0; i < count; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                audioSourcePool.Add(src);
                availableSources.Enqueue(src);
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
            }
        }

        public void PlaySound(string soundName)
        {
            if (!soundDictionary.TryGetValue(soundName, out Sound sound))
            {
                Debug.LogWarning("Sound " + soundName + " not found!");
                return;
            }

            if (sound.loop) PlayLoopingSound(soundName);
            else PlayOneShotSound(soundName);
        }

        public void PlayOneShotSound(string soundName)
        {
            if (!soundDictionary.TryGetValue(soundName, out Sound sound))
            {
                Debug.LogWarning("Sound " + soundName + " not found!");
                return;
            }

            if (sound.clip == null) return;

            float pitch = GetPitch(sound);
            float volScale = sound.volume * sfxVolume * masterVolume;

            if (sfxSource != null)
            {
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(sound.clip, volScale);
            }
        }

        public void PlayLoopingSound(string soundName)
        {
            if (!soundDictionary.TryGetValue(soundName, out Sound sound))
            {
                Debug.LogWarning("Sound " + soundName + " not found!");
                return;
            }

            if (sound.clip == null) return;

            if (sound.source != null && sound.source.isPlaying)
            {
                ApplyLoopingSoundVolume(sound);
                return;
            }

            AudioSource source = GetAvailableSource();
            if (source == null)
            {
                Debug.LogWarning("No available audio sources in pool!");
                return;
            }

            sound.source = source;

            source.clip = sound.clip;
            source.loop = true;
            source.playOnAwake = false;

            source.pitch = GetPitch(sound);
            ApplyLoopingSoundVolume(sound);

            source.Play();
        }

        public void PlaySoundAtPoint(string soundName, Vector3 position)
        {
            if (!soundDictionary.TryGetValue(soundName, out Sound sound))
            {
                Debug.LogWarning("Sound " + soundName + " not found!");
                return;
            }

            if (sound.clip == null) return;

            AudioSource.PlayClipAtPoint(sound.clip, position, sound.volume * sfxVolume * masterVolume);
        }

        public void PlayMusic(string soundName, bool fadeIn = false, float fadeDuration = 1f)
        {
            if (!soundDictionary.TryGetValue(soundName, out Sound sound))
            {
                Debug.LogWarning("Music " + soundName + " not found!");
                return;
            }

            if (sound.clip == null) return;

            if (fadeIn)
            {
                StopAllCoroutines();
                StartCoroutine(FadeToMusic(sound, fadeDuration));
                return;
            }

            _currentMusicSound = sound;

            musicSource.clip = sound.clip;
            musicSource.pitch = GetPitch(sound);
            musicSource.loop = true;
            musicSource.volume = sound.volume * musicVolume * masterVolume;
            musicSource.Play();
        }

        public void StopSound(string soundName)
        {
            if (!soundDictionary.TryGetValue(soundName, out Sound sound))
            {
                Debug.LogWarning("Sound " + soundName + " not found!");
                return;
            }

            if (sound.source != null)
            {
                sound.source.Stop();
                ReturnSourceToPool(sound.source);
                sound.source = null;
            }
        }

        public void StopAllSounds()
        {
            if (sounds != null)
            {
                foreach (var s in sounds)
                {
                    if (s == null) continue;
                    if (s.source != null)
                    {
                        s.source.Stop();
                        ReturnSourceToPool(s.source);
                        s.source = null;
                    }
                }
            }

            if (sfxSource != null) sfxSource.Stop();

            RebuildAvailableQueue();
        }

        public void StopMusic(bool fadeOut = false, float fadeDuration = 1f)
        {
            if (!musicSource) return;

            if (!fadeOut)
            {
                musicSource.Stop();
                _currentMusicSound = null;
                return;
            }

            StopAllCoroutines();
            StartCoroutine(FadeOutMusic(fadeDuration));
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateSfxVolumes();
        }

        private void UpdateAllVolumes()
        {
            UpdateMusicVolume();
            UpdateSfxVolumes();
        }

        private void UpdateMusicVolume()
        {
            if (!musicSource) return;
            if (musicSource.clip == null) return;

            float baseVol = (_currentMusicSound != null) ? _currentMusicSound.volume : 1f;
            musicSource.volume = baseVol * musicVolume * masterVolume;
        }

        private void UpdateSfxVolumes()
        {
            if (sounds == null) return;

            foreach (var s in sounds)
            {
                if (s == null) continue;
                if (!s.loop) continue;
                if (s.source == null) continue;
                if (!s.source.isPlaying) continue;

                ApplyLoopingSoundVolume(s);
            }
        }

        private void ApplyLoopingSoundVolume(Sound sound)
        {
            if (sound == null || sound.source == null) return;
            sound.source.volume = sound.volume * sfxVolume * masterVolume;
        }

        private float GetPitch(Sound sound)
        {
            float p = (sound != null) ? sound.pitch : 1f;
            if (sound != null && sound.randomizePitch && sound.pitchVariation > 0f)
            {
                float v = Mathf.Clamp01(sound.pitchVariation);
                p *= UnityEngine.Random.Range(1f - v, 1f + v);
            }
            return Mathf.Clamp(p, 0.1f, 3f);
        }

        private AudioSource GetAvailableSource()
        {
            int guard = availableSources.Count;
            while (guard-- > 0 && availableSources.Count > 0)
            {
                var src = availableSources.Dequeue();
                if (src != null && !src.isPlaying)
                    return src;

                if (src != null)
                    availableSources.Enqueue(src);
            }

            foreach (var src in audioSourcePool)
            {
                if (src != null && !src.isPlaying)
                    return src;
            }

            return null;
        }

        private void ReturnSourceToPool(AudioSource source)
        {
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.loop = false;
            source.pitch = 1f;
            source.volume = 1f;

            if (!availableSources.Contains(source))
                availableSources.Enqueue(source);
        }

        private void RebuildAvailableQueue()
        {
            availableSources.Clear();
            foreach (var src in audioSourcePool)
            {
                if (src != null && !src.isPlaying)
                    availableSources.Enqueue(src);
            }
        }

        private IEnumerator FadeToMusic(Sound newSound, float duration)
        {
            if (!musicSource) yield break;

            float start = musicSource.volume;
            float end = 0f;

            float t = 0f;
            float dur = Mathf.Max(0.0001f, duration);

            while (t < dur)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(start, end, t / dur);
                yield return null;
            }

            musicSource.volume = 0f;

            _currentMusicSound = newSound;
            musicSource.clip = newSound.clip;
            musicSource.pitch = GetPitch(newSound);
            musicSource.loop = true;
            musicSource.Play();

            start = 0f;
            end = newSound.volume * musicVolume * masterVolume;

            t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(start, end, t / dur);
                yield return null;
            }

            musicSource.volume = end;
        }

        private IEnumerator FadeOutMusic(float duration)
        {
            if (!musicSource) yield break;

            float start = musicSource.volume;
            float t = 0f;
            float dur = Mathf.Max(0.0001f, duration);

            while (t < dur)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(start, 0f, t / dur);
                yield return null;
            }

            musicSource.volume = 0f;
            musicSource.Stop();
            _currentMusicSound = null;
        }

        public bool IsSoundPlaying(string soundName)
        {
            if (!soundDictionary.TryGetValue(soundName, out Sound sound))
                return false;

            return sound.source != null && sound.source.isPlaying;
        }

        public bool IsMusicPlaying()
        {
            return musicSource != null && musicSource.isPlaying;
        }

        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSFXVolume() => sfxVolume;
    }
}
