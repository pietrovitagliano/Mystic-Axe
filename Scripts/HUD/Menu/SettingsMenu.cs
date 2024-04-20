// Author: Pietro Vitagliano

using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MysticAxe
{
    public class SettingsMenu : Singleton<SettingsMenu>
    {
        [Header("Resolution")]
        [SerializeField] private TMP_Dropdown resolutionDropDown;
        private Resolution[] resolutionArray;

        [Header("Quality")]
        [SerializeField] private TMP_Dropdown graphicsDropDown;

        [Header("Volume")]
        [SerializeField] private Slider generalVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        
        private void Start()
        {
            InitializeResolutionDropDown();
            InitializeGraphicsDropDown();
            InitializeVolumeSliders();
        }

        #region Settings Menu Initialization
        private void InitializeResolutionDropDown()
        {
            // Clear all the dropdown's options
            resolutionDropDown.ClearOptions();

            // Get the resolutions supported by the monitor.
            // They are from the lowest to the highest, thus the order has to be reversed.
            resolutionArray = Screen.resolutions.Reverse().ToArray();
            
            for (int i = 0; i < resolutionArray.Length; i++)
            {
                string resolutionString = resolutionArray[i].width + " x " + resolutionArray[i].height;
                resolutionDropDown.options.Add(new TMP_Dropdown.OptionData(resolutionString));

                if (resolutionArray[i].width == Screen.currentResolution.width &&
                    resolutionArray[i].height == Screen.currentResolution.height)
                {
                    // Set the dropdown to the current resolution index
                    resolutionDropDown.value = i;
                }
            }

            resolutionDropDown.RefreshShownValue();
        }

        private void InitializeGraphicsDropDown()
        {
            // Clear all the dropdown's options
            graphicsDropDown.ClearOptions();

            // Add the graphics quality names to the dropdown
            QualitySettings.names.ToList().ForEach(optionName => graphicsDropDown.options.Add(new TMP_Dropdown.OptionData(optionName)));

            // Set the drop down to the index of the current quality level
            graphicsDropDown.value = QualitySettings.GetQualityLevel();
        }

        private void InitializeVolumeSliders()
        {
            float currentGeneralSliderVolume = PlayerPrefs.GetFloat(Utils.AUDIO_MIXER_MASTER_VOLUME_NAME, LoadData.Instance.DefaultMasterVolume);
            generalVolumeSlider.value = currentGeneralSliderVolume;
            
            float currentMusicSliderVolume = PlayerPrefs.GetFloat(Utils.AUDIO_MIXER_MUSIC_VOLUME_NAME, LoadData.Instance.DefaultMusicVolume);
            musicVolumeSlider.value = currentMusicSliderVolume;
            
            float currentSFXSliderVolume = PlayerPrefs.GetFloat(Utils.AUDIO_MIXER_SFX_VOLUME_NAME, LoadData.Instance.DefaultSFXVolume);
            sfxVolumeSlider.value = currentSFXSliderVolume;
        }
        #endregion

        #region Change Settings Events
        public void SetResolution(int index)
        {
            Resolution resolution = resolutionArray[index];

            // Save the resolution settings into the PlayerPrefs
            PlayerPrefs.SetInt(Utils.RESOLUTION_WIDTH_PREFERENCE_KEY, resolution.width);
            PlayerPrefs.SetInt(Utils.RESOLUTION_HEIGHT_PREFERENCE_KEY, resolution.height);

            // Set the resolution
            LoadData.Instance.LoadResolutionSettings();
        }

        public void SetGraphics(int index)
        {
            // Save the graphics settings into the PlayerPrefs
            PlayerPrefs.SetInt(Utils.GRAPHICS_PREFERENCE_KEY, index);

            // Set the graphics
            LoadData.Instance.LoadQualitySettings();
        }

        public void SetGeneralVolume(float sliderVolume)
        {
            // Save the general volume settings into the PlayerPrefs
            PlayerPrefs.SetFloat(Utils.AUDIO_MIXER_MASTER_VOLUME_NAME, sliderVolume);

            // Set the general volume
            LoadData.Instance.LoadGeneralVolumeSettings();
        }

        public void SetMusicVolume(float sliderVolume)
        {
            // Save the music volume settings into the PlayerPrefs
            PlayerPrefs.SetFloat(Utils.AUDIO_MIXER_MUSIC_VOLUME_NAME, sliderVolume);

            // Set the music volume
            LoadData.Instance.LoadMusicVolumeSettings();
        }

        public void SetSFXVolume(float sliderVolume)
        {
            // Save the sfx volume settings into the PlayerPrefs
            PlayerPrefs.SetFloat(Utils.AUDIO_MIXER_SFX_VOLUME_NAME, sliderVolume);

            // Set the sfx volume
            LoadData.Instance.LoadSFXVolumeSettings();
        }
        #endregion
    }
}
