using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static AnkenMailer.ColumnFilterWindow;

namespace AnkenMailer
{
    /// <summary>
    /// ColumnFilterWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ColumnFilterWindow : Window
    {
        public ColumnFilterWindow(string headerLabel, IList<object> items, IList<object>? values)
        {
           
            InitializeComponent();
            //■itemsからlistの構築
            var allItem = new AllItem((CollectionViewSource)this.Resources["ItemsCollectionViewSource"]);
            var list = items
                        .Distinct()
                        .OrderBy(v => v)
                        .Select(item => new Item(item, values == null ? true : values.Contains(item)))
                        .ToList();
            foreach (var item in list)
            {
                item.PropertyChanged += (object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
                {
                    if(e.PropertyName == nameof(Item.Selected))
                    {
                        allItem.CallOnChange();
                    }
                };
            }
            list.Insert(0, allItem);

            //■ViewModel構築
            this.ViewModel = new ColumnFilterWindowViewModel(list, headerLabel);
            this.ViewModel.PropertyChanged += (object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
            {
                if(e.PropertyName == nameof(ColumnFilterWindowViewModel.SearchText) || e.PropertyName == nameof(ColumnFilterWindowViewModel.IgnoreCase))
                {
                    //■フィルタの適用
                    var viewSource = (CollectionViewSource)this.Resources["ItemsCollectionViewSource"];
                    var view = viewSource.View;
                    view.Refresh();

                    //■非表示項目を未選択にする
                    var filterd = view.Cast<Item>().ToList();
                    foreach (var item in this.ViewModel.Items)
                    {
                        if (!filterd.Contains(item))
                        {
                            item.Selected = false;
                        }
                    }

                    //■「すべて」の再計算
                    allItem.CallOnChange();

                }
            };

        }


        private ColumnFilterWindowViewModel ViewModel
        {
            get => (ColumnFilterWindowViewModel)this.DataContext;
            set => this.DataContext = value;
        }

        public IList<object>? GetResult()
        {
           var list = (from item in this.ViewModel.Items where item.IsAll == false && item.Selected == true select item.Value).ToList();
           return list.Count == this.ViewModel.Items.Count - 1 ? null : list;
        }
        public class ColumnFilterWindowViewModel : ObservableObject
        {
            private IList<Item> items;
            private string searchText = "";
            private string columnName = "";
            private bool ignoreCase = true;
            public ColumnFilterWindowViewModel(IList<Item> items, string columnName)
            {
                this.items = items;
                this.columnName = columnName;
            }

            private bool isItemSleep = false;

            public IList<Item> Items
            {
                get => this.items;
                set => this.SetProperty(ref this.items, value);
            }
            public string SearchText
            {
                get => this.searchText;
                set => this.SetProperty(ref this.searchText, value);
            }
            public string ColumnName
            {
                get => this.columnName;
                set => this.SetProperty(ref this.columnName, value);
            }
            public bool IgnoreCase
            {
                get => this.ignoreCase;
                set => this.SetProperty(ref this.ignoreCase, value);
            }

        }
        public class Item : ObservableObject
        {
            private object value;
            private bool? selected = false;

            public Item()
            {
                this.value = "";
            }
            public Item(object value, bool selected)
            {
                this.value = value;
                this.selected = selected;
            }
            public virtual object Value
            {
                get => this.value;
                set => this.SetProperty(ref this.value, value);
            }
            public virtual bool? Selected
            {
                get => this.selected;
                set => this.SetProperty(ref this.selected, value);
            }
            public virtual bool IsAll
            {
                get => false;
            }
        }
        public class AllItem : Item
        {
            private CollectionViewSource collectionViewSource;
            public AllItem(CollectionViewSource collectionViewSource)
            {
                this.collectionViewSource = collectionViewSource;
            }
            public override bool IsAll => true;
            public override object Value { get => "すべて"; set { } }
            public override bool? Selected
            {
                get
                {
                    var filterdList = this.collectionViewSource.View.Cast<Item>().ToList();
                    var list = filterdList.Where(item => item.IsAll == false).Select(item => item.Selected).Distinct().ToList();
                    return list.Count == 1 ? list[0] : null;
                }
                set
                {
                    if (value != null)
                    {
                        var list = this.collectionViewSource.View.Cast<Item>().Where(item => item.IsAll == false).ToList();
                        foreach (var item in list)
                        {
                            item.Selected = value;
                        }
                    }
                }

            }

            public void CallOnChange()
            {
                this.OnPropertyChanged(nameof(Item.Selected));
            }
        }
        private void acceptButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            var item = e.Item as Item;
            
            if (item != null)
            {
                e.Accepted = item.IsAll || (item.Value == null && this.ViewModel.SearchText.Trim().Length == 0) || (item.Value != null && item.Value.ToString().IndexOf(this.ViewModel.SearchText.Trim(), this.ViewModel.IgnoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture) >= 0);
            }
        }

   
    }

    public class ColumnFilterWindowTiteConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var name = (string)values[0];
            var count = (int)values[1];
            var total = (int)values[2];

            return $"{name}のフィルタ({count - 1} / {total - 1})";

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
