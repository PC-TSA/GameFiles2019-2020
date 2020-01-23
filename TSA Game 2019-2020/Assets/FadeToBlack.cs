using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeToBlack : MonoBehaviour
{
    bool shouldFade;
    public float target;
    public float time;

    public void FadeOut(float time)
    {
        target = 1;
        this.time = time;
        StartCoroutine(WaitForFade(time));
    }

    public void FadeIn(float time)
    {
        target = 0;
        this.time = time;
        StartCoroutine(WaitForFade(time));
    }

    private void Update()
    {
        if (shouldFade)
            UpdateImageColor();
    }

    void UpdateImageColor()
    {
        Color color = GetComponent<Image>().color;
        float newA = Mathf.Lerp(color.a, target, time * Time.deltaTime);
        color = new Color(color.r, color.g, color.b, newA);
        GetComponent<Image>().color = color;
    }

    IEnumerator WaitForFade(float time)
    {
        shouldFade = true;
        yield return new WaitForSeconds(time);
        shouldFade = false;
    }
}
