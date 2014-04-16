using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Boxer.Data
{
    [Serializable]
    public class FastObservableCollection<T> : ObservableCollection<T>
    {
        private bool _pause;

        public FastObservableCollection()
        {
            _pause = false;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_pause)
            {
                base.OnCollectionChanged(e);
            }
        }

        public void PauseNotification()
        {
            _pause = true;
        }

        public void ResumeNotification()
        {
            _pause = false;
        }

        public void Refresh()
        {
            ResumeNotification();
            var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            OnCollectionChanged(arg);
        }
    }
}