using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/* Pop up score increment on the canvas temporarily */
public class ScorePopup : MonoBehaviour
{
    public TextMeshProUGUI scorePopupText;

    public void PopupScore(int increment)
    {
        StartCoroutine(PopUpScoreCoroutine(increment));
    }

    public IEnumerator PopUpScoreCoroutine(int increment)
    {
        scorePopupText.text = $"+{increment}";

        yield return new WaitForSeconds(2f); // display for 2 seconds

        scorePopupText.text = ""; // reset the text
    }


}
