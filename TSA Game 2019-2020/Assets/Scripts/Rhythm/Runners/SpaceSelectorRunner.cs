using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpaceSelectorRunner : MonoBehaviour
{
    public KeyCode key;

    public List<GameObject> selectableSpaces = new List<GameObject>();

    public RhythmRunner rhythmRunner;

    public Sprite normalSprite;
    public Sprite pressSprite;

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
            GetComponent<Image>().sprite = pressSprite;

            if (selectableSpaces.Count != 0)
            {
                rhythmRunner.UpdateNotesHit(1);
                rhythmRunner.UpdateScore(1);

                //Removes oldest space in the selectable spaces list
                selectableSpaces[0].GetComponent<SpaceController>().Hit();
                selectableSpaces.RemoveAt(0);
            }
            else
                rhythmRunner.UpdateMissclicks(1);
        }

        if(Input.GetKeyUp(key))
            GetComponent<Image>().sprite = normalSprite;
    }
}
