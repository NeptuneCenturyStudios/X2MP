using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace X2MP.Models
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region INotifyPropertyChanged

        /// <summary>
        /// Fires the PropertyChanged event
        /// </summary>
        /// <param name="expression"></param>
        protected void OnPropertyChanged(Expression<Func<object>> expression)
        {
            
            //get property name
            MemberExpression body = (MemberExpression)expression.Body;
            
            //fire property changed event
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(body.Member.Name));
            }
        }

        /// <summary>
        /// Fires the PropertyChanged event
        /// </summary>
        /// <param name="expression"></param>
        protected void OnPropertyChanged(string propertyName)
        {
            //fire property changed event
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
