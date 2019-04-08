
# ECS in Unity #
ECS stands for Entity, Component, System and is a programming paradigm like Object Oriented Programming(OOP).  

    - Entity: Bucket that holds references to components.  It is just a unique ID behind the scenes.
    - Component: Data attached to entities.
    - System: Operates on Components to transform the data

In Unity we have a Monobehavior world and a ECS world that has a cost to move between.  To be able to access Unity Systems like Input or Camera in ECS Systems, we must convert them to Entity representations.

For a more detailed talk on ECS, see [Overview - Intro To The Entity Component System And C# Job System](https://www.youtube.com/watch?v=WLfhUKp2gag)

3. Hybrid Implementation
To ease the transition between ECS and Monobehaviors Unity offers the ability to move a Monobehavior component to an ECS Entity using
[ConvertToEntity](https://docs.unity3d.com/Packages/com.unity.entities@0.0/api/Unity.Entities.ConvertToEntity.html) Monobehavior with `IConvertGameObjectToEntity`.  

    ```csharp
        public void Convert(Entity entity, EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            Entity e = conversionSystem.GetPrimaryEntity(PlayerCamera);
            entityManager.AddComponentData(entity, new UserComponent { ViewCamera = e });
        }
    ```

3. ComponentSystem
Runs on the Main thread.
Can access global data like EntityManager and other Unity Systems like Input


4. JobComponent System
Runs on worker threads.  Unity controls the management of these threads.  Because it is on a worker thread memory is strictly controlled to prevent race conditions.

Chunks:
Block of data available to a job.


5. Unity Physics

The normal physics engine is only accesible from the Monobehavior world which would require us work across systems, but conventiently Unity Physics has just arrived in preview.
While it is currently under development, our simple use-case because we need to cast a Raycast from the camera to an entity to detect which objects the user is currently touching.
Accesible from ECS.


------------------------------------

## Project Architecture ##

### External Dependecies ###

- [Unity.Entities:0.0.12-preview.29](https://docs.unity3d.com/Packages/com.unity.entities@0.0/api/Unity.Entities.html)
- [Unity.Jobs:0.0.7-preview.9](https://docs.unity3d.com/Packages/com.unity.jobs@0.0/manual/index.html)
- [Unity.Physics:0.0.1-preview.1](https://docs.unity3d.com/Packages/com.unity.physics@0.0/manual/index.html)
- [Unity.Collections:0.0.9-preview.16](https://docs.unity3d.com/Packages/com.unity.collections@0.0/manual/index.html)
- [Unity.Mathematics:1.0.0-preview.1](lihttps://docs.unity3d.com/Packages/com.unity.mathematics@1.0/manual/index.htmlnk)
- [Unity.Rendering.Hybrid](https://docs.unity3d.com/Packages/com.unity.rendering.hybrid@0.0/manual/index.html)
- [Unity.Burst:1.0.0-preview.6](https://docs.unity3d.com/Packages/com.unity.burst@1.0/manual/index.html)

1. Components

    Located at _Scripts/Components
    
- User
    - [UserComponent.cs](../Input_01_GrabEntities/Assets/__Scripts/Components/UserComponent.cs)
    - Data for the current state of a user and the where they are touching the screen.

    ```csharp
        struct User {
            public Entity FocusTarget; 
            public Entity ViewCamera;
            public TouchData TouchPoint;
        }
        
        struct TouchData
        {
            public float3 Position;
            public BoolBlit IsActive;
            public Entity HitTarget;
        }
    ```

- FocusTarget
    - [FocusTarget.cs](../Input_01_GrabEntities/Assets/__Scripts/Components/FocusTarget.cs)
    - Data on whether entity is focused and holds a reference to the owner.

    ```csharp
        public float3 FocusPosition;
        public Entity FocusOwner;
    ```

2. Entities

    Located at _Scripts/Entities
    The project use Monobehaviors to create entities that implement the `IConvertGameObjectToEntity` interface to go from Monobehavior representations in a scene to ECS.
    The naming convention __Proxy is used for these behaviors.
    The project has two Entity types: a User, and one for the Targets a user can Drag/Drop 

- Focus Target Proxy
    - [FocusTargetProxy.cs](../Input_01_GrabEntities/Assets/__Scripts/Entities/FocusTargetProxy.cs)
    - A simple implementation of convert.  In the Covert Function, we add a FocusTarget component to the converted Entity.

        ```csharp
            public void Convert(Entity entity, EntityManager entityManager, GameObjectConversionSystem conversionSystem)
            {
                entityManager.AddComponentData(entity, new FocusTarget { });
            }
        ```

- Focus Target Proxy
    - [UserComponentProxy.cs](../Input_01_GrabEntities/Assets/__Scripts/Entities/UserComponentProxy.cs)
    - In this scenario, we take the PlayerCamera Monobehavior reference, convert it to an entity representation, and then store that reference in a UserComponent that is attached to the converted Entity.  This lets us access the Camera Monobehavior component in ECS Systems.

        ```csharp

                public class UserComponentProxy : MonoBehaviour, IConvertGameObjectToEntity
                {
                    public Camera PlayerCamera;

                    public void Convert(Entity entity, EntityManager entityManager, GameObjectConversionSystem conversionSystem)
                    {
                        Entity e = conversionSystem.GetPrimaryEntity(PlayerCamera);
                        entityManager.AddComponentData(entity, new UserComponent { ViewCamera = e });
                    }
                }

        ```

3. Systems
Systems perform transformations on entities.  All logic should be in systems

- User Input System
/// Systems perform transformations on entities.  All logic should be in systems
/// User Input System takes all Users and determines where they are touching the screen
/// Considers the left mouse an active touch.

UserInputSystem inherits from a ComponentSystem.  ComponentSystems run on the main thread.
    ``` 
        public class UserInputSystem : ComponentSystem
    ```

OnCreateManager gets is where initialization occurs.  This is similar to the Awake/Start functions a Monobehavior have.
In UserInputSystem we create a ComponentGroup which is a list of all UserComponents attached to Entities.  In our case we will
only have one.

TODO: Explain what a ComponentGroup is.

    ```
        private const float ZDISTANCE = 15f;

        private ComponentGroup _group;

        protected override void OnCreateManager()
        {
            _group = GetComponentGroup(
                    typeof(UserComponent)
                );
        }
    ```

    ```
        protected override void OnUpdate()
        {
            UnityEngine.Vector3 mospos = UnityEngine.Input.mousePosition;
            mospos.z = ZDISTANCE;
            
            NativeArray<Entity> entities = _group.ToEntityArray(Allocator.TempJob);

    ```

    Here we take the group we created previously and get a NativeArray of all the entities that match that group.  A NativeArray is a Unity representation of an unmanaged array. 

    TODO: 
    EntityManager.GetComponentData
    EntityManager.GetComponentObject
    SetComponentData description
    Entities.Dispose

    ```
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
    ```


- User Focus Job System
/// User Focus Job System is a job based system which means it will run on worker threads.
/// Looks at all FocusTarget components and if focused will translate them to the touch position. 

References:
[Unity.Entities](https://docs.unity3d.com/Packages/com.unity.entities@0.0/api/Unity.Entities.html)

Further Reading:
Unfortunately because of the ongoing ECS development, many of the samples and tutorials you will find are out of date and often use deprecated features.
I recommend the following for further learning:

ECS Samples:
The most up to date examples that Unity provides.
https://github.com/Unity-Technologies/EntityComponentSystemSamples

FPS Shooter Sample:
Best source for how a full game architecture can be created using a Hyrbid ECS implementation.
https://github.com/Unity-Technologies/FPSSample