using System.Collections;
using System.Collections.Generic;
using Globals;
using Unity.VisualScripting;
using UnityEngine;

/*  Play a sound effect for audio feedback after winning
    a trial (varied dependent on score outcome) */
public class ScoreSounds : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] AudioClip coins;

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
    }
    
    public void PlayCoinSound(int increment)
    {
        if (increment == General.highScore)
        {
            StartCoroutine(PlayStaggeredCoinsCoroutine(increment));
        }
        else if (increment == General.lowScore)
        {
            Debug.LogWarning("increment is equal to low score");
            StartCoroutine(PlayStaggeredCoinsCoroutine(increment));
            
  

        }
    }

    public IEnumerator PlayStaggeredCoinsCoroutine(int increment)
    {
        // Retrieve the number of repeats for the coin sound effect
        int coinRepeats = General.repeatsDict[increment]; 
        
        // Play the sound for coinRepeats repeats with a short delay
        for (int i = 0; i < coinRepeats; i++)
        {
            audioSource.PlayOneShot(coins,1);

            yield return new WaitForSeconds(0.07f);
        }

    }

}
