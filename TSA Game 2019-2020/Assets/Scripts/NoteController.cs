using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteController : MonoBehaviour
{
    bool hasBeenHit;

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Selector")
            StartCoroutine(DeathFade());
    }

    //Waits until after the note has faded out, then deletes
    IEnumerator DeathFade()
    {
        FindObjectOfType<RhythmController>().UpdateNotesMissed(1);
        GetComponent<Animation>().Play("NoteFadeOut");
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }

    //When the note is hit, play note hit anim and then fade out
    IEnumerator NoteHit()
    {
        GetComponent<Animation>().Play("NoteHit");
        yield return new WaitForSeconds(0.15f);
        Destroy(gameObject);
    }

    public void Hit()
    {
        if (!hasBeenHit)
        {
            hasBeenHit = true;
            StartCoroutine(NoteHit());
        }
    }
}
