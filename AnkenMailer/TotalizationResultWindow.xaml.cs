﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
    /// TotalizationResultWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TotalizationResultWindow : Window
    {
        public TotalizationResultWindow(DataTable table)
        {
            InitializeComponent();
            this.DataContext = table;
        }


        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            this.dataGrid.SelectAllCells();
            ApplicationCommands.Copy.Execute(null, this.dataGrid);
            this.dataGrid.UnselectAllCells();
        }
    }
}
