using UnityEngine;
using Unity.Entities;

/// <summary>
/// Implementation of IConvertGameObjectToEntity
/// </summary>
public class UserComponentProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public Camera PlayerCamera;

    public void Convert(Entity entity, EntityManager entityManager, GameObjectConversionSystem conversionSystem)
    {
        Entity e = conversionSystem.GetPrimaryEntity(PlayerCamera);
        entityManager.AddComponentData(entity, new UserComponent { ViewCamera = e });
    }
}
