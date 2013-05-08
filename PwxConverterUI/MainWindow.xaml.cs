using Ctr.FormatLibrary.Pwx;
using Ionic.Zip;
using Microsoft.Win32;
using System;
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

namespace PwxConverterUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_BrowseClick(object sender, RoutedEventArgs e)
        {
            this.StatusLabel.Visibility = System.Windows.Visibility.Hidden;
            this.StatusBorder.Visibility = System.Windows.Visibility.Hidden;

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Pwx files|*.pwx|Pwx Compressed files|*.gz";

            if ((bool)openDialog.ShowDialog())
            {
                this.FileText.Text = openDialog.FileName;
            }
        }

        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_Click_ConvertToGpx(object sender, RoutedEventArgs e)
        {
            try
            {
                string filename = this.FileText.Text;

                PwxConverter converter;

                if (filename.EndsWith("gz", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (ZipOutputStream zipStream = new ZipOutputStream(filename))
                    {
                        converter = new PwxConverter(zipStream);
                    }
                }
                else
                {
                    converter = new PwxConverter(filename);
                }

                string destinationFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\file.gpx";
                converter.SaveAsGpx(destinationFile);

                this.StatusLabel.Text = "File converted: " + destinationFile;
                this.StatusLabel.Visibility = System.Windows.Visibility.Visible;
                this.StatusBorder.Background = new SolidColorBrush(Colors.Green);
                this.StatusBorder.Visibility = System.Windows.Visibility.Visible;
            }
            catch (Exception ex)
            {
                this.StatusLabel.Text = "Error converting:" + ex.Message;
                this.StatusLabel.Visibility = System.Windows.Visibility.Visible;
                this.StatusBorder.Background = new SolidColorBrush(Colors.Red);
                this.StatusBorder.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void Button_Click_ConvertToTcx(object sender, RoutedEventArgs e)
        {
            try
            {
                string filename = this.FileText.Text;

                PwxConverter converter;

                if (filename.EndsWith("gz", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (ZipOutputStream zipStream = new ZipOutputStream(filename))
                    {
                        converter = new PwxConverter(zipStream);
                    }
                }
                else
                {
                    converter = new PwxConverter(filename);
                }

                string destinationFile = filename + @".tcx";
                converter.SaveAsTcx(destinationFile);

                this.StatusLabel.Text = "File converted: " + destinationFile;
                this.StatusLabel.Visibility = System.Windows.Visibility.Visible;
                this.StatusBorder.Background = new SolidColorBrush(Colors.Green);
                this.StatusBorder.Visibility = System.Windows.Visibility.Visible;
            }
            catch (Exception ex)
            {
                this.StatusLabel.Text = "Error converting:" + ex.Message;
                this.StatusLabel.Visibility = System.Windows.Visibility.Visible;
                this.StatusBorder.Background = new SolidColorBrush(Colors.Red);
                this.StatusBorder.Visibility = System.Windows.Visibility.Visible;
            }
        }
    }
}
