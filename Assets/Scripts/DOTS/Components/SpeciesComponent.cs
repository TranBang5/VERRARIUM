using Unity.Entities;

namespace Verrarium.DOTS.Components
{
    /// <summary>
    /// ECS Component để đánh dấu loài (species) của sinh vật cho Speciation
    /// </summary>
    public struct SpeciesComponent : IComponentData
    {
        public int speciesId;
        public float adjustedFitness; // Fitness đã được điều chỉnh bởi fitness sharing
    }
}

