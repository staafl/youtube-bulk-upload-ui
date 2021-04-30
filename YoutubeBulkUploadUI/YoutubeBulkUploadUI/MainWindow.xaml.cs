using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
using Google.Apis.Upload;
using System.ComponentModel;

namespace YoutubeBulkUploadUI
{
    public class DataGridComboBoxColumnWithBindingHack : DataGridComboBoxColumn
    {
        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            FrameworkElement element = base.GenerateEditingElement(cell, dataItem);
            CopyItemsSource(element);
            return element;
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            FrameworkElement element = base.GenerateElement(cell, dataItem);
            CopyItemsSource(element);
            return element;
        }

        private void CopyItemsSource(FrameworkElement element)
        {
            BindingOperations.SetBinding(element, ComboBox.ItemsSourceProperty,
              BindingOperations.GetBinding(this, ComboBox.ItemsSourceProperty));
        }
    }
        public enum VideoVisibility
        {
            Public,
            Unlisted,
            Private
        }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        class FileModel : INotifyPropertyChanged
        {
            string status;
            public string File { get; set; }
            public string Status { get { return status; } set { status = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs (nameof(Status))); } }
            public string Length { get; set; }
            public string Title { get; set; }
            // todo: get from EXIF tags
            public string Description { get; set; }
            public VideoVisibility Visibility { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
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
            base.OnContentRendered(e);

            UserCredential credential;
            using (FileStream stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeUpload },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("YouTube.Auth.Store")).Result;
            }

            youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
            });

        }

        private void videosInsertRequest_ResponseReceived(Video obj)
        {
        }

        private void videosInsertRequest_ProgressChanged(IUploadProgress obj)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    progress.Value = (int)(100 * (obj.BytesSent / (double)fileStream.Length));
                }
                catch (ObjectDisposedException)
                {
                    progress.Value = 100;
                }
            }));
        }

        private void Dgv_Drop(object sender, DragEventArgs e)
        {
            // Shell IDList Array;DragImageBits;DragContext;DragSourceHelperFlags;InShellDragLoop;FileDrop;FileNameW;FileName

            if (e.Data.GetDataPresent("FileDrop"))
            {
                var files = (e.Data.GetData("FileDrop") as string[]);
                foreach (var file in files)
                {
                    var model = new FileModel
                    {
                        File = file,
                        Status = "Pending",
                        Visibility = VideoVisibility.Unlisted,
                        Title = System.IO.Path.GetFileNameWithoutExtension(file)
                    };
                    filesCollection.Add(model);
                }
            }
        }

        FileStream fileStream;
        private YouTubeService youtubeService;

        private async void but_upload_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            int ii = 0;
            foreach (var file in filesCollection)
            {
                file.Status = "Uploading...";
                ii += 1;
                label.Content = "Uploading video " + ii + ": " + System.IO.Path.GetFileName(file.File);
                var filePath = file.File;
                var video = new Video();
                video.Snippet = new VideoSnippet();
                video.Snippet.Title = file.Title;
                video.Snippet.Description = file.Description;
                //video.Snippet.Tags = new string[] { "tag1", "tag2" };
                video.Snippet.CategoryId = "22"; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
                video.Status = new VideoStatus();
                video.Status.PrivacyStatus = (file.Visibility + "").ToLower();
                using (fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                    videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
                    videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;
                    await videosInsertRequest.UploadAsync();
                    file.Status = "Uploaded";
                }
            }
            label.Content = "Done!";
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
