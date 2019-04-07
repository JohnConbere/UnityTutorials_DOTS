using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

/// <summary>
/// Systems perform transformations on entities.  All logic should be in systems
/// User Input System takes all Users and determines where they are touching the screen
/// Considers the left mouse an active touch.
/// </summary>
public class UserInputSystem : ComponentSystem
{
    private const float ZDISTANCE = 15f;

    private ComponentGroup _group;

    protected override void OnCreateManager()
    {
        _group = GetComponentGroup(
                typeof(UserComponent)
            );
    }

    protected override void OnUpdate()
    {
        UnityEngine.Vector3 mospos = UnityEngine.Input.mousePosition;
        mospos.z = ZDISTANCE;
        
        NativeArray<Entity> entities = _group.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < entities.Length; i++)
        {
            //For each user take their current camera and translate it to a world pos
            // Note: for this sample we only expect one user
            UserComponent data = EntityManager.GetComponentData<UserComponent>(entities[i]);
            UnityEngine.Camera viewcam = EntityManager.GetComponentObject<UnityEngine.Camera>(data.ViewCamera);
            float3 worldPoint = viewcam.ScreenToWorldPoint(mospos);
            data.TouchPoint.Position = worldPoint;
            data.TouchPoint.IsActive = UnityEngine.Input.GetMouseButton(0);

            
            if (data.TouchPoint.IsActive)
            {
                if(data.FocusTarget != Entity.Null)
                {
                    //Update position if have focus
                    EntityManager.SetComponentData(data.FocusTarget, new FocusTarget { FocusOwner = entities[i], FocusPosition = data.TouchPoint.Position });
                }
                else
                {
                    //Check for new collision
                    Entity e = rayCast(viewcam.transform.position, data.TouchPoint.Position, viewcam.farClipPlane);
                    if (e != Entity.Null)
                    {
                        EntityManager.SetComponentData(e, new FocusTarget { FocusOwner = entities[i], FocusPosition = data.TouchPoint.Position });
                        data.TouchPoint.HitTarget = e;
                        data.FocusTarget = e;
                    }
                }
            }else {
                //Reset focus if touch is not active
                if(data.FocusTarget != Entity.Null)
                {
                    EntityManager.SetComponentData(data.FocusTarget, new FocusTarget { });
                    data.FocusTarget = Entity.Null;
                }
                data.TouchPoint.HitTarget = Entity.Null;
            }

            EntityManager.SetComponentData(entities[i], data); //Must call SetComponentData to write the data back out to the component
        }
        entities.Dispose(); //Must call dispose on any Native Collections allocations or you get a memory leak
    }

    /// <summary>
    /// Wrapper for a rayCast in Unity.Physics
    /// Assumes the world is just the active world.
    /// </summary>
    /// <param name="RayFrom"></param>
    /// <param name="RayTo"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    private Entity rayCast(float3 RayFrom, float3 RayTo, float distance)
    {
        var physicsWorldSystem = Unity.Entities.World.Active.GetExistingManager<Unity.Physics.Systems.BuildPhysicsWorld>();
        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
        RaycastInput input = new RaycastInput()
        {
            Ray = new Ray()
            {
                Origin = RayFrom,
                Direction = RayTo - RayFrom
            },
            Filter = new CollisionFilter()
            {
                CategoryBits = ~0u, // all 1s, so all layers, collide with everything 
                MaskBits = ~0u,
                GroupIndex = 0
            }
        };

        RaycastHit hit = new RaycastHit();
        bool haveHit = collisionWorld.CastRay(input, out hit);
        UnityEngine.Debug.DrawRay(input.Ray.Origin, input.Ray.Direction * distance, UnityEngine.Color.red, 5.0f);
        if (haveHit)
        {
            Entity e = physicsWorldSystem.PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
            return e;
        }
        return Entity.Null;
    }
}
