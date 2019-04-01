using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Components hold data for an entity.
/// Holds data for the current state of a user.
/// </summary>
public struct UserComponent : IComponentData
{
    public Entity FocusTarget;
    public Entity ViewCamera;
    public TouchData TouchPoint;
}

public struct TouchData
{
    public float3 Position;
    public BoolBlit IsActive;
    public Entity HitTarget;
}
