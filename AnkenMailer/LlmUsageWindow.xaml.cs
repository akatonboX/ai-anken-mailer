using Azure;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AnkenMailer
{
    /// <summary>
    /// lmUsageWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LlmUsageWindow : Window
    {
        public LlmUsageWindow()
        {
            InitializeComponent();
            var viewModel = new MyViewModel();
            //■年月リストを取得
            {
                using (var command = App.CurrentApp.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT DISTINCT substr([Date], 1, 7) AS YearMonth FROM LlmUsage ORDER BY YearMonth ASC;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            viewModel.YearMonths.Add(reader.GetString(0));
                        }
                    }
                }
            }
            //■年月を設定
            viewModel.SelectedYearMonth = viewModel.YearMonths.Count == 0 ? null : viewModel.YearMonths[viewModel.YearMonths.Count - 1];


            this.ViewModel = viewModel;
        }

        public MyViewModel ViewModel
        {
            get
            {
                return (MyViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }
        public class MyViewModel : ObservableObject
        {
            //■価格単価
            //※ https://azure.microsoft.com/ja-jp/pricing/details/cognitive-services/openai-service/ の「gpt 4o 1120 Inp Data Zone」と「gpt 4o 1120 Outp Data Zone」
            private decimal? PricePerOutputToken
            {
                get
                {
                    switch (Properties.Settings.Default.DeploymentName)
                    {
                        case "gpt-4o": return 0.011m;
                        case "gpt-4.1": return 0.008m;
                        case "gpt-4.1-mini": return 0.0016m;
                        default: return null;
                    }
                }
            }
            private decimal? PricePerInputToken
            {
                get
                {
                    switch (Properties.Settings.Default.DeploymentName)
                    {
                        case "gpt-4o": return 0.00275m;
                        case "gpt-4.1": return 0.002m;
                        case "gpt-4.1-mini": return 0.0004m;
                        default: return null;
                    }
                }
            }

            private IList<string> yearMonths = new List<string>();
            private string? selectedYearMonth = null;
            private DataTable? list = null;

            public IList<string> YearMonths
            {
                get => yearMonths;
                set => SetProperty(ref yearMonths, value);
            }
            public string? SelectedYearMonth
            {
                get => selectedYearMonth;
                set{
                    SetProperty(ref selectedYearMonth, value);
                    using (var command = App.CurrentApp.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT *  FROM LlmUsage Where substr([Date], 1, 7) = @Date ORDER BY [Date] ASC;";
                        command.Parameters.AddWithValue("@Date", value);
                        
                        
                        using (var reader = command.ExecuteReader())
                        {
                            var list = new DataTable();
                            list.Load(reader);
                            this.List = list;
                        }
                    }
                }
            }
            public DataTable? List
            {
                get => list;
                set
                {
                    SetProperty(ref list, value);
                    this.OnPropertyChanged(nameof(TotalInputTokenCount));
                    this.OnPropertyChanged(nameof(TotalInputTokencCost));
                    this.OnPropertyChanged(nameof(TotalOutputTokenCount));
                    this.OnPropertyChanged(nameof(TotalOutputTokenCost));
                    this.OnPropertyChanged(nameof(TotaTokenCost));

                }
            }

            public long? TotalInputTokenCount
            {
                get => this.list == null ? null : this.list.AsEnumerable().Sum(row => row.IsNull("InputTokenCount") ? 0 : (long)row["InputTokenCount"]);
            }
            public decimal? TotalInputTokencCost
            {
                get => this.TotalInputTokenCount == null ? null : (new Decimal((long)this.TotalInputTokenCount) / 1000m) * (this.PricePerInputToken ?? 0m);
            }
            public long? TotalOutputTokenCount
            {
                get => this.list == null ? null : this.list.AsEnumerable().Sum(row => row.IsNull("OutputTokenCount") ? 0 : (long)row["OutputTokenCount"]);
            }
            public decimal? TotalOutputTokenCost
            {
                get => this.TotalOutputTokenCount == null ? null : (new Decimal((long)this.TotalOutputTokenCount) / 1000m) * (this.PricePerOutputToken ?? 0m);
            }
            public decimal? TotaTokenCost
            {
                get => this.TotalInputTokencCost == null || this.TotalOutputTokenCost == null ? null : this.TotalInputTokencCost + this.TotalOutputTokenCost;
            }
        }

    }
}
