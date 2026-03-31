using Unity.Entities;

namespace Verrarium.DOTS.Components
{
    /// <summary>
    /// Tag đánh dấu entity cần tính Neural Network trong BrainComputeSystem.
    /// Dùng IEnableableComponent để bật/tắt theo frame mà không tạo structural change.
    /// </summary>
    public struct NeedsBrainUpdateTag : IComponentData, IEnableableComponent
    {
    }
}

