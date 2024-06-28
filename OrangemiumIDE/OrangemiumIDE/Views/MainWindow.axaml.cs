using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Avalonia.Styling;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Controls.Primitives;
using static OrangemiumIDE.App;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit.Rendering;
namespace OrangemiumIDE.Views;

public partial class MainWindow : Window
{
    public List<EditorTabControl> alltc = new();
    public TabItem? selectedtab = null;
    EditorTabControl? currenttc = null;
    RegistryOptions ro = new RegistryOptions(ThemeName.DarkPlus);
    private CompletionWindow cv;
    ArrayList alfolders = new();
    Dictionary<string, TreeViewItem> fa = new();
	
    bool controlpressed = false;
	
	
    public MainWindow(String[]? args)
    {
        InitializeComponent();

        wins.Add(this);
        Closed += (e,a) => {
            wins.Remove(this); 
        };
        
        if ((bool)settings["isMaximized"] == true) {
            this.WindowState = WindowState.Maximized;
        }

        mv.bx.Click += (e,a) => {
            mv.btmarea.IsVisible = !mv.btmarea.IsVisible;
        };
        mv.btmarea.cbtn.Click += (e,a) => {
            mv.btmarea.IsVisible = false;
        };
        
        //loaddata();

        Image dico = new() {Source = geticon("playarrow")};
		mv.debgsd.sdbg.Content = dico;

        mv.sidearea.Width = (double)settings["sidebarSize"];
        mv.tg.Children.Add(createTabControl());
        mv.wm.SubmenuOpened += (e,a) => {
            mv.wins.Items.Clear();
            int i = 0;
            foreach (MainWindow win in wins) {
                MenuItem mi = new();
                mi.Header = i + " - " + win.Title + (win == this ? " (CURRENT)" : "");
                mi.Click += (f,g) => {
                    win.Show();
                    win.Activate();
                };
                mv.wins.Items.Add(mi);
                i++;
            }
        };
        updatetabsgrid();
        

        KeyDown += async void (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.LeftAlt)
            {
				if (selectedtab.Content is tabcont) {
					((tabcont)(selectedtab).Content).Focus();
					e.Handled = true;
				}
            }
            if (controlpressed)
            {
                if (e.Key == Avalonia.Input.Key.Z)
                {
                    tabcont tab = (tabcont)(selectedtab).Content;
                    tab.editor.Undo();
                }
                if (e.Key == Avalonia.Input.Key.R)
                {
                    tabcont tab = (tabcont)(selectedtab).Content;
                    tab.editor.Redo();
                }
                if (e.Key == Avalonia.Input.Key.T)
                {
                    newtab();
                }
                if (e.Key == Avalonia.Input.Key.O)
                {
                    openfilefromdialog();
                }
                
                if (e.Key == Avalonia.Input.Key.S)
                {
                    tabcont tb = (tabcont)(selectedtab).Content;
                    savefile(tb);
                }
            }
            
            if (e.Key == Avalonia.Input.Key.LeftCtrl)
            {
                controlpressed = true;
            }
        };
        KeyUp += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.LeftCtrl)
            {
                controlpressed = false;
            }
        };
        mv.nw.Click += async void (e, a) => {
            new MainWindow([]).Show();
        };
        mv.fv.af.Click += async void (e, a) => {
            var dialog = new OpenFolderDialog();
            var dirpath = await dialog.ShowAsync(this);
            openfolder(dirpath);
        };
        mv.fv.atfp.Click += (e, a) => {
            try
            {
                TabItem tab = selectedtab;
                tabcont tb = (tabcont)tab.Content;
                string dirpath = Directory.GetParent(tb.filepath).FullName;
                openfolder(dirpath);
            }
            catch { }
        };
        foreach (Language lang in ro.GetAvailableLanguages())
        {
            MenuItem mi = new() { Header = lang.Aliases[0] };
            mi.Click += (e,a) => {
                tabcont tab = (tabcont)(selectedtab).Content;
                tab.tmi.SetGrammar(ro.GetScopeByLanguageId(lang.Id));
				mv.codetype.Content = lang.Aliases[0];
            };
            mv.langs.Items.Add(mi);
        }
        mv.und.Click += (e, a) => {
            tabcont tab = (tabcont)(selectedtab).Content;
            tab.editor.Undo();
        };
		/*mv.fm.Click += (e, a) => {
            tabcont tab = (tabcont)(selectedtab).Content;
            tab.editor.SearchPanel.Open();
        };*/
        mv.red.Click += (e, a) => {
            tabcont tab = (tabcont)(selectedtab).Content;
            tab.editor.Redo();
        };
       
		ToggleButton filb = addsidebaritem("filecopy","Files",mv.fv);
        mv.vvaricons.Children.Add(filb);
        ToggleButton dbg = addsidebaritem("bug","Debugging",mv.debgsd);
        mv.vvaricons.Children.Add(dbg);
        selectsidebaritem(0,filb);

		bool closin = false;
		Closing += async (s, e) => { //TODO, BUGGY
            try {
                savedata();
                foreach (EditorTabControl tc in alltc)
                foreach (TabItem i in tc.tabControl.Items) {
                    if (i.Content is tabcont) {
                        tabcont abb = (tabcont)i.Content;
                        if (abb.issaved == false) {
                            
                            if (!closin) {
                                e.Cancel = true;
                                var task = MessageBox.Show(this, "Do you want to save all opened files?", "Save?", MessageBox.MessageBoxButtons.YesNoCancel);
                                task.ContinueWith((Task<MessageBox.MessageBoxResult> a) =>
                                {
                                    MessageBox.MessageBoxResult r = a.Result;
                                    
                                    if (r == MessageBox.MessageBoxResult.Yes) {
                                        Dispatcher.UIThread.Post(() => {
                                            foreach (EditorTabControl tc in alltc)
                                            foreach (TabItem i in tc.tabControl.Items) {
                                                if (i.Content is tabcont) {
                                                    tabcont abb = (tabcont)i.Content;
                                                    savefile(abb);
                                                }
                                            }
                                            //closin = true;
                                            //e.Cancel = false;
                                            //Close();
                                        });
                                    }
                                    if (r == MessageBox.MessageBoxResult.No) {
                                        closin = true;
                                        e.Cancel = false;
                                        Dispatcher.UIThread.Post(() => Close());
                                    }
                                    if (r == MessageBox.MessageBoxResult.Cancel) {
                                        closin = false;
                                    }
                                });
                            }
                            break;
                        }
                    }
                }
            }catch {}
            
			
			//if (!closin) {
			//	bool iscancelled = false;
			//	foreach (TabItem i in mv.tabsmn.Items) {
			//		tabcont abb = (tabcont)i.Content;
			//		if (abb.issaved == false) {
			//			e.Cancel = true;
			//			//e.Cancel = true;
			//			
			//			
			//		}
			//	}
			//	if (!iscancelled) {
			//		closin = true;
			//		Close();
			//	}
			//}
		};
		
	
        bool openhome = true;
		if (args != null) {
            foreach (string fil in args) {
                if (File.Exists(fil)) {
                    openfile(newtab(),fil);
                    openhome = false;
                }
            }
        }
        if (openhome) hometab();
        mv.ww.IsChecked = (bool)settings["WordWrap"];
         mv.ww.IsCheckedChanged +=(e,a) => {
            settings["WordWrap"] = mv.ww.IsChecked;
            foreach (EditorTabControl ct in alltc)
            foreach (TabItem tb in ct.tabControl.Items)
            {
                if (tb.Content is tabcont) {
                    tabcont tab = (tabcont)tb.Content;
                    tab.editor.WordWrap = (bool)settings["WordWrap"];
                }
            }
         };
//KeyValuePair<string, Dictionary<string, object>> tn in themes
//Dictionary<string, object> tn in themes.Values
        foreach (KeyValuePair<string, Dictionary<string, object>> t in themes)
        {
			var tn = t.Value;
            MenuItem mi = new() { Header = tn["name"].ToString() };
            mi.Click += (e, a) => {
                foreach (EditorTabControl ct in alltc)
                foreach (TabItem tb in ct.tabControl.Items)
                {
                    if (tb.Content is tabcont) {
                        tabcont tab = (tabcont)tb.Content;
                        cthm = tn;
                        cthmid = t.Key;
                        tab.tmi.SetTheme((IRawTheme)tn["theme"]);
                        if (tn.ContainsKey("bgcolor"))
                        {
                            tab.editor.Background = (Avalonia.Media.Immutable.ImmutableSolidColorBrush)tn["bgcolor"];
                        }
                        else
                        {
                            tab.editor.Background = Brushes.Transparent;
                        }
                        if (tn.ContainsKey("txcolor"))
                        {
                            tab.editor.Foreground = (Avalonia.Media.Immutable.ImmutableSolidColorBrush)tn["txcolor"];
                        }
                        else
                        {
                            tab.editor.Foreground = Foreground;
                        }
                    }
                }
            };
            mv.thms.Items.Add(mi);
        }
        mv.nt.Click += (e, a) =>
        {
            newtab();
        };
        mv.ons.Click += (e, a) =>
        {
            EditorTabControl tc = createTabControl();
            ContextMenu ctx = new();
            tc.ContextMenu = ctx;
            MenuItem csv = new() {Header = "Close this view (Moves tabs to main tabview)"};
            ctx.Items.Add(csv);
            csv.Click += (e,a) => {
                for (int i = 0; i < tc.tabControl.Items.Count; i++) {
                    TabItem t = (TabItem)tc.tabControl.Items[0];
                    tc.tabControl.Items.Remove(t);
                    alltc[0].tabControl.Items.Add(t);
                    
                }
                mv.tg.ColumnDefinitions.RemoveAt(mv.tg.Children.IndexOf(tc));
                mv.tg.Children.Remove(tc);
                
            };
            mv.tg.Children.Add(tc);
            updatetabsgrid();
        };
        mv.wcp.Click += (e, a) =>
        {
            hometab();
        };
        mv.st.Click += (e, a) =>
        {
            settingstab();
        };
        mv.quit.Click += (e, a) =>
        {
            Close();
        };
        mv.cct.Click += (e, a) =>
        {
            ctab(selectedtab);
        };
		mv.sfa.Click += async void (e, a) => {
			tabcont tb = (tabcont)(selectedtab).Content;
            savefile(tb,true);
        };
        mv.sf.Click += async void (e, a) => {
            tabcont tb = (tabcont)(selectedtab).Content;
            savefile(tb,false);
        };
        mv.of.Click += async void (e, a) =>
        {
            openfilefromdialog();
        };
    }

    async void openfilefromdialog() {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(this);

        // Start async operation to open the dialog.
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            openfile(selectedtab, files[0].Path.AbsolutePath);
        }
    }

    
    void updatetabsgrid() {
        //mv.tg.ColumnDefinitions.Clear();
        int i = 0;
        
        foreach (EditorTabControl itm in mv.tg.Children) {

            if (mv.tg.ColumnDefinitions.Count - 1 < i) {
                ColumnDefinition cd = new ColumnDefinition(1,GridUnitType.Star);
                mv.tg.ColumnDefinitions.Add(cd);
            }
            Grid.SetColumn(itm, i);
            i++;
            
        }
    }

    void utab() {
        if ((selectedtab).Content is tabcont) {
            tabcont tab = (tabcont)selectedtab.Content;
            if (tab.lang != null) {
                mv.codetype.Content = tab.lang.Aliases[0];
            }else {
                mv.codetype.Content = "Unknown";
            }
            refocuscurrentfile();
            inittitle();
            mv.of.IsEnabled = true;
            mv.sf.IsEnabled = true;
            mv.sfa.IsEnabled = true;
            mv.langs.IsEnabled = true;
            mv.thms.IsEnabled = true;
            mv.edt.IsEnabled = true;
            mv.vv.IsEnabled = true;
            mv.codetype.IsVisible = true;
        }else {
            //mv.of.IsEnabled = false;
            mv.sf.IsEnabled = false;
            mv.sfa.IsEnabled = false;
            mv.langs.IsEnabled = false;
            mv.thms.IsEnabled = false;
            mv.edt.IsEnabled = false;
            mv.codetype.IsVisible = false;
            mv.vv.IsEnabled = false;
        }
    }
    void tabchanged(bool actualchange = true) {
        inittitle();
        
        mv.btmarea.probstack.Children.Clear();
        if (actualchange) mv.debgsd.dbgl.Children.Clear();
        if (selectedtab != null && selectedtab.Content is tabcont) {
            tabcont tb = (tabcont)selectedtab.Content;
            foreach (var problem in tb.problems) {
                problemli prbbtn = new() ;//{ + problem.content};
                prbbtn.ico.Source = geticon((problem.level == "E" ? "error" : "warning"),false);
                prbbtn.loclabel.Content = problem.level + "(" + problem.line + "," + problem.column + ")";
                prbbtn.msg.Text = problem.content;
                prbbtn.btn.Click += (e,a) => {
                    tb.editor.ScrollToLine(problem.line);
                    tb.editor.Select(problem.charind,problem.length + 1);
                };
                mv.btmarea.probstack.Children.Add(prbbtn);
            }
            if (actualchange) {
                foreach (KeyValuePair<string,Dictionary<string,object>> ext in extensions) {
                    if (((JArray)settings["extensions"]).Any(t => t.Value<string>() == ext.Key) && ext.Value.ContainsKey("Debuggers")) {
                        foreach (var dbg in ((JObject)ext.Value["Debuggers"])) {
                            bool avail = true;
                            JObject dbgr = (JObject)dbg.Value;
                            string[] c = dbgr["Available"].ToString().Split("|");
                            foreach (string rule in c) {
                                if (!avail) break;
                                string[] rulekv = rule.Split("=");
                                if (rulekv[0] == "FileType") {
                                    string[] filetps = rulekv[1].Split(",");
                                    avail = filetps.Contains(Path.GetExtension(tb.filepath));
                                }
                                if (rulekv[0] == "Platform") {
                                    string[] ps = rulekv[1].Split(",");
                                    avail = ps.Contains(platform);
                                }
                            }
                            if (avail) {
                                
                                debugoptsli dli = new();
                                dli.dbgname.Content = dbgr["Name"].ToString();
                                Image dico = new() {Source = geticon("playarrow")};
                                dli.startdbg.Content = dico;
                                mv.debgsd.dbgl.Children.Add(dli);
                                void rundbg(bool cmp = true) {
                                    mv.btmarea.IsVisible = true;
                                    DispatcherTimer consoleupdater = new() {Interval = TimeSpan.FromMilliseconds(100)};
                                    string consolecontent = "";
                                    TabItem ti = new() {Header = "Debug - " + Path.GetFileName(tb.filepath)};
                                    DockPanel dpd = new();

                                    StackPanel ddp = new() {Orientation = Orientation.Horizontal};
                                    Button termbutton = new() {Padding = new Thickness(0), Width=20, Height=20, Content = new Image() {Source = geticon("stop")}};
                                    ddp.Children.Add(termbutton);

                                    {
                                        ToolTip tp = new();
                                        tp.Content = "Kill";
                                        termbutton.SetValue(ToolTip.TipProperty,tp);
                                    }
                                    
                                    Button restartbutton = new() {Padding = new Thickness(0), Width=20, Height=20, Content = new Image() {Source = geticon("replay")}};
                                    ddp.Children.Add(restartbutton);

                                    {
                                        ToolTip tp = new();
                                        tp.Content = "Restart";
                                        restartbutton.SetValue(ToolTip.TipProperty,tp);
                                    }

                                    Button disconnectbutton = new() {Padding = new Thickness(0), Width=20, Height=20, Content = new Image() {Source = geticon("close")}};
                                    ddp.Children.Add(disconnectbutton);

                                    {
                                        ToolTip tp = new();
                                        tp.Content = "Disconnect";
                                        disconnectbutton.SetValue(ToolTip.TipProperty,tp);
                                    }

                                    dpd.Children.Add(ddp);
                                    
                                    DockPanel.SetDock(ddp,Dock.Top);

                                    ScrollViewer swc = new() {HorizontalAlignment = HorizontalAlignment.Stretch};
                                    SelectableTextBlock tbc = new() {TextWrapping = TextWrapping.Wrap,HorizontalAlignment = HorizontalAlignment.Stretch};
                                    swc.Content = tbc;
                                    dpd.Children.Add(swc);
                                    ti.Content = dpd;

                                    mv.btmarea.consoletc.Items.Add(ti);
                                    mv.btmarea.consoletc.SelectedItem = ti;

                                    debugClass d = new(dbgr, tb.filepath);
                                    termbutton.Click += (e,a) => {
                                        d.terminate();
                                    };
                                    restartbutton.Click += (e,a) => {
                                        d.restart();
                                    };
                                    disconnectbutton.Click += (e,a) => {
                                        d.disconnect();
                                        consoleupdater.Stop();
                                        //consoleupdater = null;
                                        mv.btmarea.consoletc.Items.Remove(ti);
                                    };
                                    d.OnDebugLog += (a,e) => {
                                        //Dispatcher.UIThread.Post(() => {
                                            //MessageBox.Show(this, e.content,"Debug Log (Level: " + e.type + ")", MessageBox.MessageBoxButtons.Ok);
                                            consolecontent += "\n" + e.type + ": " + e.content;
                                            //swc.ScrollToEnd();
                                        //});
                                    };
                                    d.OnComplete += (a,e) => {
                                        //Dispatcher.UIThread.Post(async void () => {
                                            consolecontent += "\nProcesss Exited. Click the disconnect button to close this tab.";
                                            //var x = await MessageBox.Show(null, "Complete!","Debug", MessageBox.MessageBoxButtons.Ok);
                                            //mv.btmarea.consoletc.Items.Remove(ti);
                                        //});
                                    };
                                    consoleupdater.Tick += (e,a) => {
                                        Dispatcher.UIThread.Post(() => {
                                            if (tbc.Text != consolecontent) {
                                                tbc.Text = consolecontent;
                                                swc.ScrollToEnd();
                                            }
                                        });
                                    };
                                    consoleupdater.Start();
                                    if (cmp)
                                    d.run();
                                    else
                                    d.runwithoutcompiling();
                                }
                                dli.startdbg.Click += (e,a) => {
                                    rundbg(true);
                                };
                                dli.rwc.Click += (e,a) => {
                                    rundbg(false);
                                };
                                dli.rwd.Click += (e,a) => {
                                    debugClass d = new(dbgr, tb.filepath);
                                    d.runwithoutdebugging();
                                };
                            }
                        }
                    }
                }
            }
        }
        
        
    }
    
    EditorTabControl createTabControl(bool isperma = true) {
        EditorTabControl tc = new();
        tabcps[tc.tabControl] = tc;
        tc.Focusable = true;

        bool isdragging = false;
        double initalx = 0;

        tc.sizePanel.PointerPressed += (a,e) => {
            isdragging = true;
            tc.sizePanel.Background = new SolidColorBrush(this.PlatformSettings.GetColorValues().AccentColor1);
            var point = e.GetCurrentPoint(null);
            var x = point.Position.X;
            initalx = x;
        };
        tc.sizePanel.PointerReleased += (a,e) => {isdragging = false;tc.sizePanel.Background = Brushes.Transparent;settings["sidebarSize"] = mv.sidearea.Width;};
        
        tc.sizePanel.PointerMoved += (a,e) => {
            if (isdragging) {
                var point = e.GetCurrentPoint(null);
                var x = point.Position.X;
                if (alltc[0] == tc) {
                    if (x > 50) {
                        mv.sidearea.Width = x;
                    }else {
                        mv.sidearea.Width = 50;
                    }
                    
                    
                }else {
                    Grid g = (Grid)tc.Parent;
                    int ix = mv.tg.Children.IndexOf(tc);
                    double v = 1 - (((x - mv.sidearea.Width) / (Width - mv.sidearea.Width)) * 2);
                    try {
                        mv.tg.ColumnDefinitions[ix] = new ColumnDefinition(v,GridUnitType.Star);
                    }
                    catch{}
                }
            }
        };

        void update() {
            updatetabsgrid();
            if (!isperma) {
                if (tc.tabControl.Items.Count == 0) {
                    tc.IsVisible = false;
                }else {
                    tc.IsVisible = true;
                }
            }
            if (currenttc == tc) {
                utab();
            }
        };

        tc.tabControl.GotFocus += (e,a) => {
            try {
                currenttc = tc;
                if (tc.tabControl.Items.Count > 0) {
                    selectedtab = (TabItem)tc.tabControl.SelectedItem;
                    tabchanged();
                    update();
                }else {
                    updatetabsgrid();
                }
            }catch{}
        };
        tc.tabControl.SelectionChanged += (e,a) => {
            try {
                currenttc = tc;
                if (tc.tabControl.Items.Count > 0) {
                    selectedtab = (TabItem)tc.tabControl.SelectedItem;
                    tabchanged();
                    update();
                }else {
                    updatetabsgrid();
                }
            }catch{}
        };
        
        

        tc.PointerMoved += (e,a) => {
            update();
        };

        currenttc = tc;

        alltc.Add(tc);
        updatetabsgrid();
        return tc;
    }
	

	
	void savedata()
	{
		settings["theme"] = cthmid;
		settings["isMaximized"] = this.WindowState == WindowState.Maximized;

		File.WriteAllText("./DATA.JSON", JsonConvert.SerializeObject(settings));
	}
	
	void inittitle() {
		try {
			tabcont tb = (tabcont)selectedtab.Content;
			Title = "OrangemiumIDE " + tb.filepath + (tb.issaved ? "": " *");
		}catch {
			Title = "OrangemiumIDE";
		}
	}
	
	async Task<bool> savefile(tabcont tb, bool isas = false) {
		if (tb.filepath.Trim() == "" || isas)
		{
			var topLevel = TopLevel.GetTopLevel(this);

			// Start async operation to open the dialog.
			var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
			{
				Title = "Save File"
			});
			if (file is not null)
			{
				// Open writing stream from the file.
				await using var stream = await file.OpenWriteAsync();
				((Label)((StackPanel)selectedtab.Header).Children[1]).Content = file.Name;
				using var streamWriter = new StreamWriter(stream);
				// Write some content to the file.
				string[] splits = tb.editor.Text.Split("\n");
				tb.filepath = file.Path.AbsolutePath;
				foreach (string split in splits)
				{
					streamWriter.WriteLine(split);
				}

				var lng = ro.GetLanguageByExtension(Path.GetExtension(file.Path.AbsolutePath));
				tb.lang = lng;
				if (lng != null)
				{
					tb.tmi.SetGrammar(ro.GetScopeByLanguageId(lng.Id));
					mv.codetype.Content = tb.lang.Aliases[0];
				}else {
					mv.codetype.Content = "Unknown";
				}

				streamWriter.Close();
				inittreeview();
				tb.issaved = true;
				inittitle();
				((Label)((StackPanel)selectedtab.Header).Children[0]).IsVisible = false;
                return true;
			}else {
                return false;
            }
		}
		else
		{
			await File.WriteAllTextAsync(tb.filepath, tb.editor.Text);
			tb.issaved = true;
			inittitle();
			((Label)((StackPanel)selectedtab.Header).Children[0]).IsVisible = false;
            return true;
		}
	}
	
	void openfolder(string dirpath) {
		alfolders.Add(dirpath);
		TreeViewItem mtvi = new() { Header = Path.GetFileName(dirpath) };
		ContextMenu cm = new();
		MenuItem mirem = new() { Header = "Remove Folder" };
		mirem.Click += (e, a) => {
			mv.fv.tex.Items.Remove(mtvi);
			alfolders.Remove(dirpath);
		};
		cm.Items.Add(mirem);
		mtvi.ContextMenu = cm;
		mtvi.IsExpanded = true;
		inittitem(mtvi, dirpath);
		mv.fv.tex.Items.Add(mtvi);
	}
	
	
	
	void refocuscurrentfile() {
        if (selectedtab.Content is tabcont) {
            tabcont tb = (tabcont)(selectedtab).Content;
            if (fa.ContainsKey(tb.filepath)) {
                mv.fv.tex.SelectedItem = fa[tb.filepath];
            }
        }
	}
	
    void inittreeview()
    {
        
        mv.fv.tex.Items.Clear();
        foreach (string dirpath in alfolders)
        {
            TreeViewItem mtvi = new() { Header = Path.GetFileName(dirpath) };
            ContextMenu cm = new();
            MenuItem mirem = new() { Header = "Remove Folder" };
            mirem.Click += (e, a) => {
                mv.fv.tex.Items.Remove(mtvi);
                alfolders.Remove(dirpath);
            };
            cm.Items.Add(mirem);
            mtvi.ContextMenu = cm;
            mtvi.IsExpanded = true;
            inittitem(mtvi, dirpath);
            mv.fv.tex.Items.Add(mtvi);
        }
    }
	
    void inittitem(TreeViewItem tvi, string path)
    {
        try
        {
            
            string[] folders = Directory.GetDirectories(path);
            foreach (string folder in folders)
            {
                StackPanel hdr = new() {Orientation = Orientation.Horizontal};
                Image icon = new() {Width = 12,Height = 12};
                Label nmlbl = new();
                hdr.Children.Add(icon);
                hdr.Children.Add(nmlbl);
                nmlbl.Content = Path.GetFileName(folder);
                icon.Source = geticon("folder");
                TreeViewItem titem = new() { Header = hdr };
                TreeViewItem ttem = new();
                titem.Items.Add(ttem);
                bool isfirst = true;
                titem.GotFocus += (e, a) =>
                {
                    if (isfirst)
                    {
                        titem.Items.Clear();
                        inittitem(titem, folder);
                        titem.IsExpanded = true;
                        isfirst = false;
                    }
					refocuscurrentfile();
                };
                tvi.Items.Add(titem);
            }
            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                StackPanel hdr = new() {Orientation = Orientation.Horizontal};
                Image icon = new() {Width = 12,Height = 12};
                Label nmlbl = new();
                hdr.Children.Add(icon);
                hdr.Children.Add(nmlbl);
                nmlbl.Content = Path.GetFileName(file);
                icon.Source = geticon("file");
                TreeViewItem titem = new() { Header = hdr };
                fa[file] = titem;
				titem.PointerReleased += (e, a) => { openfile(selectedtab, file); };
                ContextMenu fc = new();
                MenuItem oant = new() { Header = "Open At New Tab" };
                oant.Click += (e, a) => {
                    openfile(newtab(), file);
                };
                MenuItem cpp = new() {Header = "Copy Path"};
                
                cpp.Click += (e,a) => {
                    Clipboard.SetTextAsync(file);
                };
                MenuItem del = new() { Header = "Delete" };
                del.Click += async (e, a) => {
                    MessageBox.MessageBoxResult res = await MessageBox.Show(this, "Do you really want to delete this file?", "Delete File?", MessageBox.MessageBoxButtons.YesNo);
                    if (res == MessageBox.MessageBoxResult.Yes)
                    {
                        try
                        {
                            File.Delete(file);
                            tvi.Items.Remove(titem);
                        }
                        catch (Exception g) {
                            await MessageBox.Show(this, "Failled To Delete This File: \n" + g.Message, "Delete File", MessageBox.MessageBoxButtons.Ok);
                        }
                    };
                };
                fc.Items.Add(oant);
                fc.Items.Add(cpp);
                fc.Items.Add(del);
                titem.ContextMenu = fc;
                tvi.Items.Add(titem);
            }
        }catch (Exception e) { 
            
        }
    }

    public void openfile(TabItem tb, string path, bool force = false)
    {
        foreach (EditorTabControl tc in alltc)
		foreach (TabItem i in tc.tabControl.Items) {
            if (i.Content is tabcont) {
                tabcont abb = (tabcont)i.Content;
                if (abb.filepath.ToLower() == path.ToLower()) {
                    tc.tabControl.SelectedItem = abb;
                    return;
                }
            }
		}
		
		TabItem t = tb;
		if (t.Content is tabcont) {
            if (((tabcont)t.Content).issaved == false && force == false) {
                t = newtab();
            }
        }else {
            ctab(t);
            t = newtab();
        }
		tabcont tab = (tabcont)t.Content;
		tab.editor.TextArea.MinWidth = 0;
        string filecontent = File.ReadAllText(path);
        tab.filepath = path;
        tab.langext = Path.GetExtension(path);
        try
        {
            var lng = ro.GetLanguageByExtension(Path.GetExtension(path));
			tab.lang = lng;
            if (lng != null)
            {
                tab.tmi.SetGrammar(ro.GetScopeByLanguageId(lng.Id));
				mv.codetype.Content = lng.Aliases[0];
            }else {
				mv.codetype.Content = "Unknown";
			}
        }
        catch
        {

        }
        tab.editor.Text = filecontent;
        ((Label)((StackPanel)t.Header).Children[1]).Content = Path.GetFileName(path);
		((Label)((StackPanel)selectedtab.Header).Children[0]).IsVisible = false;
        inittitle();
    }


    //MainWindow newwindow(EditorTabControl etc) {
    //    MainWindow win = new();
    //    win.Show();
    //    ((Grid)etc.Parent).Children.Remove(etc);
    //    win.mv.tg.Children.Clear();
    //    win.mv.tg.Children.Add(etc);
    //    return win;
    //}

    void inittabitem(TabItem tab) {
        StackPanel hstack = (StackPanel)tab.Header;
        ContextMenu tabtb = new();
        hstack.ContextMenu = tabtb;
        if (tab.Content is tabcont) {
            tabcont tc = (tabcont)tab.Content;
            MenuItem cpp = new() {Header = "Copy Path"};
            tabtb.Items.Add(cpp);
            cpp.Click += (e,a) => {
                Clipboard.SetTextAsync(tc.filepath);
            };
            tabtb.Items.Add(new Separator());
        }
        MenuItem mvt = new() {Header = "Move to different split"};
        tabtb.Items.Add(mvt);
        MenuItem mtdw = new() {Header = "Move to different window"};
        tabtb.Items.Add(mtdw);
        tabtb.Items.Add(new Separator());
        MenuItem cl = new() {Header = "Close"};
        tabtb.Items.Add(cl);
        cl.Click += (e,a) => {
            ctab(tab);
        };
        tabtb.Opened += (e,a) => {
            mvt.Items.Clear();
            mtdw.Items.Clear();
            {
                int i = 0;
                foreach (EditorTabControl ct in mv.tg.Children) {
                    MenuItem cti = new() {Header = "Split - " + i};
                    mvt.Items.Add(cti);
                    cti.Click += (b,x) => {
                        ((TabControl)tab.Parent).Items.Remove(tab);
                        ct.tabControl.Items.Add(tab);
                    };
                    i++;
                }
            }
            {
                int i = 0;
                foreach (MainWindow win in wins) {
                    MenuItem cti = new() {Header = "Window " + i + ": " + win.Title};
                    mtdw.Items.Add(cti);
                    int s = 0;
                    foreach (EditorTabControl ct in win.mv.tg.Children) {
                        MenuItem sp = new() {Header = "Split - " + s};
                        cti.Items.Add(sp);
                        sp.Click += (b,x) => {
                            ((TabControl)tab.Parent).Items.Remove(tab);
                            ct.tabControl.Items.Add(tab);
                            win.UpdateLayout();
                            win.Show();
                            win.Activate();
                        };
                        s++;
                    }
                    i++;
                }
            }
        };
    }

    TabItem settingstab(EditorTabControl? tc = null)
    {
        if (tc == null) tc = currenttc;
        TabItem tab = new();
        SettingsTab tabcontent = new();
        tab.Content = tabcontent;
        
		
        StackPanel hstack = new() { Orientation = Avalonia.Layout.Orientation.Horizontal };
		
        Label tabtitle = new() { Content = "Settings", FontSize = 15 };
        hstack.Children.Add(tabtitle);
        Button closebtn = new() { Content = "x",Background = Brushes.Transparent , Height = 20, Width = 20, FontSize = 13, HorizontalContentAlignment = HorizontalAlignment.Center,VerticalContentAlignment = VerticalAlignment.Center, Padding = new Avalonia.Thickness(0), CornerRadius = new Avalonia.CornerRadius(10) };
        hstack.Children.Add(closebtn);
        closebtn.Click += (e, a) =>
        {
			ctab(tab);
        };

        tabcontent.fac.IsChecked = (bool)settings["enableFallbackAutocomplete"];
        tabcontent.sln.IsChecked = (bool)settings["ShowLineNumbers"];
        tabcontent.thmtb.Text = settings["IconPack"].ToString();
        
        tabcontent.fac.IsCheckedChanged += (e,a) => {
            settings["enableFallbackAutocomplete"] = tabcontent.fac.IsChecked;
        };
        tabcontent.sln.IsCheckedChanged += (e,a) => {
            settings["ShowLineNumbers"] = tabcontent.sln.IsChecked;
            foreach (EditorTabControl ct in alltc)
            foreach (TabItem tb in ct.tabControl.Items)
            {
                if (tb.Content is tabcont) {
                    tabcont tab = (tabcont)tb.Content;
                    tab.editor.ShowLineNumbers = (bool)settings["ShowLineNumbers"];
                }
            }
        };
        tabcontent.thmtb.TextChanged += (e,a) => {
            settings["IconPack"] = tabcontent.thmtb.Text;
        };

        foreach (KeyValuePair<string,Dictionary<string,object>> ext in extensions) {
            extensionli li = new();
            li.namelbl.Content = ext.Value["Name"].ToString();
            li.verlbl.Content = ext.Value["Version"].ToString();
            li.shortdesc.Text = ext.Value["ShortDescription"].ToString();
            li.ico.Source = new Bitmap(Path.Join(ext.Key,ext.Value["Icon"].ToString()));
            li.chenable.IsChecked = ((JArray)settings["extensions"]).Any(t => t.Value<string>() == ext.Key);
            tabcontent.exc.Children.Add(li);
            li.chenable.IsCheckedChanged += (e,a) => {
                if (li.chenable.IsChecked == true) {
                    ((JArray)settings["extensions"]).Add(ext.Key);
                }else {
                    ((JArray)settings["extensions"]).Remove(ext.Key);
                }
            };
        }

        tab.Header = hstack;
        tc.tabControl.Items.Add(tab);
        tc.tabControl.SelectedItem = tab;
        inittabitem(tab);
        return tab;
    }
    
    TabItem hometab(EditorTabControl? tc = null)
    {
        if (tc == null) tc = currenttc;
        TabItem tab = new();
        WelcomeTab tabcontent = new();
        tab.Content = tabcontent;
        
		
        StackPanel hstack = new() { Orientation = Avalonia.Layout.Orientation.Horizontal };
		
        Label tabtitle = new() { Content = "Welcome!", FontSize = 15 };
        hstack.Children.Add(tabtitle);
        Button closebtn = new() { Content = "x",Background = Brushes.Transparent , Height = 20, Width = 20, FontSize = 13, HorizontalContentAlignment = HorizontalAlignment.Center,VerticalContentAlignment = VerticalAlignment.Center, Padding = new Avalonia.Thickness(0), CornerRadius = new Avalonia.CornerRadius(10) };
        hstack.Children.Add(closebtn);
        closebtn.Click += (e, a) =>
        {
			ctab(tab);
        };
        

        tab.Header = hstack;
        tc.tabControl.Items.Add(tab);
        tc.tabControl.SelectedItem = tab;
        inittabitem(tab);
        return tab;
    }
    Dictionary<int,Control> sidebartabs = new();
    int sbid = 0;
	List<ToggleButton> sdbi = new();
    void selectsidebaritem(int id,ToggleButton btn) {
		foreach (ToggleButton t in sdbi) {
			if (t != btn) {
				t.IsChecked = false;
			}else {
				t.IsChecked = true;
			}
		}
        foreach (KeyValuePair<int,Control> c in sidebartabs) {
            if (c.Key == id) {
                c.Value.IsVisible = true;
            }else {
                c.Value.IsVisible = false; 
            }
        }
    }
	Bitmap errbitmap = new Bitmap(AssetLoader.Open(new Uri("avares://OrangemiumIDE/Assets/icoerr.png")));

    Bitmap geticon(string iconname, bool autovariant = true) {
        try {
            if (this.PlatformSettings.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark && autovariant) {
                iconname += "_DARK";
            }
			if (cachedicons.ContainsKey(iconname)) {
				return cachedicons[iconname];
			}else {
				string slchar = "/";
				if (((string)settings["IconPack"]).Contains("\\")) {
					slchar = "\\";
				}
				
				//mv.statuslbl.Content = Path.Join((string)settings["IconPack"], slchar + iconname + ".png");
				cachedicons[iconname] = new Bitmap(Path.Join((string)settings["IconPack"], slchar + iconname + ".png"));
				return cachedicons[iconname];
			}
        }catch (Exception e) {
            //mv.statuslbl.Content = Path.Join((string)settings["IconPack"], "/" + iconname + ".png") + e.ToString();
            return errbitmap;
        }
    }
	
    ToggleButton addsidebaritem(string icon, string header,Control tab) {
        ToggleButton btn = new();
        btn.Width = 50;
        btn.Height = 50;
        Image ico = new() {Source = geticon(icon)};
		btn.Content = ico;
        ToolTip tp = new();
        tp.Content = header;
        btn.SetValue(ToolTip.TipProperty,tp);
        int id = sbid;
        sidebartabs[id] = tab;
		sdbi.Add(btn);
        btn.Click += (e,a) => {
           selectsidebaritem(id,btn);
           if (mv.sidearea.Width == 50) {
            mv.sidearea.Width = 330;
            settings["sidebarSize"] = mv.sidearea.Width;
           }
           
        };
        sbid++;
        return btn;
    }

    async void ctab(TabItem tab) {
        TabControl tc = (TabControl)tab.Parent;
        if (tab.Content is tabcont) {
            tabcont tabcontent = (tabcont)tab.Content;
            if (tabcontent.issaved == false) {
                var r = await MessageBox.Show(this, "Do you want to save this file?", "Close File", MessageBox.MessageBoxButtons.YesNoCancel);
                if (r == MessageBox.MessageBoxResult.Yes) {
                    if (await savefile(tabcontent) == true) {tc.Items.Remove(tab);tabcontent.editor.Clear();tabcontent = null;}

                    
                }
                if (r == MessageBox.MessageBoxResult.No) {
                    tc.Items.Remove(tab);
                    tabcontent.editor.Clear();
                    tabcontent = null;
                }
            }else {
                tc.Items.Remove(tab);
                tabcontent.editor.Clear();
                tabcontent = null;
            }
        }else {
            tc.Items.Remove(tab);
        }
        if (tc.Items.Count == 0 && alltc[0].tabControl != tc) {
            mv.tg.Children.Remove(tc);
        }

        tab = null;
    }

    public TabItem newtab(EditorTabControl? tc = null)
    {
        if (tc == null) tc = currenttc;
        TabItem tab = new();
        tabcont tabcontent = new();
        tabcontent.editor.WordWrap = (bool)settings["WordWrap"];
        tabcontent.editor.TextArea.TextView.LineTransformers.Add(new SelectionLineTransformer(tabcontent.editor.TextArea,tabcontent)); 
        //tabcontent.editor.TextArea.TextWrapping = TextWrapping.WrapWithOverflow;
        tab.Content = tabcontent;
        tabcontent.cop.Click += (e, a) => {
            Clipboard.SetTextAsync(tabcontent.editor.TextArea.Selection.GetText());
        };
        tabcontent.cut.Click += (e, a) => {
            Clipboard.SetTextAsync(tabcontent.editor.TextArea.Selection.GetText());
            tabcontent.editor.TextArea.Selection.ReplaceSelectionWithText("");
        };
        tabcontent.pas.Click += async void (e, a) => {
            tabcontent.editor.TextArea.Selection.ReplaceSelectionWithText(await Clipboard.GetTextAsync());
        };
        tabcontent.sall.Click += async void (e, a) => {
            tabcontent.editor.TextArea.Selection = Selection.Create(tabcontent.editor.TextArea, 0, tabcontent.editor.Text.Length);
        };
		void u() {
			//if (tabcontent.editor.TextArea.MinWidth < tabcontent.editor.TextArea.DesiredSize.Width) {
			//	tabcontent.editor.MinWidth = tabcontent.editor.TextArea.DesiredSize.Width;
			//}
		}

        tabcontent.editor.TextArea.GotFocus += (e,a) => {
            currenttc = tabcps[(TabControl)tab.Parent];
            selectedtab = tab;
            tabchanged();
            utab();
        };
        
		Label usi = new() { Content = "*", FontSize = 15, IsVisible = false };
		tabcontent.PointerMoved += (e,a) => {
			u();
		};
        tabcontent.editor.ShowLineNumbers = (bool)settings["ShowLineNumbers"];
        tabcontent.editor.TextArea.KeyDown += (e,a) => {
            tabcontent.issaved = false;
			usi.IsVisible = true;
			inittitle();
        };
        tabcontent.editor.TextArea.TextEntered += async void (sender, e) => {
			
			try {
				if (e.Text != "\n") {
                    tabcontent.editor.LineDown();
                    cv = new CompletionWindow(tabcontent.editor.TextArea);
                    cv.Closed += (o, args) => cv = null;
                    {
                        Task<List<MyCompletionData>> ctx = getSuggestions(tabcontent);
                        ctx.ContinueWith((Task<List<MyCompletionData>> c) =>
                        {
                            if (c.IsCompletedSuccessfully) {
                                Dispatcher.UIThread.Post(() => {
                                    try {
                                        foreach (var x in c.Result) {
                                            cv.CompletionList.CompletionData.Add(x);
                                        }
                                        
                                        cv.Show();
                                    }catch {}
                                });
                            }
                        });
                    }
					{
                        Task<Dictionary<string,object>> ctx = checkDoc(tabcontent);
                        ctx.ContinueWith((Task<Dictionary<string,object>> c) =>
                        {
                            if (c.IsCompletedSuccessfully) {
                                Dispatcher.UIThread.Post(() => {
                                    tabcontent.problems = (List<docProb>)c.Result["Problems"];

                                    tabchanged(false);
                                });
                            }
                        });
                    }
					
				}
				
			}catch {
				mv.statuslbl.Content = "Failled Giving Suggestions";
			}
			u();
        };
        StackPanel hstack = new() { Orientation = Avalonia.Layout.Orientation.Horizontal };
		
        hstack.Children.Add(usi);
        Label tabtitle = new() { Content = "New Tab", FontSize = 15 };
        hstack.Children.Add(tabtitle);
        Button closebtn = new() { Content = "x",Background = Brushes.Transparent , Height = 20, Width = 20, FontSize = 13, HorizontalContentAlignment = HorizontalAlignment.Center,VerticalContentAlignment = VerticalAlignment.Center, Padding = new Avalonia.Thickness(0), CornerRadius = new Avalonia.CornerRadius(10) };
        hstack.Children.Add(closebtn);
        closebtn.Click += (e, a) =>
        {
			ctab(tab);
        };

        

        var _textMateInstallation = tabcontent.editor.InstallTextMate(ro);
        tabcontent.tmi = _textMateInstallation;

        tab.Header = hstack;
        tc.tabControl.Items.Add(tab);
        tc.tabControl.SelectedItem = tab;
        tabcontent.tmi.SetTheme((IRawTheme)cthm["theme"]);
        if (cthm.ContainsKey("bgcolor"))
        {
            tabcontent.editor.Background = (Avalonia.Media.Immutable.ImmutableSolidColorBrush)cthm["bgcolor"];
        }
        else
        {
            tabcontent.editor.Background = Brushes.Transparent;
        }
        if (cthm.ContainsKey("txcolor"))
        {
            tabcontent.editor.Foreground = (Avalonia.Media.Immutable.ImmutableSolidColorBrush)cthm["txcolor"];
        }
        else
        {
            tabcontent.editor.Foreground = Foreground;
        }
        inittabitem(tab);
        return tab;
    }

    
}

public class MyCompletionData : ICompletionData
{
    public MyCompletionData(string text, string desc, int splitindex)
    {
        Text = text;
        Desc = desc;
        SplitIndex = splitindex;
    }
    
    public int SplitIndex = 0;

    public IImage Image => null;

    public string Text { get; }
    public string Desc { get; }

    // Use this property if you want to show a fancy UIElement in the list.
    public object Content => Text;

    public object Description => Desc;

    public double Priority { get; } = 0;

    public void Complete(TextArea textArea, ISegment completionSegment,
        EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text.Substring(SplitIndex));
    }
}

internal class SelectionLineTransformer : GenericLineTransformer
{
    internal SelectionLineTransformer(TextArea textArea, tabcont tabcontent) : base(null)
    {
        _textArea = textArea;
        tbc = tabcontent;
    }

    protected override void TransformLine(DocumentLine line, ITextRunConstructionContext context)
    {
        if (_textArea.Selection.StartPosition.Line == _textArea.Selection.EndPosition.Line &&
            line.LineNumber == _textArea.Selection.StartPosition.Line)
            SetTextStyle(
                line,
                0,
                line.Length,
                null,
                new SolidColorBrush(Color.FromArgb(150,170,170,170)).ToImmutable(),
                Avalonia.Media.FontStyle.Normal,
                FontWeight.Normal,
                false);
        //var linei = tbc.editor.Text.Take(tbc.editor.SelectionStart).Count(c => c == '\n') + 1;
        //if (linei == line.LineNumber)
            
        foreach (var problem in tbc.problems)
        if (problem.line == line.LineNumber)
        {
            try {
                var startColumn = problem.column;//_textArea.Selection.StartPosition.Column - 1;
                var endColumn = problem.column + problem.length + 1;//_textArea.Selection.EndPosition.Column - 1;
                IImmutableBrush color = Brushes.Transparent;
                switch (problem.level)
                {
                    case "E":
                        color = new SolidColorBrush(Color.FromArgb(200,255,0,0)).ToImmutable();
                        break;
                    case "W":
                        color = new SolidColorBrush(Color.FromArgb(200,255,250,0)).ToImmutable();
                        break;
                }
                SetTextStyle(
                    line,
                    startColumn,
                    endColumn - startColumn,
                    null,
                    color,
                    Avalonia.Media.FontStyle.Normal,
                    FontWeight.Normal,
                    false);
            }catch {}
        }
    }
    tabcont tbc;
    TextArea _textArea;
}