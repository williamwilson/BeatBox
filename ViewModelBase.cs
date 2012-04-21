using System.ComponentModel;

namespace BeatBox
{
    /// <summary>
    /// Represents the base for view models with property change notification.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler _propertyChanged;

        /// <summary>
        /// Raised when a property on this instance has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        /// <summary>
        /// Fires the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property for which the value has changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (_propertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                _propertyChanged(this, e);
            }
        }
    }
}
