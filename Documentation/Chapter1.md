Series of Tutorials to create an interface like Hearthstone where you can drag/drop objects around the scene using touch/hold, and have hover events using ECS systems.
Chapter one will focus on creating the drag/drop system.  Gentle introduction to ECS, that aims to be up to date with the current packages.
You'll find that lots of tutorials are out of date.





1. What is DOTS?

1b. Why DOTS


2. ECS in Unity
ECS stands for Entity, Component, System and is a programming paradigm like Object Oriented Programming(OOP).  

    Entity - ID that that is like a bucket that holds references to components.
    Component - Data
    System - Operates on Components to transform the data

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
Create a interface like Hearthstone where you can drag/drop objects around the scene using touch/hold, and have hover events using ECS systems.
Chapter one will focus on creating the drag/drop system.

Architecture:

1. Components
- User
    - Data for the current state of a user.
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
    - Data on whether entity is focused and holds a reference to the owner.
    ```csharp
        public float3 FocusPosition;
        public Entity FocusOwner;
    ```

2. Entities

The project use Monobehaviors to create entities that implement the `IConvertGameObjectToEntity` interface to go from Monobehavior representations in a scene to ECS.
The naming convention __Proxy is used for these behaviors.

An example of a proxy is the UserComponentProxy, where we take the Camera reference and add it as an component to the created entity.

        ```

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