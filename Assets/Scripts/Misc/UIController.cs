using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject whiteOutSquare;
    public void Update()
    {

    }

    public void FadeToWhite(bool fade = true, int speed = 5)
    {
        StartCoroutine(FadeWhiteOutSquare(fade, speed));
    }

    public IEnumerator FadeWhiteOutSquare(bool fadeToWhite = true, int fadeSpeed = 5)
    {
        Color objectColor = whiteOutSquare.GetComponent<Image>().color;
        float fadeAmount;

        if (fadeToWhite)
        {
            while (whiteOutSquare.GetComponent<Image>().color.a < 1)
            {
                fadeAmount = objectColor.a + (fadeSpeed * Time.deltaTime);

                objectColor = new Color(objectColor.r, objectColor.g, objectColor.b, fadeAmount);
                whiteOutSquare.GetComponent<Image>().color = objectColor;
                yield return null;
            }
        } else
        {
            while (whiteOutSquare.GetComponent<Image>().color.a > 0)
            {
                fadeAmount = objectColor.a - (fadeSpeed * Time.deltaTime);

                objectColor = new Color(objectColor.r, objectColor.g, objectColor.b, fadeAmount);
                whiteOutSquare.GetComponent<Image>().color = objectColor;
                yield return null;
            }
        }
    }
}
