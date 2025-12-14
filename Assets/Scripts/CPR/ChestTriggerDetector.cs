using UnityEngine;

public class ChestTriggerDetector : MonoBehaviour
{
    [HideInInspector]
    public CprHandController handController;
    
    // This is optional - you can use it if you want to detect specific hand objects
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand") || other.CompareTag("Player"))
        {
            Debug.Log("Hands entered chest zone");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hand") || other.CompareTag("Player"))
        {
            Debug.Log("Hands left chest zone");
        }
    }
}