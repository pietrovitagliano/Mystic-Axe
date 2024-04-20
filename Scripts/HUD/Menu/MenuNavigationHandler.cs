// Author: Pietro Vitagliano

using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MysticAxe
{
    [RequireComponent(typeof(CanvasUnscaledFadeHandler))]
    public class MenuNavigationHandler : MonoBehaviour
    {
        [Header("Selectable Settings")]
        [SerializeField] private Selectable selectablePanel;
        [SerializeField] private Selectable firstSelectable;
        
        private GameObject lastMenu = null;
        private CanvasUnscaledFadeHandler canvasUnscaledFadeHandler;

        private Scrollbar scrollBar;

        public Selectable FirstSelectable { get => firstSelectable; }
        public GameObject LastMenu { get => lastMenu; }

        // The gameObject can be disabled, that's why
        // canvasUnscaledFadeHandler needs the Awake() method,
        // in order to be correctly initialized
        private void Awake()
        {
            canvasUnscaledFadeHandler = GetComponent<CanvasUnscaledFadeHandler>();
        }

        // This method update the scroll bar position when a new Selectable is selected
        // (the selection is handled int the inspector by the EventTrigger).
        public void UpdateScrollbarPosition(BaseEventData eventData)
        {
            // Get the y position of the first selectable
            float referenceY = firstSelectable.transform.position.y;

            // Compute the max distance (along Y axis) from the first selectable and the others
            float maxDistanceFromFirstSelectable = GetComponentsInChildren<Selectable>().Max(selectable => Mathf.Abs(selectable.transform.position.y - referenceY));

            // Compute the distance (along Y axis) between the currently selected Selectable and the first one
            float distanceFromFirstSelectable = Mathf.Abs(eventData.selectedObject.transform.position.y - referenceY);

            // Normalize the distance
            float normalizedDistanceFromFirstSelectable = distanceFromFirstSelectable / maxDistanceFromFirstSelectable;
            
            // Update the scrollbar
            scrollBar.value = Mathf.Clamp01(1 - normalizedDistanceFromFirstSelectable);            
        }


        public void OpenMenu()
        {
            // If the menu is scrollable, before opening the menu,
            // reset the scroll position
            ScrollRect scrollRect = GetComponentInChildren<ScrollRect>();
            if (scrollRect != null)
            {
                InitializeVerticalScrollBar(scrollRect);
            }

            canvasUnscaledFadeHandler.Show();

            // Reset the current selected object
            EventSystem.current.SetSelectedGameObject(null);

            // Select the first selectable according to the input device
            SelectFirstSelectable();
        }

        public void GoToNextMenu(GameObject nextMenu)
        {
            MenuNavigationHandler nextMenuUIHandler = nextMenu.GetComponentInChildren<MenuNavigationHandler>();
            nextMenuUIHandler.OpenMenu();
            nextMenuUIHandler.lastMenu = gameObject;

            canvasUnscaledFadeHandler.HideAndDeactivate();
        }

        public void CloseMenu()
        {
            if (lastMenu != null)
            {
                MenuNavigationHandler lastMenuUIHandler = lastMenu.GetComponentInChildren<MenuNavigationHandler>();
                lastMenuUIHandler.OpenMenu();
                lastMenu = null;
            }

            canvasUnscaledFadeHandler.HideAndDeactivate();
        }

        private void SelectFirstSelectable()
        {
            if (InputDeviceHandler.Instance.IsUsingKeyboardAndMouse)
            {
                EventSystem.current.SetSelectedGameObject(selectablePanel.gameObject);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
            }
        }

        private void InitializeVerticalScrollBar(ScrollRect scrollRect)
        {
            if (scrollRect.vertical && scrollRect.verticalScrollbar != null)
            {
                scrollRect.verticalNormalizedPosition = 1;
                scrollBar = scrollRect.verticalScrollbar;
            }
        }

        #region Play UI Sounds Events
        public static void OnPlayConfirmSound()
        {
            AudioManager.Instance.PlaySound(Utils.UIConfirmAudioName, AudioManager.Instance.gameObject);
        }

        public static void OnPlayNavigationSound()
        {
            AudioManager.Instance.PlaySound(Utils.UINavigationAudioName, AudioManager.Instance.gameObject);
        }

        public static void OnPlayCancelSound()
        {
            AudioManager.Instance.PlaySound(Utils.UIBackAudioName, AudioManager.Instance.gameObject);
        }
        #endregion
    }
}