using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpaceSelectorController : MonoBehaviour
{
    public KeyCode key;
    public KeyCode spaceGenKey; //Used for placing spaces in manual gen mode

    public List<GameObject> selectableSpaces = new List<GameObject>();

    public bool shouldKillSpaces; //If false, spaces that are hit by this selector wont die; For map making/testing purposes 

    public RhythmController rhythmController;
    public ScrollerController scrollerController;

    public Color color;
    public Color pressColor;

    private void Start()
    {
        spaceGenKey = rhythmController.placeSpaceKeycode;

        color = new Color(rhythmController.selectorColor.r, rhythmController.selectorColor.g, rhythmController.selectorColor.b, 0.5f);
        pressColor = new Color(rhythmController.selectorPressColor.r, rhythmController.selectorPressColor.g, rhythmController.selectorPressColor.b, 0.4f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Space")
            selectableSpaces.Add(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Space")
            selectableSpaces.Remove(collision.gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(key))
        {
            GetComponent<Image>().color = pressColor;

            if (selectableSpaces.Count != 0)
            {
                if (shouldKillSpaces)
                    selectableSpaces[0].GetComponent<SpaceController>().Hit();
                else
                    selectableSpaces[0].GetComponent<SpaceController>().HitNoKill();
                selectableSpaces.RemoveAt(0);
            }
        }

        if(Input.GetKeyUp(key))
            GetComponent<Image>().color = color;

        if (Input.GetKeyDown(spaceGenKey)) //If in manual gen edit mode
        {
            if (rhythmController.editMode == 1)
                scrollerController.SpawnSpace();
        }
    }
}
