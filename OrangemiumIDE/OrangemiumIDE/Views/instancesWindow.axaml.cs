using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Layout;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using static OrangemiumIDE.App;

namespace OrangemiumIDE.Views;

public partial class instancesWindow : Window
{
    HttpClient mainclient;
    public instancesWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        mainclient = new HttpClient();
        InitializeComponent();
        string[] insts = Directory.GetFiles("./instances");
        foreach (string inst in insts) {
            Button ibtn = new() {Content = Path.GetFileName(inst), HorizontalAlignment = HorizontalAlignment.Stretch};
            lst.Children.Add(ibtn);
            ibtn.Click += (e,a) => {
                StringContent sc = new(JsonConvert.SerializeObject(new { args = desktop.Args }));
                var task = mainclient.PostAsync("http://localhost:" + Path.GetFileName(inst).Split(".")[0] + "/openfile", sc);
                task.ContinueWith((Task<HttpResponseMessage> httpTask) =>
                {
                    try
                    {
                        Task<string> task = httpTask.Result.Content.ReadAsStringAsync();
                        Task continuation = task.ContinueWith(t =>
                        {
                            if (t.IsCompletedSuccessfully)
                            {
                                Dispatcher.UIThread.Post(() =>
                                {Close();});
                            }
                        });
                    }catch {}
                });
            };
        }
        {
            Button ibtn = new() {Content = "Create new instance", HorizontalAlignment = HorizontalAlignment.Stretch};
            lst.Children.Add(ibtn);
            ibtn.Click += (e,a) => {
                new MainWindow(desktop.Args).Show();
                createinstance(desktop);
                Close();
            };
        }
    }
}