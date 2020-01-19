using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatGenController : MonoBehaviour
{
    public KeyCode genKey;

    public RhythmController rhythmController;
    public ScrollerController scrollerController;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(genKey)) //If in manual gen edit mode
        {
            if (rhythmController.editMode == 2)
            {
                int randomNoteType = Random.Range(0, rhythmController.laneCount + 1); //0 = space, 1 = right arrow, 2 = up arrow, 3 = left arrow, 4 = down arrow (upper bound exclusive)
                if (randomNoteType == 0)
                    scrollerController.SpawnSpace();
                else
                    scrollerController.SpawnNote(randomNoteType - 1);
            }
        }
    }
}
