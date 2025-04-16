using TMPro;
using UnityEngine;


/* Simple class to define a single view on the scoreboard
Player rank, name, and score are shown per entry */
public class LeaderboardScoreView : MonoBehaviour {
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;

    public void Initialize(string rank, string playerName, string score) {
        rankText.text = rank;
        nameText.text = playerName;
        scoreText.text = score;
    }
}