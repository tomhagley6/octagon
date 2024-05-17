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
        Debug.LogError("PopUpScore is running");
        StartCoroutine(PopUpScoreCoroutine(increment));
    }

    public IEnumerator PopUpScoreCoroutine(int increment)
    {
        Debug.LogError($"PopUpScore Coroutine is running, with increment {increment}");
        scorePopupText.text = $"+{increment}";

        yield return new WaitForSeconds(2f);

        scorePopupText.text = "";
    }


}
