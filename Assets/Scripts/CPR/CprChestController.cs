using UnityEngine;

[DisallowMultipleComponent]
public sealed class CprChestController : MonoBehaviour
{
    [Header("Chest Reference")]
    [SerializeField] private Transform chestPlate;

    [Header("Physics Settings")]
    [SerializeField, Min(0f)] private float chestStiffness = 200f;
    [SerializeField, Min(0f)] private float chestDamping = 10f;
    [SerializeField, Min(0f)] private float maxCompression = 0.06f;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float minDepthToCount = 0.01f;

    private Vector3 _chestOriginalLocalPos;
    private Vector3 _chestOriginalWorldPos;

    private float _currentCompression;
    private float _velocity;

    private float _startHandY;
    private float _maxDepthReached;
    private bool _tracking;
    private bool _wasInZone;

    /// <summary>
    /// World-space offset of the chest plate from its original position.
    /// This is what you add to your pinned CPR pose to make it "sink" with the chest.
    /// </summary>
    public Vector3 ChestWorldOffset { get; private set; }

    public void Initialize()
    {
        if (chestPlate == null) return;

        _chestOriginalLocalPos = chestPlate.localPosition;
        _chestOriginalWorldPos = chestPlate.position;
        ChestWorldOffset = Vector3.zero;
    }

    /// <summary>
    /// Updates compression tracking and injects hand force into the chest spring.
    /// Returns true once a compression is completed (hands leave zone).
    /// </summary>
    public bool UpdateCompression(Vector3 handsCenter, bool inCprZone, float dt, out float depthReached)
    {
        depthReached = 0f;

        if (chestPlate == null)
            return false;

        if (inCprZone)
        {
            if (!_tracking)
            {
                _startHandY = handsCenter.y;
                _tracking = true;
                _maxDepthReached = 0f;
            }

            float currentDepth = _startHandY - handsCenter.y;
            if (currentDepth > _maxDepthReached)
                _maxDepthReached = currentDepth;

            if (currentDepth > 0f)
            {
                float targetCompression = Mathf.Clamp(currentDepth, 0f, maxCompression);
                float diff = targetCompression - _currentCompression;

                // Inject force based on how far behind we are.
                _velocity += diff * chestStiffness * dt;
            }

            _wasInZone = true;
            return false;
        }

        // Left zone: finalize compression if we had one.
        if (_wasInZone && _tracking && _maxDepthReached >= minDepthToCount)
        {
            depthReached = _maxDepthReached;
            ResetTrackingInternal();
            return true;
        }

        ResetTrackingInternal();
        return false;
    }

    public void UpdatePhysics(float dt)
    {
        if (chestPlate == null)
            return;

        // Spring back to zero compression.
        float springForce = -_currentCompression * chestStiffness;
        _velocity += springForce * dt;

        // Stable damping (frame-rate independent)
        float dampingFactor = Mathf.Exp(-chestDamping * dt);
        _velocity *= dampingFactor;

        _currentCompression += _velocity * dt;
        _currentCompression = Mathf.Clamp(_currentCompression, 0f, maxCompression);

        chestPlate.localPosition = _chestOriginalLocalPos - Vector3.up * _currentCompression;

        // Update world offset after we've moved the chest
        ChestWorldOffset = chestPlate.position - _chestOriginalWorldPos;
    }

    public void ResetTracking()
    {
        ResetTrackingInternal();
    }

    private void ResetTrackingInternal()
    {
        _wasInZone = false;
        _tracking = false;
        _maxDepthReached = 0f;
    }
}
