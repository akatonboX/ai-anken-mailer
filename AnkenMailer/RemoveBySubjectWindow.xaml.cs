using AnkenMailer.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Data.Sqlite;
using Org.BouncyCastle.Asn1.X509;
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
using static AnkenMailer.LlmUsageWindow;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AnkenMailer
{
    /// <summary>
    /// RemoveBySubjectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class RemoveBySubjectWindow : Window
    {
        public RemoveBySubjectWindow()
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
                        Skills.[SkillName]
                        , COALESCE([SkillValidity].[IsNecessary] , 0) as [IsNecessary]
                    from (
                        select distinct [SkillName] from [Skill] where length([SkillName]) > 1
                    ) Skills
                    left join [SkillValidity]
                    on Skills.SkillName = [SkillValidity].SkillName
                    order by Skills.[SkillName];
                """;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            viewModel.Data.Add(new SkillValidity(reader.GetString("SkillName"), reader.GetBoolean("IsNecessary")));
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
            this.Save();
            this.DialogResult = true;
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            this.Save();
            MessageBox.Show("保存しました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            var item = e.Item as SkillValidity;
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
            var targets = this.dataGrid.Items.Cast<SkillValidity>().ToList();
            foreach (var target in targets)
            {
                target.IsNecessary = true;
            }
        }

        private void UnCheckAllButton_Click(object sender, RoutedEventArgs e)
        {
            var targets = this.dataGrid.Items.Cast<SkillValidity>().ToList();
            foreach (var target in targets)
            {
                target.IsNecessary = false;
            }
        }

        private void Save()
        {
            var targets = this.ViewModel.Data;

            //■SkillValidityテーブルを全件削除
            using (var command = App.CurrentApp.Connection.CreateCommand())
            {
                command.CommandText = "delete from [SkillValidity];";
                command.ExecuteNonQuery();
            }

            //■必要なスキルだけinsert(少ないと予想しているので
            using (var command = App.CurrentApp.Connection.CreateCommand())
            {
                command.CommandText = "insert into  [SkillValidity]( [SkillName] , [IsNecessary]) values (@SkillName, 1);";
                command.Parameters.Add(new SqliteParameter("@SkillName", SqliteType.Text));
                foreach (var item in targets.Where(x => x.IsNecessary).ToList())
                {
                    command.Parameters["@SkillName"].Value = item.SkillName;
                    command.ExecuteNonQuery();
                }
            }


            //■不要なスキルを登録
            using (var command = App.CurrentApp.Connection.CreateCommand())
            {
                command.CommandText = """
                    insert into [SkillValidity]( [SkillName] , [IsNecessary]) 
                    select 
                        distinct
                        [Skill].[SkillName]
                        , 0 as [IsNecessary]
                    from [Skill]
                    where 
                        not exists (select 1 from [SkillValidity] where [SkillName] = [Skill].[SkillName]);
                """;
                command.ExecuteNonQuery();
            }

        }
    }

    public class MyViewModel : ObservableObject
    {
        private IList<SkillValidity> data;
        private ColumnFilters columnFilters = new ColumnFilters();

        public MyViewModel()
        {
            this.data = new List<SkillValidity>();
        }

        public IList<SkillValidity> Data
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
    public class SkillValidity : ObservableObject
    {
        private string skillName;
        private bool isNecessary;

        public SkillValidity(string skillName, bool isNecessary)
        {
            this.skillName = skillName;
            this.isNecessary = isNecessary;
        }

        public string SkillName
        {
            get => skillName;
            set => SetProperty(ref skillName, value);
        }

        public bool IsNecessary
        {
            get => isNecessary;
            set => SetProperty(ref isNecessary, value);
        }

    }
}
