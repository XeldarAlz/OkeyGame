using Cysharp.Threading.Tasks;

namespace Runtime.Core.Architecture
{
    public interface IView
    {
        bool IsActive { get; }
        UniTask ShowAsync();
        UniTask HideAsync();
        void SetInteractable(bool interactable);
    }
}
