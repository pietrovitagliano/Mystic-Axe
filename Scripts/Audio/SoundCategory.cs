// Author: Pietro Vitagliano

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace MysticAxe
{
    [Serializable]
    public class SoundCategory
    {
        public class AudioSourceWithMetadata
        {
            private AudioSource audioSource;
            private float defaultVolume;

            public AudioSourceWithMetadata(AudioSource audioSource)
            {
                this.audioSource = audioSource;
                defaultVolume = audioSource.volume;
            }

            public AudioSource AudioSource { get => audioSource; set => audioSource = value; }
            public float DefaultVolume { get => defaultVolume; set => defaultVolume = value; }            
        }
        
        [SerializeField] private string name;

        [SerializeField] private AudioClip[] audioClips;

        [SerializeField] private AudioMixerGroup audioMixerGroup;

        [SerializeField, Range(0, 1f)] private float volume = 1;

        [SerializeField, Range(0, 3f)] private float pitch = 1;

        [SerializeField, Range(-1f, 1f)] private float panStereo;

        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1;

        [SerializeField, Range(0f, 1.1f)] private float reverbZoneMix;

        [SerializeField, Range(0f, 100f)] private float minDistance = 3.5f;

        [SerializeField, Range(0f, 500f)] private float maxDistance = 40f;

        [SerializeField] private bool loop = false;

        [SerializeField] private bool ignoreListenerPause = false;

        private readonly List<AudioSourceWithMetadata> audioSourceWithMetadataList = new List<AudioSourceWithMetadata>();
        

        public string Name { get => name; }
        public AudioClip[] AudioClips { get => audioClips; }
        public AudioMixerGroup AudioMixerGroup { get => audioMixerGroup; }
        public float Volume { get => volume; }
        public float Pitch { get => pitch; }
        public float PanStereo { get => panStereo; }
        public float SpatialBlend { get => spatialBlend; }
        public float ReverbZoneMix { get => reverbZoneMix; }
        public float MinDistance { get => minDistance; }
        public float MaxDistance { get => maxDistance; }
        public bool Loop { get => loop; }
        public bool IgnoreListenerPause { get => ignoreListenerPause; }
        public List<AudioSourceWithMetadata> AudioSourceWithMetadataList { get => audioSourceWithMetadataList; }

        
        public bool HasBeenInitialized(GameObject gameObjectSource)
        {
            return audioSourceWithMetadataList.Find(audioSourceWithMetadata => audioSourceWithMetadata.AudioSource != null && audioSourceWithMetadata.AudioSource.gameObject == gameObjectSource) != null;
        }

        public void InitializeAudioSource(GameObject gameObjectSource)
        {
            // When a gameObject is destroyed, the audio source in the list becomes null and
            // if the same gameObject is created again, it will try to access to some audio sources that are null.
            // To avoid this, every time the initialization is executed,
            // the class Sound Category controls if there are any null values in the audioSourcesList and if so, it removes them.
            audioSourceWithMetadataList.RemoveAll(audioSourceWithMetadata => audioSourceWithMetadata.AudioSource == null);
            
            if (!HasBeenInitialized(gameObjectSource))
            {
                AudioSource audioSource = gameObjectSource.AddComponent<AudioSource>();

                audioSource.outputAudioMixerGroup = audioMixerGroup;
                audioSource.volume = volume;
                audioSource.pitch = pitch;
                audioSource.loop = loop;
                audioSource.panStereo = panStereo;
                audioSource.spatialBlend = spatialBlend;
                audioSource.reverbZoneMix = reverbZoneMix;
                audioSource.minDistance = minDistance;
                audioSource.maxDistance = maxDistance;
                audioSource.ignoreListenerPause = ignoreListenerPause;

                audioSourceWithMetadataList.Add(new AudioSourceWithMetadata(audioSource));
            }
        }
        
        public AudioClip GetRandomSound()
        {
            if (audioClips.Length >= 1)
            {
                int index = UnityEngine.Random.Range(0, audioClips.Length);

                return audioClips[index];
            }
            else
            {
                return null;
            }
        }

        public AudioSourceWithMetadata GetAudioSourceWithMetadata(GameObject gameObjectSource)
        {
            return audioSourceWithMetadataList.Find(audioSourceWithMetadata => audioSourceWithMetadata.AudioSource != null && audioSourceWithMetadata.AudioSource.gameObject == gameObjectSource);
        }

        public void Play(GameObject gameObjectSource)
        {
            AudioSourceWithMetadata audioSourceWithMetadata = GetAudioSourceWithMetadata(gameObjectSource);
            AudioSource audioSource = audioSourceWithMetadata.AudioSource;
            
            audioSource.clip = GetRandomSound();
            audioSource.Play();
        }

        public void PlayOneShot(GameObject gameObjectSource)
        {
            AudioSourceWithMetadata audioSourceWithMetadata = GetAudioSourceWithMetadata(gameObjectSource);
            AudioSource audioSource = audioSourceWithMetadata.AudioSource;

            audioSource.clip = GetRandomSound();
            audioSource.PlayOneShot(audioSource.clip);
        }

        public bool IsPlaying(GameObject gameObjectSource)
        {
            AudioSourceWithMetadata audioSourceWithMetadata = GetAudioSourceWithMetadata(gameObjectSource);
            AudioSource audioSource = audioSourceWithMetadata.AudioSource;

            return audioSource.isPlaying;
        }
        
        public bool Is2DSound()
        {
            return spatialBlend == 0;
        }

        public void Stop(GameObject gameObjectSource)
        {
            AudioSourceWithMetadata audioSourceWithMetadata = GetAudioSourceWithMetadata(gameObjectSource);
            AudioSource audioSource = audioSourceWithMetadata.AudioSource;

            audioSource.Stop();
        }

        public void SetVolume(GameObject gameObjectSource, float volume)
        {
            AudioSourceWithMetadata audioSourceWithMetadata = GetAudioSourceWithMetadata(gameObjectSource);
            AudioSource audioSource = audioSourceWithMetadata.AudioSource;

            audioSourceWithMetadata.DefaultVolume = volume;
            audioSource.volume = volume;
        }

        public void SetPitch(GameObject gameObjectSource, float pitch)
        {
            AudioSourceWithMetadata audioSourceWithMetadata = GetAudioSourceWithMetadata(gameObjectSource);
            AudioSource audioSource = audioSourceWithMetadata.AudioSource;

            audioSource.pitch = pitch;
        }
    }
}