public class OctagonWallTrigger : MonoBehaviour
{
    // assign parent arena
    private Transform arenaRoot;
    // scripts
    [SerializeField] IdentityAssignment identityAssignment;
    [SerializeField] OctagonArea octagonArea;
    [SerializeField] MLAgent playerAgent;
    [SerializeField] MLAgent opponentAgent;
    // variables
    public int triggerID;
    public int wallID1;
    public int wallID2;
    public List<int> wallIDs;

    void Awake()
    {

        if (identityAssignment != null)
        {
            triggerID = identityAssignment.customID;
        }

        arenaRoot = transform.parent;

        // get colliders
        wallColliders = arenaRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("Wall"))
            .Select(t => t.GetComponent<Collider>())
            .Where(c => c != null)
            .ToList();
    }

    void Start()
    {
        if (!octagonArea.soloMode && opponentAgent == null)
        {
            opponentAgent = arenaRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.CompareTag("OpponentAgent"))
                ?.GetComponent<MLAgent>();
        }
        if (playerAgent == null)
        {
            playerAgent = arenaRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.CompareTag("PlayerAgent"))
                ?.GetComponent<MLAgent>();
        }

    }

    void OnTriggerEnter(Collider interactingObject)
    {
        // if GameObject has an MLAgent component attached assign it to the agent variable
        if (!interactingObject.TryGetComponent<MLAgent>(out var agent)) return;

        wallID1 = octagonArea.activeWalls.wall1;
        wallID2 = octagonArea.activeWalls.wall2;
        wallIDs = new List<int> { wallID1, wallID2 };

        if (wallIDs.Contains(triggerID))
        {
            string winnerTag = agent.CompareTag("PlayerAgent") ? "PlayerAgent" : "OpponentAgent";
            string loserTag = winnerTag == "PlayerAgent" ? "OpponentAgent" : "PlayerAgent";

            HandleWallTrigger(triggerID, wallID1, wallID2, interactorTag);
        }
        else if (!wallIDs.Contains(triggerID))
        {
            string interactorTag = agent.CompareTag("PlayerAgent") ? "PlayerAgent" : "OpponentAgent";

            HandleInactiveTrigger(triggerID, interactorTag);
        }
    }

    public void HandHandleInactiveTrigger(int triggerID, string interactorTag)
    {
        float inactiveWallPenalty = -0.01f;

        MLAgent interactor = interactorTag == "PlayerAgent" ? playerAgent : opponentAgent;

        interactor.AddReward(inactiveWallPenalty);
    }

    public void HandleWallTrigger(int triggerID, int wallID1, int wallID2, string interactorTag)
    {
        MLAgent interactor = interactorTag == "PlayerAgent" ? playerAgent : opponentAgent;

        string thisTrialType = interactor.thisTrialType;

        var (score, rewardType) = octagonArea.TrialInteraction(triggerID, wallID1, wallID2, thisTrialType);

        float scaledReward = score / 10f;

        if (!octagonArea.soloMode && opponentAgent != null)
        {
            MLAgent winner = interactorTag == "PlayerAgent" ? playerAgent : opponentAgent;
            MLAgent loser = interactorTag == "PlayerAgent" ? opponentAgent : playerAgent;

            winner.AddReward(scaledReward);
            loser.AddReward(-scaledReward);

            octagonArea.DisableTriggers();

            playerAgent.EndEpisode();
            opponentAgent.EndEpisode();
        }
        else if (octagonArea.soloMode)
        {
            MLAgent winner = playerAgent;
            winner.AddReward(scaledReward);
            playerAgent.EndEpisode();
        }
    }
}