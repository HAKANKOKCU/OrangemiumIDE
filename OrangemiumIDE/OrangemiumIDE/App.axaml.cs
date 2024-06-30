using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrangemiumIDE.ViewModels;
using OrangemiumIDE.Views;
using TextMateSharp.Grammars;
using System.Net;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Linq;
using System.Net.Http;

namespace OrangemiumIDE;

public partial class App : Application
{
    public static DispatcherTimer ticker = new();

    public static HttpClient mainclient;
    public static int port = 0;
    public static Dictionary<string,extProcess> extexes = new();
    public static string serverurl = "";
    public static HttpListener _httpListener = new HttpListener();
    public static Random rnd = new Random();
    public static string platform = "";
    public static Dictionary<string,Dictionary<string,object>> extensions = new();
    public static Dictionary<TabControl,EditorTabControl> tabcps = new();
    public static Dictionary<string,object> settings = new();
    public static char[] schars = new char[] { ']', '[', '.','(',')','!','*','{','}', ' ', '\n', '\r', '\t', '\v', '/', '\\', ',', ';','<','>','=','&','|','?','0','1','2','3','4','5','6','7','8','9','-','+',':','%'};
    public static Dictionary<string,Dictionary<string, object>> themes = new();
    public static Dictionary<string, object> cthm = new();
	public static string cthmid = "";
    public static Dictionary<string,Bitmap> cachedicons = new();
    
    public static List<MainWindow> wins = new();
    RegistryOptions ro = new RegistryOptions(ThemeName.DarkPlus);

    public enum Level {
        Normal,
        Error,
        Warning,
        Info
    }
    public class DebugLogEventArgs : EventArgs
    {
        public Level type = Level.Normal;
        public string content = "";
    }

    public class extProcess {
        public Process process;
        public int port = 0;
        public string[] filetypes = [];
        public extProcess(Process proc) {
            process = proc;
        }
    }

    public class debugClass {
        public JObject debugger;

        public event EventHandler<DebugLogEventArgs> OnDebugLog;
        public event EventHandler OnComplete;
        public string dbgfile = "";
        public Process process = new Process();
        protected virtual void DebugLog(DebugLogEventArgs e)
        {
            EventHandler<DebugLogEventArgs> handler = OnDebugLog;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        

        public debugClass(JObject dbg,string file) {
            debugger = dbg;
            dbgfile = file;
        }
        
        public void terminate() {
            process.Kill();
        }

        public void restart() {
            try {process.CancelOutputRead();process.CancelErrorRead();}catch{}
            process.Kill();
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        public void disconnect() {
            process.Dispose();
        }

        public bool run() {
            if (debugger.ContainsKey("Compiler")) {
                JObject compiler = (JObject)debugger["Compiler"];
                JObject cmp;
                if (compiler.ContainsKey(platform)) {
                    cmp = (JObject)compiler[platform];
                }else {
                    if (compiler.ContainsKey("Global")) {
                        cmp = (JObject)compiler["Global"];
                    }else
                        return false;
                }
                string args = cmp["Args"].ToString().Replace("%FILE%",dbgfile);
                process = new Process();
                process.EnableRaisingEvents = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = cmp["Executable"].ToString();
                process.StartInfo.Arguments = args;
                process.Exited += (a,e) => {
                    
                    if (process.ExitCode == 0) {
                        process.Dispose();
                        runwithoutcompiling();
                    }else {
                        process.Dispose();
                        OnComplete?.Invoke(this, EventArgs.Empty);
                    }

                    
                };
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                //* Set your output and error (asynchronous) handlers
                process.OutputDataReceived += (a,e) => {
                    DebugLog(new DebugLogEventArgs() {content = e.Data + "",type = Level.Normal});
                };
                process.ErrorDataReceived += (a,e) => {
                    DebugLog(new DebugLogEventArgs() {content = e.Data + "",type = Level.Error});
                };
                //* Start process and handlers
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                

                return true;
            }else {
                return runwithoutcompiling();
            }

            
        }

        public bool runwithoutcompiling() {
            JObject dbg;
            if (debugger.ContainsKey(platform)) {
                dbg = (JObject)debugger[platform];
            }else {
                if (debugger.ContainsKey("Global")) {
                    dbg = (JObject)debugger["Global"];
                }else
                    return false;
            }
            string args = dbg["Args"].ToString().Replace("%FILE%",dbgfile);
            process = new Process();
            process.EnableRaisingEvents = true;
            process.StartInfo.FileName = dbg["Executable"].ToString();
            process.StartInfo.Arguments = args;
            process.Exited += (a,e) => {
                OnComplete?.Invoke(this, EventArgs.Empty);
            };
            
            if (dbg["DebugLogger"].ToString() == "InApp") {
                DebugLog(new DebugLogEventArgs() {content = "In case of input request, please focus the console window and then send input there.",type = Level.Info});
                //process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                //* Set your output and error (asynchronous) handlers
                process.OutputDataReceived += (a,e) => {
                    DebugLog(new DebugLogEventArgs() {content = e.Data + "",type = Level.Normal});
                };
                process.ErrorDataReceived += (a,e) => {
                    DebugLog(new DebugLogEventArgs() {content = e.Data + "",type = Level.Error});
                };
                //* Start process and handlers
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }else {
                //TODO
                process.Start();
            }
            return true;
        }
        public bool runwithoutdebugging() {
            JObject dbg;
            if (debugger.ContainsKey(platform)) {
                dbg = (JObject)debugger[platform];
            }else {
                if (debugger.ContainsKey("Global")) {
                    dbg = (JObject)debugger["Global"];
                }else
                    return false;
            }
            string args = dbg["Args"].ToString().Replace("%FILE%",dbgfile);
            process = new Process();
            process.StartInfo.FileName = dbg["Executable"].ToString();
            process.StartInfo.Arguments = args;
            process.Start();
            return true;
        }
    }   


    static void respond()
    {
        Task<HttpListenerContext> ctx = _httpListener.GetContextAsync();
        ctx.ContinueWith((Task<HttpListenerContext> c) =>
        {
            if (c.IsCompletedSuccessfully) {
                var context = c.Result;
                string url = context.Request.Url.ToString().Replace(serverurl,"");
                Console.WriteLine(url);
                if (url == "openfile") {
                    var body = new StreamReader(context.Request.InputStream).ReadToEnd();
                    //try {
                        var a = JsonConvert.DeserializeObject<Dictionary<string,object>>(body);
                        JArray args = (JArray)a["args"];
                        foreach (object f in args) {
                            var fil = f.ToString();
                            if (File.Exists(fil)) {
                                Dispatcher.UIThread.Post(() => {
                                    wins[0].openfile(wins[0].newtab(),fil);
                                    wins[0].Show();
                                    wins[0].Activate();
                                });
                            }
                        }
                        
                    //}catch {}
                }
                context.Response.OutputStream.Write([], 0, 0);
                context.Response.KeepAlive = false; 
                context.Response.Close(); 
                Console.WriteLine("Respone given to a request.");
                respond();
            }
        });
    }
        
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        mainclient = new HttpClient();
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            platform = "Windows";
        }else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            platform = "MacOS";
        }else {
            platform = "Linux";
        }
        if (!Directory.Exists(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToString(),"/OrangemiumIDE"))) {
            Directory.CreateDirectory(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToString(),"/OrangemiumIDE"));
        }
        Directory.SetCurrentDirectory(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToString(),"/OrangemiumIDE"));
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.Abbys);
            thm["name"] = "Abbys";
            themes["BULITIN_Abbys"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.Dark);
            thm["name"] = "Dark";
            thm["txcolor"] = Brushes.White;
            thm["bgcolor"] = Brushes.Black;
            themes["BULITIN_Dark"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.DarkPlus);
            thm["name"] = "Dark Plus";
            thm["bgcolor"] = Brushes.Black;
            thm["txcolor"] = Brushes.White;
            themes["BULITIN_DPlus"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.DimmedMonokai);
            thm["name"] = "Dimmed Monokai";
            themes["BULITIN_DimMonokai"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.HighContrastDark);
            thm["name"] = "High Contrast Dark";
            thm["bgcolor"] = Brushes.Black;
            thm["txcolor"] = Brushes.White;
            themes["BULITIN_HCD"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.HighContrastLight);
            thm["name"] = "High Contrast Light";
            thm["txcolor"] = Brushes.Black;
            thm["bgcolor"] = Brushes.White;
            themes["BULITIN_HCL"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.KimbieDark);
            thm["name"] = "Kimbie Dark";
            thm["bgcolor"] = Brushes.Black;
            thm["txcolor"] = Brushes.White;
            themes["BULITIN_KimbieD"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.Light);
            thm["name"] = "Light";
            thm["txcolor"] = Brushes.Black;
            thm["bgcolor"] = Brushes.White;
            themes["BULITIN_Light"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.LightPlus);
            thm["name"] = "Light Plus";
            thm["txcolor"] = Brushes.Black;
            thm["bgcolor"] = Brushes.White;
            themes["BULITIN_LPlus"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.Monokai);
            thm["name"] = "Monokai";
            themes["BULITIN_Monokai"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.QuietLight);
            thm["name"] = "Quiet Light";
            themes["BULITIN_QLight"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.Red);
            thm["name"] = "Red";
            thm["txcolor"] = Brushes.White;
            thm["bgcolor"] = Brushes.DarkRed;
            themes["BULITIN_Red"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.SolarizedDark);
            thm["name"] = "Solarized Dark";
            thm["txcolor"] = Brushes.White;
            thm["bgcolor"] = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromRgb(41, 33, 0));
            themes["BULITIN_SDark"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.SolarizedLight);
            thm["name"] = "Solarized Light";
            thm["txcolor"] = Brushes.Black;
            thm["bgcolor"] = Brushes.Wheat;
            themes["BULITIN_SLight"] = thm;
        }
        {
            Dictionary<string, object> thm = new();
            thm["theme"] = ro.LoadTheme(ThemeName.TomorrowNightBlue);
            thm["name"] = "Tomorrow Night Blue";
            thm["txcolor"] = Brushes.White;
            thm["bgcolor"] = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromRgb(0,7,42));
            themes["BULITIN_TNB"] = thm;
        }

        if (!File.Exists("./DATA.JSON"))
        {
            File.Create("./DATA.JSON").Close();
        }
        settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText("./DATA.JSON"));
		if (settings == null)
		{
			settings = new Dictionary<string, object>();
		}
		if (!settings.ContainsKey("theme"))
		{
            if (this.PlatformSettings.GetColorValues().ThemeVariant  == PlatformThemeVariant.Dark) {
                cthm = themes["BULITIN_DPlus"];
                cthmid = "BULITIN_DPlus";
            }else {
                cthm = themes["BULITIN_LPlus"];
                cthmid = "BULITIN_LPlus";
            }
        }else
		{
			cthm = themes[settings["theme"].ToString()];
			cthmid = settings["theme"].ToString();

        }
        if (!settings.ContainsKey("IconPack"))
		{
            settings["IconPack"] = "";
        }
        if (!settings.ContainsKey("WordWrap"))
		{
            settings["WordWrap"] = false;
        }
        if (!settings.ContainsKey("ShowLineNumbers"))
		{
            settings["ShowLineNumbers"] = true;
        }
        if (!settings.ContainsKey("enableFallbackAutocomplete"))
		{
            settings["enableFallbackAutocomplete"] = true;
        }
        if (!settings.ContainsKey("sidebarSize"))
		{
            settings["sidebarSize"] = 330.0;
        }
        if (!settings.ContainsKey("extensions"))
		{
            settings["extensions"] = new List<string>();
        }
        if (!settings.ContainsKey("isMaximized"))
		{
            settings["isMaximized"] = true;
        }

        
        if (!Directory.Exists("./extensions")) {
            Directory.CreateDirectory("./extensions");
        }

        if (!Directory.Exists("./instances")) {
            Directory.CreateDirectory("./instances");
        }

        string[] exts = Directory.GetDirectories("./extensions");
        foreach (string ext in exts) {
            Dictionary<string,object> extinfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(Path.Join(ext,"/extension.json")));
            extensions[ext] = extinfo;
            if (((JArray)settings["extensions"]).Any(t => t.Value<string>() == ext)) {
                if (extinfo.ContainsKey("ExecutableInteractions")) {
                    var ei = (JObject)extinfo["ExecutableInteractions"];
                    if (ei.ContainsKey(platform)) {
                        var exs = (JObject)ei[platform];
                        foreach (var ex in exs) {
                            var e = (JObject)ex.Value;
                            int extport = rnd.Next(10000, 20000);
                            Process process = new();
                            process.EnableRaisingEvents = true;
                            process.StartInfo.CreateNoWindow = true;
                            process.StartInfo.FileName = Path.Join(ext,ex.Key);
                            process.StartInfo.Arguments = port + " " + extport + " " + ex.Key;
                            process.Exited += (a,e) => {Console.WriteLine("Extension Exited... Exit code: " + process.ExitCode);};
                            //process.StartInfo.UseShellExecute = false;
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.RedirectStandardError = true;
                            //* Set your output and error (asynchronous) handlers
                            process.OutputDataReceived += (a,e) => {
                                //DebugLog(new DebugLogEventArgs() {content = e.Data + "",type = Level.Normal});
                                Console.WriteLine(e.Data);
                            };
                            process.ErrorDataReceived += (a,e) => {
                                //DebugLog(new DebugLogEventArgs() {content = e.Data + "",type = Level.Error});
                                Console.WriteLine(e.Data);
                            };
                            process.Start();
                            if (e.ContainsKey("codeTools")) {
                                string[] extens = e["codeTools"].ToString().Split(",");
                                foreach (string ek in extens) {
                                    extProcess pr = new(process);
                                    pr.port = extport;
                                    pr.filetypes = extens;
                                    extexes[ek] = pr;
                                }
                            }
                            if (e.ContainsKey("RunBG")) {
                                process.Start();
                            }
                        }
                    }
                }
            }
        }

        Process[] ins = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
        if (ins.Length < 2) {
            string[] insts = Directory.GetFiles("./instances");
            foreach (string file in insts) {
                File.Delete(file);
            }
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            string[] insts = Directory.GetFiles("./instances");
            if (insts.Length > 0) {
                desktop.MainWindow = new instancesWindow(desktop);
            }else {
                desktop.MainWindow = new MainWindow(desktop.Args);
                createinstance(desktop);
            }
        }
        /*else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }*/

        

        base.OnFrameworkInitializationCompleted();
    }

    public static void OpenBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
        else
        {
			//...
		}
    }

    public static async Task<List<MyCompletionData>> getSuggestions(tabcont tabcontent) {
        List<MyCompletionData> dt = new();
        if (extexes.ContainsKey(Path.GetExtension(tabcontent.filepath).ToLower()) && !extexes[Path.GetExtension(tabcontent.filepath).ToLower()].process.HasExited) {
            var extexe = extexes[Path.GetExtension(tabcontent.filepath).ToLower()];
            StringContent sc = new(JsonConvert.SerializeObject(new { fileContent = tabcontent.editor.Text, file = tabcontent.filepath,cursor = tabcontent.editor.SelectionStart - 1 }));
            var resp = await mainclient.PostAsync("http://localhost:" + extexe.port + "/autocomplete", sc);
            if (resp != null) {
                var content = await resp.Content.ReadAsStringAsync();
                //Console.WriteLine(content);
                var a = JsonConvert.DeserializeObject<List<Dictionary<string,object>>>(content);
                foreach (var sgs in a) {
                    dt.Add(new MyCompletionData(sgs["content"].ToString(),sgs["description"].ToString(),Int32.Parse(sgs["addindex"].ToString())));
                }
            }
        }else if ((bool)settings["enableFallbackAutocomplete"]) {
            try {
                List<string> suggestions = new();
                bool istring = false;
                
                foreach (string i in tabcontent.editor.Text.Split(schars, StringSplitOptions.RemoveEmptyEntries)) {
                    if (i.Contains("\"") ||i.Contains("'")) {
                        istring = !istring;
                    }else if (istring == false) {
                        if (!suggestions.Contains(i)) {
                            suggestions.Add(i);
                            
                        }
                    }
                }
                
                string wstart = "";
                int wi = 0;
                int index = tabcontent.editor.SelectionStart - 1;
                while(true) {
                    if (schars.Contains(tabcontent.editor.Text[index])) {
                        break;
                    }else {
                        wstart = tabcontent.editor.Text[index] + wstart;
                        wi += 1;
                    }
                    index -= 1;
                }
                wstart = wstart.ToLower();
                
                foreach (string s in suggestions) {
                    if (s.ToLower().StartsWith(wstart) && s != wstart) {
                        dt.Add(new MyCompletionData(s,"Unknown stament/varailable from splitted code that is from this file: \n" + s,wi));
                    }
                }
            }catch {};
        }

        return dt;
    }

    public static async Task<Dictionary<string,object>> checkDoc(tabcont tabcontent) {
        List<docProb> dt = new();
        if (extexes.ContainsKey(Path.GetExtension(tabcontent.filepath).ToLower()) && !extexes[Path.GetExtension(tabcontent.filepath).ToLower()].process.HasExited) {
            var extexe = extexes[Path.GetExtension(tabcontent.filepath).ToLower()];
            StringContent sc = new(JsonConvert.SerializeObject(new { fileContent = tabcontent.editor.Text, file = tabcontent.filepath,cursor = tabcontent.editor.SelectionStart - 1 }));
            var resp = await mainclient.PostAsync("http://localhost:" + extexe.port + "/checkdoc", sc);
            if (resp != null) {
                var content = await resp.Content.ReadAsStringAsync();
                //Console.WriteLine(content);
                var a = JsonConvert.DeserializeObject<Dictionary<string,object>>(content);
                var probls = (JArray)a["Problems"];
                foreach (var prob in probls) {
                    var problem = (JObject)prob;
                    docProb prb = new() {
                        level = problem["Level"].ToString(), 
                        content = problem["Content"].ToString(),
                        line = Int32.Parse(problem["Line"].ToString()),
                        charind = Int32.Parse(problem["Char"].ToString()),
                        length = Int32.Parse(problem["Length"].ToString()),
                        column = Int32.Parse(problem["Column"].ToString())
                    };
                    dt.Add(prb);
                }
            }
        }
        Dictionary<string,object> res = new();
        res["Problems"] = dt;
        return res;
    }
    
    public static void createinstance(IClassicDesktopStyleApplicationLifetime desktop) {
        port = rnd.Next(10000, 20000);
        serverurl = "http://localhost:" + port + "/";
        _httpListener.Prefixes.Add(serverurl);
        _httpListener.Start();
        Console.WriteLine("Server started. " + serverurl);
        respond();
        File.WriteAllText("./instances/" + port + ".OIDE","");

        ticker.Tick += (e,a) => {
            List<Process> remaingprocs = new();
            foreach (var x in extexes) {
                if (!remaingprocs.Contains(x.Value.process)) remaingprocs.Add(x.Value.process);
            }
            foreach (MainWindow win in wins) {
                foreach (EditorTabControl etc in win.alltc) {
                    foreach (TabItem titem in etc.tabControl.Items) {
                        if (titem.Content is tabcont) {
                            tabcont tab = (tabcont)titem.Content;
                            string key = Path.GetExtension(tab.filepath).ToLower();
                            if (extexes.ContainsKey(key)) {
                                Process proc = extexes[key].process;
                                if (proc.HasExited) {proc.Start();proc.BeginErrorReadLine();proc.BeginOutputReadLine();}
                                remaingprocs.Remove(extexes[key].process);
                            }
                        } 
                    }
                }
            }
            foreach (var x in remaingprocs) {
                if (!x.HasExited) {try {x.CancelOutputRead();x.CancelErrorRead();}catch{}Console.WriteLine("Killing " + x.ProcessName);x.Kill();}
            }
        };
        ticker.Interval = TimeSpan.FromSeconds(1);
        ticker.Start();

        desktop.Exit += (e,a) => {
            _httpListener.Stop();
            foreach (var x in extexes) {
                if (!x.Value.process.HasExited) {try {x.Value.process.CancelOutputRead();x.Value.process.CancelErrorRead();}catch{}Console.WriteLine("Killing " + x.Value.process.ProcessName);x.Value.process.Kill();}
            }
            File.Delete("./instances/" + port + ".OIDE");
        };
    }
}
public class docProb {
    public string content = "";
    public int line = 0;
    public int charind = 0;
    public int length = 0;
    public int column = 0;
    public string level = "N";
}