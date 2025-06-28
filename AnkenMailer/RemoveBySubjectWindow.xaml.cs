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
using System.Windows.Markup;
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
                            [SkillName]
                        from [NecessarySkill]
                        order by [SkillName];
                    """;
                    using (var reader = command.ExecuteReader())
                    {
                        var stringBuilder = new StringBuilder();
                        while (reader.Read())
                        {
                            stringBuilder.Append(reader.GetString("SkillName"));
                            stringBuilder.Append("\r\n");
                        }
                        viewModel.Text = stringBuilder.ToString();
                    }
                }
            }

            this.DataContext = viewModel;

        }

        public MyViewModel ViewModel
        {
            get => (MyViewModel)this.DataContext;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var skillNames = this.ViewModel.SkillNames;
            var window = new SelectSkillsWindow(skillNames);
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                this.ViewModel.SkillNames = skillNames.Union(window.ViewModel.Data.Where(x => x.Selected).Select(x => x.SkillName).ToList()).ToList();
            }
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
        private void Save()
        {
            //■SkillValidityテーブルを全件削除
            using (var command = App.CurrentApp.Connection.CreateCommand())
            {
                command.CommandText = "delete from [NecessarySkill];";
                command.ExecuteNonQuery();
            }

            //■必要なスキルだけinsert(少ないと予想しているので
            using (var command = App.CurrentApp.Connection.CreateCommand())
            {
                command.CommandText = "insert into  [NecessarySkill]([SkillName]) values (@SkillName);";
                command.Parameters.Add(new SqliteParameter("@SkillName", SqliteType.Text));
                foreach (var skillName in this.ViewModel.SkillNames)
                {
                    command.Parameters["@SkillName"].Value = skillName;
                    command.ExecuteNonQuery();
                }
            }
        }


        public class MyViewModel : ObservableObject
        {
            private string text = "";



            public string Text
            {
                get => this.text;
                set
                {
                    this.SetProperty(ref this.text, value);
                    this.OnPropertyChanged(nameof(SkillNames));
                }
            }
            public List<string> SkillNames
            {
                get => this.Text
                        .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(x => x.Length > 1)
                        .OrderBy(x => x)
                        .ToList();
                set
                {
                    var newText = string.Join(
                                            Environment.NewLine,
                                            value
                                                .Where(x => x.Length > 1)
                                                .OrderBy(x => x)
                                                .ToList()
                                         );
                    this.Text = newText;
                }
            }
        }


    }

}
