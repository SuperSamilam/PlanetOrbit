using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] bool ShowChild = false;
    [SerializeField] GameObject ObjectToShow;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ShowChild)
            ObjectToShow.SetActive(true);
        else
            this.transform.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ShowChild)
            ObjectToShow.SetActive(false);
        else
            this.transform.gameObject.SetActive(false);
    }

}
