using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponent<Animation>().Play("ButtonHighlight");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponent<Animation>().Play("ButtonUnHighlight");
    } 
}
