using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X2MP.Core
{
    public class ParamEqInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public float Center { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }

        private float _gain;
        public float Gain
        {
            get { return _gain; }
            set
            {
                _gain = value;

                //property changed
                OnPropertyChanged("Gain");
            }
        }

        #region Event Handlers

        /// <summary>
        /// Fires the PropertyChanged event
        /// </summary>
        /// <param name="expression"></param>
        protected void OnPropertyChanged(string propertyName)
        {
            //fire event
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
