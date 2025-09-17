using UnityEngine;

namespace Runtime.Presentation.Views
{
    public abstract class BaseView : MonoBehaviour
    {
        protected bool _isInitialized = false;

        protected virtual void Awake()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        protected virtual void Initialize()
        {
            _isInitialized = true;
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            Cleanup();
        }

        protected virtual void Cleanup()
        {
        }
    }
}