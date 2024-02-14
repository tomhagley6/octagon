using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Attached to the scoreboard Canvas, will update the score for the game HUD
// as recorded in GameManager
public class Score : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public GameManager gameManager;
    public int score = 0;

    // Keep canvas text updated with the current value of score
    // as recorded in GameManager.cs
    void Update()
    {
        score = gameManager.score;
        scoreText.text = $"Score: {score}";
    }
}
