﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;

namespace BeatBox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            WindowInteropHelper interopHelper = new WindowInteropHelper(this);
            /* note: the window has not loaded yet, so has no handle, force the handle
             * creation now */
            interopHelper.EnsureHandle();
            DataContext = new BeatBoxViewModel(interopHelper.Handle);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void PowerButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
