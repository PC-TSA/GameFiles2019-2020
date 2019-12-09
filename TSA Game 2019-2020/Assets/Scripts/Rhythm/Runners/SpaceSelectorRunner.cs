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

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.tag == "Space")
            selectableSpaces.Add(collision.gameObject);
    }

    private void OnTriggerExit(Collider collision)
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
                SpaceHitAccuracy(selectableSpaces[0]);

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

    public void SpaceHitAccuracy(GameObject space)
    {
        float hitAccuracy = ((Vector3.Distance(space.transform.position, transform.position) * 100) / transform.GetComponent<RectTransform>().sizeDelta.y) * 1000;
        if (hitAccuracy >= 50) //Hit accuracy = 0-~90
        {
            rhythmRunner.UpdateScore(0.4f); //Bad hit
            rhythmRunner.SpawnSplashTitle("Bad", Color.red);
        }
        else if (hitAccuracy < 50 && hitAccuracy >= 15)
            rhythmRunner.UpdateScore(0.6f); //Moderate hit
        else if (hitAccuracy < 15 && hitAccuracy >= 8)
        {
            rhythmRunner.UpdateScore(0.8f); //Good hit
            rhythmRunner.SpawnSplashTitle("Good", Color.cyan);
        }
        else if (hitAccuracy < 8)
        {
            rhythmRunner.UpdateScore(1); //Perfect hit
            rhythmRunner.SpawnSplashTitle("Perfect", Color.green);
        }
    }
}
