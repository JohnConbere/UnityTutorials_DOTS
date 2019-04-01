using UnityEngine;
using Unity.Entities;

/// <summary>
/// Implementation of IConvertGameObjectToEntity
/// </summary>
public class FocusTargetProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager entityManager, GameObjectConversionSystem conversionSystem)
    {
        entityManager.AddComponentData(entity, new FocusTarget { });
    }
}
