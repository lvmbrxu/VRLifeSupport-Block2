using System.Collections;
using UnityEngine;

public class DogAction : MonoBehaviour
{
    [SerializeField] Transform endPoint;
    [SerializeField] Transform awayPoint;
    [SerializeField] float movementSpeed;

    [SerializeField] int waitTimeToMoveToPlayer = 3;
    [SerializeField] int waitTimeToMoveAway = 5;

    [SerializeField] Collider endCollider;

    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip barkSound;

    private bool dogMovingToPlayer = false;
    private bool dogMovingAway = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine (StartMovement());
        StartCoroutine (MoveAway());

    }

    // Update is called once per frame
    void Update()
    {
       MoveDogToPlayer();
       MoveDogAway();
    }

    private IEnumerator StartMovement()
    {
        yield return new WaitForSeconds(waitTimeToMoveToPlayer);
        dogMovingToPlayer = true;
    }

    private IEnumerator MoveAway()
    {
        yield return new WaitForSeconds(waitTimeToMoveAway);
        dogMovingToPlayer = false;
        dogMovingAway = true;
    }

    void MoveDogToPlayer()
    {
        if (dogMovingToPlayer == true)
        {
            if(Vector3.Distance(gameObject.transform.position, endPoint.position) > 0.5f) 
            { 
            gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, endPoint.transform.position, movementSpeed * Time.deltaTime);
            }
        }
    }

    void MoveDogAway()
    {
        
        if (dogMovingAway == true)
        {
            dogMovingToPlayer = false;
            if (Vector3.Distance(gameObject.transform.position, awayPoint.position) > 0.5f)
            {
                gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, awayPoint.transform.position, movementSpeed * Time.deltaTime);
            }
        }
    }

    void Bark()
    {
          audioSource.Play();
    }

    private void OnTriggerEnter(Collider endCollider)
    {
        Bark();
    }

    private void OnTriggerExit(Collider endCollider)
    {
        audioSource.Stop();
    }
}
