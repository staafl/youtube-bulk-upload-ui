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
            public string Status { get; set; }
            public string Length { get; set; }
            public string Title { get; set; }
            // todo: get from EXIF tags
            public string Description { get; set; }
            public string Visibility { get; set; }
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

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
            });

            var filePath = @"D:\gopro\GOPR0001-stabilized.mp4"; // Replace with path to actual movie file.
            var video = new Video();
            video.Snippet = new VideoSnippet();
            video.Snippet.Title = System.IO.Path.GetFileNameWithoutExtension(filePath);
            //video.Snippet.Description = "Default Video Description";
            //video.Snippet.Tags = new string[] { "tag1", "tag2" };
            video.Snippet.CategoryId = "22"; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
            video.Status = new VideoStatus();
            video.Status.PrivacyStatus = "unlisted"; // or "private" or "public"
            //using ()
            {
                fileStream = new FileStream(filePath, FileMode.Open);
                var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
                videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;
                videosInsertRequest.UploadAsync();
            }
        }
        FileStream fileStream;

        private void videosInsertRequest_ResponseReceived(Video obj)
        {
        }

        private void videosInsertRequest_ProgressChanged(IUploadProgress obj)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                progress.Value = (int)(100*(obj.BytesSent / fileStream.Length));
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
