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
        Destroy(gameObject);
    }

    private void Update()
    {
        if (mouseDown) //Note dragging for RhythmMaker
            transform.position = new Vector3(transform.position.x, Input.mousePosition.y, transform.position.z);
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0.1f); //Prevents note from swaying slowly toward negative z (idk why) which would cause it to eventually clip behind the canvas / dissapear
    }

    public void MouseDown()
    {
        mouseDown = true;
    }

    public void MouseUp()
    {
        mouseDown = false;
    }
}
