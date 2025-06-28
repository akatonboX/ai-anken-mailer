using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

namespace AnkenMailer
{
    /// <summary>
    /// SkillSelectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectSkillsWindow : Window
    {
        public SelectSkillsWindow(IList<string> currentSkillNames)
        {
            InitializeComponent();

            //■ViewModel構築
            var viewModel = new MyViewModel();
            //■データの読み取り
            {
                using (var command = App.CurrentApp.Connection.CreateCommand())
                {
                    command.CommandText = """
                        select 
                            distinct [SkillName] 
                        from [Skill] 
                        where 
                            length([SkillName]) > 1
                        order by [SkillName];
                    """;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var skillName = reader.GetString(0);
                            if (!currentSkillNames.Contains(skillName, StringComparer.OrdinalIgnoreCase))
                            {
                                viewModel.Data.Add(new Skill(skillName, false));
                            }
                          
                        }
                    }
                }
            }

            this.DataContext = viewModel;
        }
        public MyViewModel ViewModel
        {
            get => (MyViewModel)this.DataContext;
        }

        private void acceptButton_Click(object sender, RoutedEventArgs e)
        {
            if(this.ViewModel.Data.Where(x => x.Selected).Count() == 0)
            {
                MessageBox.Show("選択されていません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
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
            var item = e.Item as Skill;
            if (item != null)
            {
                var clazz = item.GetType();



                foreach (var filterItem in this.ViewModel.ColumnFilters.Items)
                {
                    if (filterItem.Value != null)
                    {
                        var value = clazz.GetProperty(filterItem.Key)?.GetValue(item);
                        e.Accepted = filterItem.Value.Contains(value);

                    }
                    if (e.Accepted == false)
                    {
                        break;
                    }
                }
            }

        }
        private void ColumnHeader_RightClick(object sender, MouseButtonEventArgs e)
        {
            //■編集中であればコミット
            var viewSource = (CollectionViewSource)this.Resources["DataCollectionView"];
            if (viewSource.View is IEditableCollectionView editableView)
            {
                // 必要に応じて Commit してから Refresh
                if (editableView.IsEditingItem)
                    editableView.CommitEdit(); // または CancelEdit()
                if (editableView.IsAddingNew)
                    editableView.CommitNew(); // または CancelNew()
            }

            //■データ収集
            var columnHeader = (DataGridColumnHeader)sender;
            var column = columnHeader.Column;
            var headerName = column.SortMemberPath;

            //■選択肢の生成
            var items = this.ViewModel.Data == null ? new List<object?>()
                        : this.ViewModel.Data
                            .OfType<object>() // DataGridが内部で使うDataRowViewなどを避けるため
                            .Select(item =>
                            {
                                try
                                {
                                    var prop = item.GetType().GetProperty(headerName);
                                    return prop?.GetValue(item);
                                }
                                catch (Exception ex)
                                {
                                    return null;
                                }
                            })
                            .OrderBy(v => v)
                            .Distinct()
                            .ToList();

            //■Windowの準備と表示
            var window = new ColumnFilterWindow(column.Header.ToString().Replace("*", ""), items, this.ViewModel.ColumnFilters[headerName]);
            if (window.ShowDialog() == true)
            {
                //■ViewModelに反映
                this.ViewModel.ColumnFilters[headerName] = window.GetResult();

                //■ヘッダの出力調整
                var header = column.Header.ToString();
                if (header.EndsWith("*") && this.ViewModel.ColumnFilters[headerName] == null)
                {
                    column.Header = header.Substring(0, header.Length - 1);
                }
                else if (!header.EndsWith("*") && this.ViewModel.ColumnFilters[headerName] != null)
                {
                    column.Header = header + "*";
                }


                //■フィルタの適用

                viewSource.View.Refresh();
            }
        }

        private void CheckAllButton_Click(object sender, RoutedEventArgs e)
        {
            var targets = this.dataGrid.Items.Cast<Skill>().ToList();
            foreach (var target in targets)
            {
                target.Selected = true;
            }
        }

        private void UnCheckAllButton_Click(object sender, RoutedEventArgs e)
        {
            var targets = this.dataGrid.Items.Cast<Skill>().ToList();
            foreach (var target in targets)
            {
                target.Selected = false;
            }
        }



        public class MyViewModel : ObservableObject
        {
            private IList<Skill> data;
            private ColumnFilters columnFilters = new ColumnFilters();

            public MyViewModel()
            {
                this.data = new List<Skill>();
            }

            public IList<Skill> Data
            {
                get => data;
                set => SetProperty(ref data, value);
            }

            public ColumnFilters ColumnFilters
            {
                get => this.columnFilters;
                set => this.SetProperty(ref this.columnFilters, value);
            }
        }
        public class Skill : ObservableObject
        {
            private string skillName = "";
            private bool selected;

            public Skill(string skillName, bool selected)
            {
                this.skillName = skillName;
                this.selected = selected;
            }
            public string SkillName
            {
                get => skillName;
                set => SetProperty(ref skillName, value);
            }

            public bool Selected
            {
                get => selected;
                set => SetProperty(ref selected, value);
            }

        }
    }

}
