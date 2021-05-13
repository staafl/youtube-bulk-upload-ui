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
using System.Runtime.InteropServices;
using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Google;

namespace YoutubeBulkUploadUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly ObservableCollection<FileModel> filesCollection = new ObservableCollection<FileModel>();
        readonly ObservableCollection<CategoryModel> categoriesCollection = new ObservableCollection<CategoryModel>();
        FileStream fileStream;
        FileModel currentUpload;
        YouTubeService youtubeService;
        string country;

        class CategoryModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
        class FileModel : INotifyPropertyChanged
        {
            string status;
            string url;
            public string File { get; set; }
            public string Status { get { return status; } set { status = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs (nameof(Status))); } }
            public string Url { get { return url; } set { url = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs (nameof(Url))); } }
            public string Length { get; set; }
            public string Title { get; set; }
            // todo: get from EXIF tags
            public string Description { get; set; }
            public VideoVisibility Visibility { get; set; }
            public Video Video { get; set; }
            public bool MadeForKids { get; set; }
            public CategoryModel Category { get; set; }
            public string Tags { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new
            {
                Files = filesCollection,
                Categories = categoriesCollection
            };
            dgv.AllowDrop = true;
            dgv.Drop += Dgv_Drop;

        }

        protected async override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            UserCredential credential;
            using (var stream =
                (ClientSecret.clientSecret != null ? new MemoryStream(Encoding.UTF8.GetBytes(ClientSecret.clientSecret)) : null) ??
                Assembly.GetExecutingAssembly().GetManifestResourceStream("YoutubeBulkUploadUI.client_secret.json")
                ?? (Stream)new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeUpload },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("YouTube.Auth.Store")).Result;
            }

            youtubeService = new YouTubeService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
                });

            // https://developers.google.com/youtube/v3/docs/videoCategories/list?apix_params=%7B%22part%22%3A%5B%22snippet%22%5D%2C%22regionCode%22%3A%22us%22%7D
            categoriesCollection.Add(null);
            if (File.Exists("categories.txt"))
            {
                foreach (var line in File.ReadAllLines("categories.txt"))
                {
                    var split = line.Split(new[] { ',' }, 2);
                    categoriesCollection.Add(
                        new CategoryModel
                        {
                            Id = split[0],
                            Name = split[1]
                        });
                }
            }
            else
            {
                string regionName;
                try
                {
                    string info = new WebClient().DownloadString("http://ipinfo.io");
                    regionName = Regex.Match(info, "\"country\": *\"([^\"]+)\"").Groups[1].Value;
                }
                catch
                {
                    regionName = "us";
                }

                //var ipInfo = jsonObject.Deserialize<IpInfo>(info);

                RegionInfo region = new RegionInfo(regionName);


                var categoriesRequest = youtubeService
                    .VideoCategories
                    .List("snippet");
                categoriesRequest.RegionCode = region.Name.ToLower();

                var categories = await categoriesRequest.ExecuteAsync();
                foreach (var category in categories.Items)
                {
                    categoriesCollection.Add(
                        new CategoryModel
                        {
                            Id = category.Id,
                            Name = category.Snippet.Title
                        });
                    File.AppendAllLines("categories.txt", new[] { category.Id + "," + category.Snippet.Title });
                }
            }

        }

        public class IpInfo
        {
            //country
            public string Country { get; set; }
        }

        private void videosInsertRequest_ResponseReceived(Video obj)
        {
            var file = filesCollection.FirstOrDefault(x => x.Title == obj.Snippet.Title);
            string error =
                obj.ProcessingDetails?.ProcessingFailureReason ??
                obj.Status?.RejectionReason ??
                obj.Status.FailureReason;
            if (error != null)
            {
                file.Status = "Error";
                file.Url = error;
            }
            else if (obj.Status?.UploadStatus != null)
            {
                file.Status = obj.Status.UploadStatus;
                if (obj.Id != null)
                {
                    file.Url = "https://youtube.com/watch?v=" + obj.Id;
                }
            }
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

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);
        class StrCmpLogicalWComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return StrCmpLogicalW(x, y);
            }
        }

        private void Dgv_Drop(object sender, DragEventArgs e)
        {
            // Shell IDList Array;DragImageBits;DragContext;DragSourceHelperFlags;InShellDragLoop;FileDrop;FileNameW;FileName

            if (e.Data.GetDataPresent("FileDrop"))
            {
                var files = (e.Data.GetData("FileDrop") as string[]);
                Array.Sort(files, new StrCmpLogicalWComparer());

                foreach (var file in files)
                {
                    var model = new FileModel
                    {
                        File = file,
                        Status = "Pending",
                        Visibility = VideoVisibility.Unlisted,
                        Title = "%f", //System.IO.Path.GetFileNameWithoutExtension(file),
                        Category = categoriesCollection.First(),
                        Description = ""
                    };
                    if (copy.IsChecked == true && filesCollection.Any())
                    {
                        var last = filesCollection.Last();
                        model.Visibility = last.Visibility;
                        model.Title = last.Title;
                        model.Description = last.Description;
                        model.Category = last.Category;
                        model.MadeForKids = last.MadeForKids;
                        model.Tags = last.Tags;
                    }
                    filesCollection.Add(model);
                }
            }
        }

        private async void but_upload_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            int ii = 0;
            foreach (var file in filesCollection)
            {
                currentUpload = file;
                file.Status = "Uploading...";
                ii += 1;
                label.Content = "Uploading video " + ii + ": " + System.IO.Path.GetFileName(file.File);
                var filePath = file.File;
                var video = new Video();
                video.Snippet = new VideoSnippet();
                video.Snippet.Title = file.Title = ReplacePatterns(file, file.Title, ii);
                video.Snippet.Description = file.Description = ReplacePatterns(file, file.Description, ii);
                video.Snippet.Tags = file.Tags?.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                video.Snippet.CategoryId = file.Category?.Id; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
                video.Status = new VideoStatus();
                video.Status.PrivacyStatus = (file.Visibility + "").ToLower();
                video.Status.MadeForKids = file.MadeForKids;
                file.Video = video;
                using (fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                    videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
                    videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;
                    var uploadProgress = await videosInsertRequest.UploadAsync();
                    file.Status = uploadProgress.Status.ToString();
                    if (uploadProgress.Exception != null)
                    {
                        file.Url = (uploadProgress.Exception as GoogleApiException)?.Error?.Message ?? uploadProgress.Exception.Message;
                    }
                    //file.Status = "Uploaded";
                    // TODO: are we finished at this point?
                    progress.Value = 100;
                }
            }
            label.Content = "Done!";

            using (var sw = new StreamWriter("upload.csv"))
            {
                sw.WriteLine("File,Status,Result,Title,Description,Category,Tags,Made for Kids");
                foreach (var file in filesCollection)
                {
                    var csvLine = new[]{
                            file.File,
                            file.Status,
                            file.Url,
                            file.Title,
                            file.Description,
                            file.Category?.Name,
                            string.Join(", ", file.Tags),
                            file.MadeForKids + ""
                        }.Select(x => (x + "").Replace("\"", "\"\"").Replace("\r", "").Replace("\n", " "));
                    sw.WriteLine("\"" + string.Join("\",\"", csvLine) + "\"");
                }
            }
            using (var sw = new StreamWriter("upload-list.log"))
            {
                foreach (var file in filesCollection)
                {
                    sw.WriteLine(file.Title + ": " + file.Url);
                }
            }
            Process.Start("notepad.exe", "upload-list.log");
        }

        private string ReplacePatterns(FileModel model, string what, int index)
        {
            return what
                .Replace("%i", index + "")
                .Replace("%c", filesCollection.Count + "")
                .Replace("%f", System.IO.Path.GetFileNameWithoutExtension(model.File));
        }
    }
}
