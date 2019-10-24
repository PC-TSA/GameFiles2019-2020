using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteController : MonoBehaviour
{
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Selector")
        {
            GetComponent<Animator>().Play("NoteFadeOut");
            StartCoroutine(waitToDelete());
        }
    }

    //Waits until after the note has faded out, then deletes
    IEnumerator waitToDelete()
    {
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }
}
