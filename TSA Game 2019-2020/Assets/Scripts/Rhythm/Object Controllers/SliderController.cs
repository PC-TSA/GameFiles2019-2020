using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderController : MonoBehaviour //When scaling in RhythmMaker, RectTransform.height += offset, pos = offset / 2, collider.size.y = RectTransform.height; INVERT FOR NOTE SELECTION
{
    public bool hasBeenHit;
    public bool canBeHit = true; //If false, this slider has existed for too long to be hit
    public bool incompleteHit; //If true, this hit was stopped half way

    public float sliderHeightChange; //How much the height (not scale) is changed by each FixedUpdate call

    public float canBeHitTime; //How long the player has from the start of the slider to hit it
        
    public SliderObj sliderCodeObject; //This slider's code object counterpart in the noteObjects list in ScrollController; Used for serialization

    public bool mouseDown;
    public float mouseDownPos;
    public float mouseDownSliderPos;

    private void Start()
    {
        //Generates canBeHitTime, adjusting for faster tracks
        if (FindObjectOfType<RhythmRunner>() != null)
        {
            canBeHitTime = FindObjectOfType<RhythmRunner>().scrollSpeed * 0.05f;
            sliderHeightChange = FindObjectOfType<RhythmRunner>().scrollSpeed;
        }
        else if (FindObjectOfType<ScrollerController>() != null)
        {
            canBeHitTime = FindObjectOfType<ScrollerController>().scrollSpeed * 0.05f;
            sliderHeightChange = FindObjectOfType<ScrollerController>().scrollSpeed;
        }
    }

    //Waits until after the note has faded out, then deletes
    IEnumerator DeathFade()
    {
        GetComponent<Animation>().Play("NoteFadeOut");
        yield return new WaitForSeconds(1f);
        Die();
    }

    //When the note is hit, play note hit anim but DONT kill note; For level testing purposes
    public void HitNoKill()
    {
        GetComponent<Animation>().Play("NoteHitNotKill");
    }

    //When the note is done being hit, play note hit anim and then kill note
    IEnumerator NoteHitDeath()
    {
        //GetComponent<Animation>().Play("NoteHit"); No death anim for sliders currently since they shrink into nothingness
        yield return new WaitForSeconds(0.15f); 
        Die();
    }

    public void StartCanBeHitTimer()
    {
        StartCoroutine(CanBeHitTimer());
    }

    IEnumerator CanBeHitTimer()
    {
        yield return new WaitForSeconds(canBeHitTime);
        canBeHit = false;
        if(!hasBeenHit)
            FindObjectOfType<RhythmRunner>().UpdateNotesMissed(1);
    }

    public void HitDeath()
    {
        StartCoroutine(NoteHitDeath());
    }

    public void StartDeathFade()
    {
        StartCoroutine(DeathFade());
    }

    public void Die()
    {
        if (FindObjectOfType<RhythmController>() != null)
        {
            FindObjectOfType<RhythmController>().currentRecording.sliders.Remove(sliderCodeObject);
            FindObjectOfType<RhythmController>().sliderGameObjects.Remove(gameObject);
            FindObjectOfType<RhythmController>().UpdateSliderCount(-1);
        }
        Destroy(gameObject);
    }

    private void Update()
    {
        if (mouseDown) //Note dragging/clicking, Only works if camera is in screen overlay
            transform.position = new Vector3(transform.position.x, mouseDownSliderPos + -(mouseDownPos - Input.mousePosition.y), transform.position.z);
    }

    public void MouseDown()
    {
        if (Input.GetMouseButton(0)) //Left click
        {
            mouseDown = true;
            mouseDownPos = Input.mousePosition.y;
            mouseDownSliderPos = transform.position.y;
        }
        else if (Input.GetMouseButton(1)) //Right click
            HitDeath();

    }

    public void MouseUp()
    {
        mouseDown = false;
    }
}
