// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.Audio;

namespace MysticAxe
{
    public class LoadData : Singleton<LoadData>
    {        
        [Header("Audio")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField, Range(0, 1)] private float defaultMasterVolume = 0.5f; // 50% of the max volume
        [SerializeField, Range(0, 1)] private float defaultMusicVolume = 0.4f; // 40% of the max volume
        [SerializeField, Range(0, 1)] private float defaultSFXVolume = 0.6f; // 60% of the max volume

        public float DefaultMasterVolume { get => defaultMasterVolume; }
        public float DefaultMusicVolume { get => defaultMusicVolume; }
        public float DefaultSFXVolume { get => defaultSFXVolume; }

        // The values are 0.0316 and 3.16 because in db they are -30 db and 10 db,
        // that are the min and max values used for the AudioMixer.
        // The AudioMixer only accepts values in db between -80 db and 20 db but,
        // since -80 and 20 are excessive, -30 and 10 are used instead.
        private const float MIN_VOLUME = 0.0316f;
        private const float MAX_VOLUME = 3.16f;
        
        
        protected override void Awake()
        {
            base.Awake();
            
            LoadResolutionSettings();
            LoadQualitySettings();
            LoadGeneralVolumeSettings();
            LoadMusicVolumeSettings();
            LoadSFXVolumeSettings();
        }

        #region Settings
        public void LoadResolutionSettings()
        {
            int resolutionWidth = PlayerPrefs.GetInt(Utils.RESOLUTION_WIDTH_PREFERENCE_KEY, Screen.currentResolution.width);
            int resolutionHeight = PlayerPrefs.GetInt(Utils.RESOLUTION_HEIGHT_PREFERENCE_KEY, Screen.currentResolution.height);
            Screen.SetResolution(resolutionWidth, resolutionHeight, true);
        }

        public void LoadQualitySettings()
        {
            int currentQualityIndex = PlayerPrefs.GetInt(Utils.GRAPHICS_PREFERENCE_KEY, QualitySettings.GetQualityLevel());
            QualitySettings.SetQualityLevel(currentQualityIndex);
        }

        public void LoadGeneralVolumeSettings()
        {
            float currentGeneralSliderVolume = PlayerPrefs.GetFloat(Utils.AUDIO_MIXER_MASTER_VOLUME_NAME, defaultMasterVolume);
            float currentGeneralVolume = ConvertSliderValueToAudioMixerInteravalValue(currentGeneralSliderVolume);
            float currentGeneralVolumeDB = ConvertLinearValueToDB(currentGeneralVolume);
            
            audioMixer.SetFloat(Utils.AUDIO_MIXER_MASTER_VOLUME_NAME, currentGeneralVolumeDB);
        }

        public void LoadMusicVolumeSettings()
        {
            float currentMusicSliderVolume = PlayerPrefs.GetFloat(Utils.AUDIO_MIXER_MUSIC_VOLUME_NAME, defaultMusicVolume);
            float currentMusicVolume = ConvertSliderValueToAudioMixerInteravalValue(currentMusicSliderVolume);
            float currentMusicVolumeDB = ConvertLinearValueToDB(currentMusicVolume);
            
            audioMixer.SetFloat(Utils.AUDIO_MIXER_MUSIC_VOLUME_NAME, currentMusicVolumeDB);
        }

        public void LoadSFXVolumeSettings()
        {
            float currentSFXSliderVolume = PlayerPrefs.GetFloat(Utils.AUDIO_MIXER_SFX_VOLUME_NAME, defaultSFXVolume);
            float currentSFXVolume = ConvertSliderValueToAudioMixerInteravalValue(currentSFXSliderVolume);
            float currentSFXVolumeDB = ConvertLinearValueToDB(currentSFXVolume);
            
            audioMixer.SetFloat(Utils.AUDIO_MIXER_SFX_VOLUME_NAME, currentSFXVolumeDB);
        }
        #endregion
        
        // Convert the value from a linear scale to a logarithmic one
        private float ConvertLinearValueToDB(float value)
        {
            return 20 * Mathf.Log10(value);
        }

        // The slider value is between 0 and 1, but the AudioMixer accepts values between MIN_VOLUME and MAX_VOLUME
        // This method converts the slider value to a value between MIN_VOLUME and MAX_VOLUME
        private float ConvertSliderValueToAudioMixerInteravalValue(float sliderValue)
        {
            return sliderValue * (MAX_VOLUME - MIN_VOLUME) + MIN_VOLUME;
        }
    }
}