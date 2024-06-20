using ElectronNET.API;
using ElectronNET.API.Entities;
using iterate.file;
using iterate.versions;

namespace iterate.ui.data
{
    public class UIEventBus : INotifier
    {
        private IterateApplicationContext _context;

        public UIDataHandler<Notification> Notification = new UIDataHandler<Notification>("notification");
        public UIDataHandler<VersionTreeData> Versions;
        public UIDataHandler<VersionNode> LatestVersion;
        public UIDataHandler<VersionNode> CurrentVersion;
        public UIDataHandler<string> RemovedVersion;

        public BrowserWindow? sidePanelOpenButton;
        public BrowserWindow? sidePanel;
        public BrowserWindow? treeView;

        public UIEventBus(IterateApplicationContext context)
        {
            _context = context;
            //initializing data handlers
            Versions = new UIDataHandler<VersionTreeData>("versions");
            _context.versionManager!.DataChanged += (s, e) => Versions.Update(e);
            LatestVersion = new UIDataHandler<VersionNode>("version-latest");
            _context.versionManager.VersionAdded += (s, e) => LatestVersion.Update(e);
            CurrentVersion = new UIDataHandler<VersionNode>("version-current");
            _context.versionManager.CurrentVersionChanged += (s, e) => CurrentVersion.Update(e);
            RemovedVersion = new UIDataHandler<string>("version-removed");
            _context.versionManager.VersionRemoved += (s, e) => RemovedVersion.Update(e);
            //initializing all IPC channels
            Electron.IpcMain.On("init-all", InitializeViewData);
            Electron.IpcMain.On("open-side-panel", OpenSidePanel);
            Electron.IpcMain.On("hide-side-panel", HideSidePanel);
            Electron.IpcMain.On("load-version", LoadVersion);
            Electron.IpcMain.On("edit-version", EditVersion);
            Electron.IpcMain.On("delete-version", DeleteVersion);
            Electron.IpcMain.On("open-tree", OpenTree);
            Electron.IpcMain.On("close", CloseApp);
            //Electron.IpcMain.On("edit-version-image", EditVersionImage);
        }

        public async void CreateWindows()
        {
            ElectronNET.API.Entities.Size screenSize = (await Electron.Screen.GetPrimaryDisplayAsync()).Size;
            //sidePanelOpenButton
            sidePanelOpenButton = await Electron.WindowManager.CreateWindowAsync(
                new BrowserWindowOptions
                {
                    Width = 50,
                    Height = 50,
                    X = screenSize.Width - 50,
                    Y = screenSize.Height - 125,
                    AlwaysOnTop = true,
                    Closable = false,
                    Maximizable = false,
                    Minimizable = false,
                    Icon = Path.Combine(Directory.GetCurrentDirectory(), "ui/assets/iterate_ico1.png"),
                    TitleBarStyle = TitleBarStyle.hidden,
                    WebPreferences = {

                }
                });
            sidePanelOpenButton.LoadURL($"{Environment.CurrentDirectory}/wwwroot/index.html#fab");
            sidePanelOpenButton.OnReadyToShow += sidePanelOpenButton.Show;

            //sidePanel
            sidePanel = await Electron.WindowManager.CreateWindowAsync(
                new BrowserWindowOptions
                {
                    Width = 250,
                    Height = screenSize.Height,
                    //MaxWidth = 250,
                    X = screenSize.Width - 250,
                    AlwaysOnTop = true,
                    Closable = false,
                    Maximizable = false,
                    Minimizable = false,
                    Icon = Path.Combine(Directory.GetCurrentDirectory(), "ui/assets/iterate_ico1.png"),
                    TitleBarStyle = TitleBarStyle.hidden,
                });
            sidePanel.LoadURL($"{Environment.CurrentDirectory}/wwwroot/index.html#drawer");
            sidePanel.Hide();

        }
        public async void InitializeViewData(object data)
        {
            string page = (string)data;
            if(page == "fab")
            {
                Notification.Listeners.Add(sidePanelOpenButton!);
                return;
            }
            if(page == "drawer")
            {
                Console.WriteLine("Adding listeners to drawer...");
                Notification.Listeners.Add(sidePanel!);
                LatestVersion.Listeners.Add(sidePanel!);
                CurrentVersion.Listeners.Add(sidePanel!);
                Console.WriteLine("Listeners added. Sending initial data...");
                Console.WriteLine(_context.versionManager!.GetCurrentVersion().Id);
                CurrentVersion.Update(_context.versionManager!.GetCurrentVersion());
                Console.WriteLine("Data sent!");
                Notify(new ui.Notification("Ініціалізацію завершено"));
                return;
            }
            if(page == "tree")
            {
                Versions.Listeners.Add(treeView!);

                Versions.Update(_context.versionManager!.tree);
                return;
            }
        }
        public void Notify(Notification notification)
        {
            Notification.Update(notification);
        }

        private void OpenSidePanel(object data)
        {
           sidePanelOpenButton!.Hide();
           sidePanel!.Show();
        }
        private void HideSidePanel(object obj)
        {
            sidePanel!.Hide();
            sidePanelOpenButton!.Show();
        }
        private async void OpenTree(object obj)
        {
            if(treeView != null)
            {
                treeView.Focus();
                return;
            }
            //treeView
            treeView = await Electron.WindowManager.CreateWindowAsync(
                new BrowserWindowOptions
                {
                    Width = 800,
                    Height = 600,
                    Icon = Path.Combine(Directory.GetCurrentDirectory(), "ui/assets/iterate_ico1.png"),
                    Title = "iterate: Tree View"
                });
            treeView.LoadURL($"{Environment.CurrentDirectory}/wwwroot/index.html#tree");
            treeView.OnReadyToShow += treeView.Show;
            treeView.OnClosed += () => treeView = null;
        }

        private void LoadVersion(object obj)
        {
            string old = _context.versionManager.GetCurrentVersion().Id;

            string id = (string)obj;
            _context.DelaySaveChanges();
            Notify(new Notification("Loading version...", true));
            _context.versionStorage!.Retrieve(_context.observer!.path, id);
            _context.versionManager!.SetCurrentVersion(id);
            Notify(new Notification ("Version loaded successfully"));

            CurrentVersion.Update(_context.versionManager.GetCurrentVersion());
            LatestVersion.Update(_context.versionManager.GetVersion(old));
        }

        private void EditVersion(object obj)    //only descriptions for now
        {
            VersionNode version_update = (VersionNode)obj;
            VersionNode? target_version = _context.versionManager!.GetVersion(version_update.Id);
            if (target_version == null) return;
            if (version_update.Description != null && version_update.Description != target_version.Description)
                target_version.Description = version_update.Description;
            _context.versionManager.SetVersion(target_version);
        }

        private async void DeleteVersion(object obj)
        {
            string id = (string)obj;
            MessageBoxResult confirm = await Electron.Dialog.ShowMessageBoxAsync(new MessageBoxOptions
                ("Ви певні що хочете видалити версію? Ця операція безповоротна.")
            {
                Title = "Delete Version",
                Buttons = new string[] { "Так", "Ні" }
            });
            if (confirm.Response == 0)
            {
                _context.versionManager!.DeleteVersion(id);
            }
        }

        private void CloseApp(object obj)
        {
            Electron.App.Relaunch();
            foreach (BrowserWindow w in Electron.WindowManager.BrowserWindows)
            {
                w.Close();
            }
            Electron.App.Exit();
        }


        /*  Code bits for future edit-version-image (note: receives Id (string))
         if(command == "edit-version-image")
                {
                    if (obj["data"]["Id"] == null) return;
                    VersionNode version = _context.versionManager.GetVersion(obj["data"]["Id"].Value<string>());
                    ImageDialogResult res = _context.OpenImageDialog();
                    if(res.Result == System.Windows.Forms.DialogResult.OK)
                    {
                        version.Image = _context.imageFormatter.ToBase64(res.Image);
                        _context.versionManager.SetVersion(version);
                    }
                    return;
                }*/
    }
}
