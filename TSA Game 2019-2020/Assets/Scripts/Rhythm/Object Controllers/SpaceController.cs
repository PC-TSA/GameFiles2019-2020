using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceController : MonoBehaviour
{
    public bool hasBeenHit;

    public SpaceObj spaceCodeObject; //This space's code object counterpart in the spaceObjects list in ScrollController; Used for serialization

    public bool mouseDown;
    public Vector3 screenPoint;
    public Vector3 offset;

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

    public void StartDeathFade()
    {
        StartCoroutine(DeathFade());
    }

    public void Die()
    {
        if (FindObjectOfType<RhythmController>() != null)
        {
            FindObjectOfType<RhythmController>().currentRecording.spaces.Remove(spaceCodeObject);
            FindObjectOfType<RhythmController>().spaceGameObjects.Remove(gameObject);
            FindObjectOfType<RhythmController>().UpdateSpaceCount(-1);
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

        FindObjectOfType<RhythmController>().isSaved = false;
    }

    public void MouseUp()
    {
        mouseDown = false;
        UpdateCodeObject();
    }
    public void UpdateCodeObject()
    {
        spaceCodeObject.pos = transform.localPosition;
    }
}
