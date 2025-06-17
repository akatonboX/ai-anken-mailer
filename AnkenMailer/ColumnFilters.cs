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
                    return this.columnFilters[headerName];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                this.columnFilters[headerName] = value;
                this.OnPropertyChanged($"Item[{headerName}]");

            }
        }

        public Dictionary<string, IList<object?>?> Items { get => this.columnFilters; }

        public void Clear()
        {
            this.columnFilters.Clear();
            this.OnPropertyChanged("item");
        }

    }
}
