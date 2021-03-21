using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace YoutubeBulkUploadUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        class FileModel
        {
            public string File { get; set; }
        }

        readonly ObservableCollection<FileModel> filesCollection = new ObservableCollection<FileModel>();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new
            {
                TestBinding = filesCollection
            };
            dgv.AllowDrop = true;
            dgv.Drop += Dgv_Drop;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            dgv.Columns[0].IsReadOnly = true;

            base.OnContentRendered(e);
        }

        private void Dgv_Drop(object sender, DragEventArgs e)
        {

            // Shell IDList Array;DragImageBits;DragContext;DragSourceHelperFlags;InShellDragLoop;FileDrop;FileNameW;FileName

            if (e.Data.GetDataPresent("FileDrop"))
            {
                var files = (e.Data.GetData("FileDrop") as string[]);
                foreach (var file in files)
                {
                    filesCollection.Add(new FileModel { File = file });
                }
            }
            // MessageBox.Show(string.Join(";", ));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void but_upload_Click(object sender, RoutedEventArgs e)
        {

        }

        private void but_add_Click(object sender, RoutedEventArgs e)
        {

        }

        private void but_import_Click(object sender, RoutedEventArgs e)
        {

        }

        private void but_export_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
