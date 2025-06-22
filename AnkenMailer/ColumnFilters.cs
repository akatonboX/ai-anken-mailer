using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AnkenMailer
{
    public class ColumnFilters : ObservableObject
    {
        private Dictionary<string, IList<object?>?> columnFilters = new Dictionary<string, IList<object?>?>();
        public IList<object?>? this[string headerName]
        {
            get
            {
                if (columnFilters.ContainsKey(headerName))
                {
                    return columnFilters[headerName];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                columnFilters[headerName] = value;
                OnPropertyChanged($"Item[{headerName}]");

            }
        }

        public Dictionary<string, IList<object?>?> Items { get => columnFilters; }

        public void Clear()
        {
            columnFilters.Clear();
            OnPropertyChanged("item");
        }

    }
}
