using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Burst;
using Unity.Jobs;

/// <summary>
/// Systems perform logic on entities.
/// User Focus Job System is a job based system which means it will run on worker threads.
/// Looks at all FocusTarget components and if focused will translate them to the touch position. 
/// </summary>
[UpdateAfter(typeof(UserInputSystem))]
public class UserFocusJobSystem : JobComponentSystem
{
    private ComponentGroup _group;

    protected override void OnCreateManager()
    {
        _group = GetComponentGroup(
                typeof(FocusTarget), typeof(Translation)
            );
    }

    //Runs on a worker thread
    [BurstCompile]
    struct MoveToTouchPosJob : IJobChunk
    {
        public ArchetypeChunkComponentType<Translation> TranslationType;
        [ReadOnly] public ArchetypeChunkComponentType<FocusTarget> FocusTargetType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<FocusTarget> focusTargets = chunk.GetNativeArray(FocusTargetType); //All accesible data comes from the chunk, this extracts it from the chunk to an array format
            NativeArray<Translation> translations = chunk.GetNativeArray(TranslationType);

            for (var i = 0; i < chunk.Count; i++)
            {
                FocusTarget target = focusTargets[i];
                if (target.IsFocused)
                {
                    translations[i] = new Translation { Value = target.FocusPosition };
                }

            }
        }
    }

    // OnUpdate runs on the main thread.
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        MoveToTouchPosJob job = new MoveToTouchPosJob()
        {
            TranslationType = GetArchetypeChunkComponentType<Translation>(false), //Archetypes tell the job system how it should setup the chunk data
            FocusTargetType = GetArchetypeChunkComponentType<FocusTarget>(true)
        };

        return job.Schedule(_group, inputDependencies);
    }

}
