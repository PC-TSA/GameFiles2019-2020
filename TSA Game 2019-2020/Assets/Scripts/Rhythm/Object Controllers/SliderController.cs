using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderController : MonoBehaviour //When scaling in RhythmMaker, RectTransform.height += offset, pos = offset / 2, collider.size.y = RectTransform.height; INVERT FOR NOTE SELECTION
{
    public bool isInGame; //True in game, false in rhythm maker

    public bool hasBeenHit;
    public bool canBeHit = true; //If false, this slider has existed for too long to be hit
    public bool incompleteHit; //If true, this hit was stopped half way

    public float sliderHeightChange; //How much the height (not scale) is changed by each FixedUpdate call

    public float canBeHitTime; //How long the player has from the start of the slider to hit it
        
    public SliderObj sliderCodeObject; //This slider's code object counterpart in the noteObjects list in ScrollController; Used for serialization

    public bool mouseDown;
    public bool mouseOver;
    public Vector3 screenPoint;
    public Vector3 offset;

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

    //When the note is hit, play note hit anim and then kill note
    IEnumerator NoteHit()
    {
        GetComponent<Animation>().Play("NoteHit");
        yield return new WaitForSeconds(0.15f);
        Die();
    }

    public void Hit()
    {
        if (!hasBeenHit)
        {
            hasBeenHit = true;
            StartCoroutine(NoteHit());
        }
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
        if (mouseDown)
        {
            Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorPoint) + offset;
            transform.position = new Vector3(transform.position.x, cursorPosition.y, transform.position.z);
        }

        if (mouseOver)
        {
            if (Input.GetAxis("Mouse ScrollWheel") != 0f) //If scrolling
            {
                transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.x, transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.y + (Input.GetAxis("Mouse ScrollWheel") * 30));
                transform.GetChild(0).localPosition = new Vector3(transform.GetChild(0).localPosition.x, transform.GetChild(0).localPosition.y + (Input.GetAxis("Mouse ScrollWheel") * 15), transform.GetChild(0).localPosition.z);
                GetComponent<BoxCollider2D>().size = new Vector2(GetComponent<BoxCollider2D>().size.x, GetComponent<BoxCollider2D>().size.y + (Input.GetAxis("Mouse ScrollWheel") * 30));
                GetComponent<BoxCollider2D>().offset = new Vector2(GetComponent<BoxCollider2D>().offset.x, GetComponent<BoxCollider2D>().offset.y + (Input.GetAxis("Mouse ScrollWheel") * 15));
            }
        }
    }

    public void MouseDown()
    {
        if (Input.GetMouseButton(0)) //Left click
        {
            mouseDown = true;
            screenPoint = Camera.main.WorldToScreenPoint(transform.position);
            offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
        }
        else if (Input.GetMouseButton(1)) //Right click
            Hit();
    }

    public void MouseUp()
    {
        if(!isInGame)
        {
            mouseDown = false;
            sliderCodeObject.height = transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.y;
            sliderCodeObject.colliderSizeY = GetComponent<BoxCollider2D>().size.y;
            sliderCodeObject.childY = transform.GetChild(0).localPosition.y;
            sliderCodeObject.pos = transform.localPosition;
        }
    }

    void OnMouseEnter()
    {
        if(!isInGame)
            mouseOver = true;
    }

    void OnMouseExit()
    {
        if(!isInGame)
        {
            mouseOver = false;
            sliderCodeObject.height = transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.y;
            sliderCodeObject.childY = transform.GetChild(0).localPosition.y;
            sliderCodeObject.colliderCenterY = GetComponent<BoxCollider2D>().offset.y;
            sliderCodeObject.pos = transform.localPosition;
        }
    }
}
