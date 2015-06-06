using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace X2MP.Core
{
    public class PlayListEntry : INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        /// <summary>
        /// Gets or sets the tag info
        /// </summary>
        public TagInfo TagInfo { get; set; }

        /// <summary>
        /// Gets or sets the file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets whether this entry is playing
        /// </summary>
        private bool _isPlaying { get; set; }
        [JsonIgnore]
        public bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            set
            {
                
                _isPlaying = value;

                OnPropertyChanged("IsPlaying");
            }
        }

        #region Event Handlers
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
