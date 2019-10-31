﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteController : MonoBehaviour
{
    public bool hasBeenHit;

    public Note noteCodeObject; //This note's code object counterpart in the noteObjects list in ScrollController; Used for serialization

    public bool mouseDown;

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
        hasBeenHit = true;
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
        if(FindObjectOfType<RhythmController>() != null)
        {
            FindObjectOfType<RhythmController>().currentRecording.notes.Remove(noteCodeObject);
            FindObjectOfType<RhythmController>().noteGameObjects.Remove(gameObject);
        }
        Destroy(gameObject);
    }

    private void Update()
    {
        Debug.Log(Input.mousePosition);
        if (mouseDown) //Note dragging for RhythmMaker; BROKEN, may not be fixable. Replacement with note 'select' and adjust using keys to move up / down a bit?
            transform.localPosition = new Vector3(transform.localPosition.x, Input.mousePosition.y, transform.localPosition.z);
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
