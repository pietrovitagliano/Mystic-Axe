// Author: Pietro Vitagliano

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MysticAxe
{
    public class DropdownScrollBarHandler : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropDown;
        
        public void UpdateScrollbarPosition(BaseEventData eventData)
        {
            Scrollbar scrollBar = GetComponent<Scrollbar>();

            // Get the index of the selected option
            // The sibling index start from 1 and goes up to the number of options: that's why it's necessary to subtract 1
            int selectedIndex = eventData.selectedObject.transform.GetSiblingIndex() - 1;

            // Compute scrollbar normalized position, taking into account the dropdown index
            // To work properly, the scrollbar has to go from 1 to 0 and the direction has to be from the bottom to the top
            float normalizedValue = (float)selectedIndex / (dropDown.options.Count - 1);
            scrollBar.value = Mathf.Clamp01(1 - normalizedValue);
        }
    }
}