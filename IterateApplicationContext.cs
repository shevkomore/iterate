using ElectronNET.API;
using ElectronNET.API.Entities;
using iterate.file;
using iterate.ui;
using iterate.ui.data;
using iterate.versions;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;

namespace iterate
{
    public class IterateApplicationContext
    {
        public readonly string HOME_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iterate");

        public IIDGenerator IDGenerator = new BasicIdGenerator();
        public UIEventBus? UIEventBus;
        public ProjectManager projectManager;
        public VersionStorage? versionStorage;
        public FileObserver? observer;
        public ImageFormatter imageFormatter;
        public string? CurrentProject;

        //Repo selection is set up before application finishes initializing;
        //therefore the logic needed to set it up is defined here
        public BrowserWindow? projectSelectForm;
        public VersionManager? versionManager;
        UIDataHandler<List<ProjectManager.ProjectData>> Projects;

        public IterateApplicationContext()
        {
            SaveDelay.AutoReset = false;
            SaveDelay.Elapsed += (s, e) =>
            {
                SaveBlocked = false;
                SaveDelay.Stop();
            };
            imageFormatter = new ImageFormatter(150, 100);
            Directory.CreateDirectory(HOME_PATH);
            projectManager = new ProjectManager(this);
            Projects = new UIDataHandler<List<ProjectManager.ProjectData>>("repos");
        }

        public async Task Start()
        {
            _ = Electron.IpcMain.On("init-start", _ => Projects.Update(projectManager.GetAllProjects()));
            _ = Electron.IpcMain.On("open-repo", OnOpenRepo);
            _ = Electron.IpcMain.On("create-repo", OnCreateRepo);
            projectSelectForm = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
            {
                Width = 800,
                Height = 600,
                Icon = Path.Combine(Directory.GetCurrentDirectory(), "ui/assets/iterate_ico1.png"),
                Title = "iterate: Оберіть проект",
                WebPreferences = {

                }

            });
            Projects.Listeners.Add(projectSelectForm);
            projectSelectForm.LoadURL($"{Environment.CurrentDirectory}/wwwroot/index.html#start");
            //TODO connect start to receive repo list
            projectSelectForm.OnReadyToShow += () => projectSelectForm.Show();
            projectSelectForm.OnClose += ProjectSelect_OnClose;
        }

        private void ProjectSelect_OnClose()
        {
            if (CurrentProject != null) return;
            projectSelectForm?.Close();
            Electron.App.Quit();
        }

        public async void OnCreateRepo(object data)
        {
            string path = (await Electron.Dialog.ShowOpenDialogAsync(projectSelectForm, new OpenDialogOptions
            {
                Title = "Select a file to create a version tree:",
            }))[0];
            OnOpenRepo(projectManager.AddFilePath(path));
        }
        public async void OnOpenRepo(object data)
        {
            string id = (string)data;
            projectManager.SelectProject(id);
            CurrentProject = id;
            versionStorage = new VersionStorage(this, id);
            versionManager = new VersionManager(this, id);

            observer = new FileObserver(this, projectManager.CurrentProject.Path);
            UIEventBus = new UIEventBus(this);

            observer.watcher.Changed += onFileChanged;

            Electron.IpcMain.Send(projectSelectForm, "loaded", CurrentProject);

            projectSelectForm?.Hide();

            UIEventBus.CreateWindows();
        }

        private void VersionManager_CurrentVersionChanged(object sender, VersionTreeData e)
        {
            UIEventBus!.Notify(new ui.Notification("Завантажено версію!"));
            UIEventBus.Versions.Update(e);
        }
        #region Version saving logic
        private static SemaphoreSlim FileChangeSemaphore = new SemaphoreSlim(1, 1);
        private System.Timers.Timer SaveDelay = new System.Timers.Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
        private bool SaveBlocked = false;
        private async void onFileChanged(object sender, FileSystemEventArgs e)
        {
            if (SaveBlocked) return;
            await FileChangeSemaphore.WaitAsync();
            observer!.watcher.EnableRaisingEvents = false;
            System.Drawing.Image screenshot = imageFormatter.Resize(imageFormatter.CaptureWorkingWindow());

            UIEventBus!.Notify(new ui.Notification("Збереження версії...", true));

            string id = versionStorage!.Store(observer.path);
            versionManager!.AddVersion(id, new VersionNode() { TimeCreated = DateTime.Now.ToString("g"), Image = imageFormatter.ToBase64(screenshot) });

            UIEventBus.Notify(new ui.Notification("Версію збережено!"));
            UIEventBus.Versions.Update(versionManager.tree);

            observer.watcher.EnableRaisingEvents = true;
            DelaySaveChanges();
            FileChangeSemaphore.Release();
        }
        public void DelaySaveChanges()
        {
            SaveBlocked = true;
            SaveDelay.Start();
        }
        #endregion

        private void TryExit()
        {
            if (Electron.WindowManager.BrowserWindows.Count == 0)
            {
                Electron.App.Quit();
            }
        }
    }
}
