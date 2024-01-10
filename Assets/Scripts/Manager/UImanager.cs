using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class UImanager : MonoBehaviour
{
    [SerializeField] EventSystem eventSystem;
    GameObject currentActive;

    public void OnClick(GameObject obj)
    {
        if (obj == currentActive)
        {
            currentActive.SetActive(false);
            currentActive = null;
            return;
        }
        if (currentActive != null)
        {
            currentActive.SetActive(false);
        }
        currentActive = obj;
        currentActive.SetActive(true);
    }

    public void ActivateOrDeactivate(GameObject obj)
    {
        if (obj.activeSelf)
        {
            obj.SetActive(false);
            ResetClick();
        }
        else
            obj.SetActive(true);
    }


    void ResetClick()
    {
        if (currentActive == null)
            return; 
            
        currentActive.SetActive(false);
        currentActive = null;
    }
}
