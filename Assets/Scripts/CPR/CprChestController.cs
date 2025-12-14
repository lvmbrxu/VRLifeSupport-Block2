using UnityEngine;

/// <summary>
/// Manages chest compression physics and detection
/// </summary>
public class CprChestController : MonoBehaviour
{
    [Header("Chest Reference")]
    public Transform chestPlate;
    
    [Header("Physics Settings")]
    public float chestStiffness = 200f;
    public float chestDamping = 10f;
    public float maxCompression = 0.06f;
    
    private Vector3 chestOriginalPos;
    private float currentChestCompression;
    private float chestVelocity;
    
    private float startHandY;
    private float maxDepthReached;
    private bool trackingPress;
    private bool previouslyInCube;
    
    public void Initialize()
    {
        if (chestPlate != null)
        {
            chestOriginalPos = chestPlate.position;
        }
    }
    
    public bool IsHandsInChest(Vector3 handsCenter)
    {
        if (chestPlate == null) return false;
        Bounds chestBounds = new Bounds(chestPlate.position, chestPlate.localScale);
        return chestBounds.Contains(handsCenter);
    }
    
    public bool UpdateCompression(Vector3 handsCenter, out float depthReached)
    {
        depthReached = 0f;
        bool compressionCompleted = false;
        
        if (chestPlate == null) return false;
        
        bool handsInCube = IsHandsInChest(handsCenter);
        
        if (handsInCube)
        {
            if (!trackingPress)
            {
                startHandY = handsCenter.y;
                trackingPress = true;
                maxDepthReached = 0f;
            }
            
            float currentDepth = startHandY - handsCenter.y;
            if (currentDepth > maxDepthReached)
            {
                maxDepthReached = currentDepth;
            }
            
            if (currentDepth > 0)
            {
                float targetCompression = Mathf.Clamp(currentDepth, 0f, maxCompression);
                float compressionDiff = targetCompression - currentChestCompression;
                chestVelocity += compressionDiff * chestStiffness * Time.deltaTime;
            }
            
            previouslyInCube = true;
        }
        else
        {
            if (previouslyInCube && trackingPress && maxDepthReached > 0.01f)
            {
                depthReached = maxDepthReached;
                compressionCompleted = true;
            }
            
            trackingPress = false;
            maxDepthReached = 0f;
            previouslyInCube = false;
        }
        
        return compressionCompleted;
    }
    
    public void UpdatePhysics()
    {
        if (chestPlate == null) return;
        
        float springForce = -currentChestCompression * chestStiffness;
        chestVelocity += springForce * Time.deltaTime;
        chestVelocity *= (1f - chestDamping * Time.deltaTime);
        currentChestCompression += chestVelocity * Time.deltaTime;
        currentChestCompression = Mathf.Clamp(currentChestCompression, 0f, maxCompression);
        
        chestPlate.position = chestOriginalPos - new Vector3(0, currentChestCompression, 0);
    }
    
    public void ResetTracking()
    {
        previouslyInCube = false;
        trackingPress = false;
    }
    
    public void OnDrawGizmos()
    {
        if (chestPlate != null)
        {
            Gizmos.color = previouslyInCube ? Color.red : Color.yellow;
            Gizmos.DrawWireCube(chestPlate.position, chestPlate.localScale);
        }
    }
}