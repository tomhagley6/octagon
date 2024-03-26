using TMPro;
using UnityEngine;

// Attached to the scoreboard Canvas, will update the score for the game HUD
// as recorded in GameManager
public class Score : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TrialHandler trialHandler;
    public int score = 0;


    void Start() 
    {
        trialHandler = FindObjectOfType<TrialHandler>();
    }
    // Keep canvas text updated with the current value of score
    // as recorded in GameManager.cs
    void Update()
    {
        score = trialHandler.score;
        scoreText.text = $"Score: {score}";
    }
}
