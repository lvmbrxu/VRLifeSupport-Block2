using System.Collections;
using UnityEngine;

public class DogAction : MonoBehaviour
{
    [SerializeField] Transform endPoint;
    [SerializeField] float movementSpeed;

    [SerializeField] int waitTime = 5;

    [SerializeField] Collider endCollider;

    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip barkSound;

    private bool dogMoving = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine (StartMovement());

    }

    // Update is called once per frame
    void Update()
    {
       MoveDog();

    }

    private IEnumerator StartMovement()
    {
        yield return new WaitForSeconds(waitTime);
        dogMoving = true;
    }

    void MoveDog()
    {
        if (dogMoving == true)
        {
            if(Vector3.Distance(gameObject.transform.position, endPoint.position) > 0.5f) 
            { 
            gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, endPoint.transform.position, movementSpeed * Time.deltaTime);
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
}
