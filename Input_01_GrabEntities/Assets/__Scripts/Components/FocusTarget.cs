using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Components hold data for an entity.
/// Holds data on whether entity is focused and holds a reference
/// to the owner
/// </summary>
public struct FocusTarget : IComponentData
{
    public float3 FocusPosition;
    public Entity FocusOwner;

    public bool IsFocused
    {
        get {
            return FocusOwner != Entity.Null;
        }
    }

    public void SetFocus(float3 position, Entity owner)
    {
        FocusPosition = position;
        FocusOwner = owner;
    }

}
