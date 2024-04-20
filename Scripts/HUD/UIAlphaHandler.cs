// Author: Pietro Vitagliano

using System.Collections;
using UnityEngine;

namespace MysticAxe
{
    public abstract class UIAlphaHandler : MonoBehaviour
    {
        [Header("UI Components Settings")]
        [SerializeField] protected float hideUIDelay = 4.5f;

        protected GameObject character;
        protected HealthHandler healthHandler;
        protected LevelSystemHandler levelSystemHandler;
        protected Canvas[] uiElements;
        
        protected Coroutine hideUICoroutine;

        protected virtual void Start()
        {
            InitializeCharacter();
            
            uiElements = GetComponentsInChildren<Canvas>();

            healthHandler = character.GetComponent<HealthHandler>();
            levelSystemHandler = character.GetComponent<LevelSystemHandler>();

            healthHandler.OnHealthRestoredEvent.AddListener(ShowCharacterUIOnEvent);
            healthHandler.OnHealthLostEvent.AddListener(ShowCharacterUIOnEvent);
            levelSystemHandler.OnLevelUpEvent.AddListener(ShowCharacterUIOnEvent);
        }

        private void Update()
        {
            HandleUIAppearence();
        }

        protected abstract void InitializeCharacter();

        protected abstract void HandleUIAppearence();

        protected void ShowCharacterUI()
        {
            if (hideUICoroutine != null)
            {
                StopCoroutine(hideUICoroutine);
                hideUICoroutine = null;
            }
            
            ShowUIComponents(uiElements);
        }

        protected void HideCharacterUI(float delay = 0, bool hideInstantly = false)
        {
            if (hideUICoroutine == null)
            {
                hideUICoroutine = StartCoroutine(HideCharacterUICoroutine(delay, hideInstantly));
            }
        }
        
        protected void ShowCharacterUIOnEvent()
        {
            ShowCharacterUI();

            hideUICoroutine = StartCoroutine(HideCharacterUICoroutine(hideUIDelay, false));
        }
        
        private IEnumerator HideCharacterUICoroutine(float delay = 0, bool hideInstantly = false)
        {
            yield return new WaitForSeconds(delay);
            
            HideUIComponents(uiElements, hideInstantly);

            hideUICoroutine = null;
        }

        private void ShowUIComponent(Canvas uiComponent)
        {
            uiComponent.GetComponent<FadeHandler>().Show();
        }

        private void HideUIComponent(Canvas uiComponent, bool hideInstantly = false)
        {
            if (hideInstantly)
            {
                uiComponent.GetComponent<FadeHandler>().HideInstantly();
            }
            else
            {
                uiComponent.GetComponent<FadeHandler>().Hide();
            }
        }

        private void ShowUIComponents(Canvas[] uiComponents)
        {
            foreach (Canvas uiComponent in uiComponents)
            {
                ShowUIComponent(uiComponent);
            }
        }

        private void HideUIComponents(Canvas[] uiComponents, bool hideInstantly = false)
        {
            foreach (Canvas uiComponent in uiComponents)
            {
                HideUIComponent(uiComponent, hideInstantly);
            }
        }
    }
}