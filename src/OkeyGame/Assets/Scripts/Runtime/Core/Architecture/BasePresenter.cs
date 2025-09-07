using System;
using Cysharp.Threading.Tasks;

namespace Runtime.Core.Architecture
{
    public abstract class BasePresenter<TView> : IDisposable where TView : class, IView
    {
        protected TView _view;
        protected bool _isDisposed;

        public virtual void SetView(TView view)
        {
            _view = view;
        }

        public virtual async UniTask InitializeAsync()
        {
            if (_view != null)
            {
                await OnViewSetAsync();
            }
        }

        protected virtual UniTask OnViewSetAsync()
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnViewDestroyed()
        {
            
        }

        public virtual void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            OnViewDestroyed();
            _view = null;
            _isDisposed = true;
        }
    }
}
