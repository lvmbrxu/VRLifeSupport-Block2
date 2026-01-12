public enum ScenarioFlag
{
    None = 0,

    // Minute 1 
    CheckedSafety,
    CheckedResponsiveness,
    OpenedAirway,
    CheckedBreathing,
    Called112,
    AskedForAED,

    // Minute 2
    StartedCPR,
    AedArrived,
    UsedAed,
    MadeSpaceGesture
}

public enum ScenarioAction
{
    MakeSpaceGesture,
    StartCPR,
    UseAED
}