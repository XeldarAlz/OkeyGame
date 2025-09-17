using Cysharp.Threading.Tasks;
using Runtime.Presentation.Views;
using System;
using Zenject;

namespace Runtime.Presentation.Presenters
{
    public abstract class BasePresenter<TView> : IInitializable, IDisposable where TView : BaseView
    {
        protected TView _view;
        protected bool _isInitialized = false;

        [Inject]
        protected virtual void Construct(TView view)
        {
            _view = view;
        }

        public virtual void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            SubscribeToEvents();
            InitializeView();
        }

        protected virtual void InitializeView()
        {
        }

        protected virtual void SubscribeToEvents()
        {
        }

        protected virtual void UnsubscribeFromEvents()
        {
        }

        public virtual void Dispose()
        {
            UnsubscribeFromEvents();
            _isInitialized = false;
        }

        public virtual void ShowView()
        {
            _view?.Show();
        }

        public virtual void HideView()
        {
            _view?.Hide();
        }

        protected virtual async UniTask OnViewShownAsync()
        {
            await UniTask.CompletedTask;
        }

        protected virtual async UniTask OnViewHiddenAsync()
        {
            await UniTask.CompletedTask;
        }
    }
}