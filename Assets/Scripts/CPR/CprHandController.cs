using UnityEngine;

/// <summary>
/// Manages hand visuals and CPR pose detection with magnetic snap
/// </summary>
public class CprHandController : MonoBehaviour
{
    [Header("Hand References")]
    public Transform leftHand;
    public Transform rightHand;
    
    [Header("Hand Visuals")]
    public GameObject leftHandVisual;
    public GameObject rightHandVisual;
    public GameObject cprHandsPose;
    
    [Header("Settings")]
    public float handOverlapDistance = 0.15f;
    
    [Header("Magnetic Snap")]
    public SphereCollider chestTriggerZone;
    public Transform magneticSnapPoint; // The exact position on chest where hands should snap
    public float snapSpeed = 10f;
    public bool requireTriggerForSnap = true;
    
    private bool previouslyOverlapping;
    private bool handsInTriggerZone;
    private bool isSnapped;
    
    void Start()
    {
        // Setup trigger if needed
        if (chestTriggerZone != null)
        {
            chestTriggerZone.isTrigger = true;
            // Add a trigger detector component
            ChestTriggerDetector detector = chestTriggerZone.gameObject.GetComponent<ChestTriggerDetector>();
            if (detector == null)
            {
                detector = chestTriggerZone.gameObject.AddComponent<ChestTriggerDetector>();
            }
            detector.handController = this;
        }
    }
    
    public bool CheckHandsOverlap()
    {
        if (leftHand == null || rightHand == null) return false;
        float distance = Vector3.Distance(leftHand.position, rightHand.position);
        return distance < handOverlapDistance;
    }
    
    public Vector3 GetHandsCenter()
    {
        if (leftHand == null || rightHand == null) return Vector3.zero;
        return (leftHand.position + rightHand.position) / 2f;
    }
    
    public bool IsHandsInTriggerZone()
    {
        if (chestTriggerZone == null) return true; // If no trigger, always allow
        
        Vector3 handsCenter = GetHandsCenter();
        return chestTriggerZone.bounds.Contains(handsCenter);
    }
    
    public void UpdateHandState()
    {
        bool handsOverlap = CheckHandsOverlap();
        handsInTriggerZone = IsHandsInTriggerZone();
        
        // Only allow CPR pose if hands overlap AND are in trigger zone (if required)
        bool canActivateCpr = handsOverlap && (!requireTriggerForSnap || handsInTriggerZone);
        
        if (canActivateCpr && !previouslyOverlapping)
        {
            ShowCprPose();
            isSnapped = true;
        }
        else if (!handsOverlap && previouslyOverlapping)
        {
            ShowNormalHands();
            isSnapped = false;
        }
        
        previouslyOverlapping = handsOverlap;
        
        // Update CPR pose position
        if (canActivateCpr && cprHandsPose != null && cprHandsPose.activeSelf)
        {
            UpdateCprPosePosition();
        }
    }
    
    private void UpdateCprPosePosition()
    {
        if (magneticSnapPoint != null)
        {
            // Snap to the magnetic point
            cprHandsPose.transform.position = Vector3.Lerp(
                cprHandsPose.transform.position,
                magneticSnapPoint.position,
                Time.deltaTime * snapSpeed
            );
            cprHandsPose.transform.rotation = Quaternion.Slerp(
                cprHandsPose.transform.rotation,
                magneticSnapPoint.rotation,
                Time.deltaTime * snapSpeed
            );
        }
        else
        {
            // Fall back to hands center
            Vector3 midPoint = GetHandsCenter();
            cprHandsPose.transform.position = midPoint;
            cprHandsPose.transform.rotation = leftHand.rotation;
        }
    }
    
    private void ShowCprPose()
    {
        if (leftHandVisual != null) leftHandVisual.SetActive(false);
        if (rightHandVisual != null) rightHandVisual.SetActive(false);
        if (cprHandsPose != null)
        {
            cprHandsPose.SetActive(true);
            if (magneticSnapPoint != null)
            {
                cprHandsPose.transform.position = magneticSnapPoint.position;
                cprHandsPose.transform.rotation = magneticSnapPoint.rotation;
            }
            else
            {
                cprHandsPose.transform.position = GetHandsCenter();
            }
        }
    }
    
    private void ShowNormalHands()
    {
        if (leftHandVisual != null) leftHandVisual.SetActive(true);
        if (rightHandVisual != null) rightHandVisual.SetActive(true);
        if (cprHandsPose != null) cprHandsPose.SetActive(false);
    }
    
    public void OnDrawGizmos()
    {
        if (leftHand != null && rightHand != null)
        {
            Vector3 center = GetHandsCenter();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, 0.03f);
        }
        
        // Draw trigger zone
        if (chestTriggerZone != null)
        {
            Gizmos.color = handsInTriggerZone ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(chestTriggerZone.transform.position, chestTriggerZone.radius);
        }
        
        // Draw magnetic snap point
        if (magneticSnapPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(magneticSnapPoint.position, 0.05f);
            Gizmos.DrawLine(magneticSnapPoint.position, magneticSnapPoint.position + magneticSnapPoint.forward * 0.1f);
        }
    }
}