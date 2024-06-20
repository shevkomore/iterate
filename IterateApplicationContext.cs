using ElectronNET.API;
using ElectronNET.API.Entities;
using iterate.file;
using iterate.ui;
using iterate.ui.data;
using iterate.versions;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Drawing;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace iterate
{
    public class IterateApplicationContext
    {
        public readonly string HOME_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iterate", "Repositories");

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

        //Variables for version storing procedure
        private static SemaphoreSlim FileChangeSemaphore = new SemaphoreSlim(1, 1);
        private System.Timers.Timer SaveDelay = new System.Timers.Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
        private bool SaveBlocked = false;

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
            string[] path = (await Electron.Dialog.ShowOpenDialogAsync(projectSelectForm, new OpenDialogOptions
            {
                Title = "Select a file to create a version tree:",
            }));
            if (path.Length < 1)
            {
                Electron.App.Relaunch();
                projectSelectForm.Close();
                Electron.App.Exit();
                return;
            }
            OnOpenRepo(projectManager.AddFilePath(path[0]));
        }
        public async void OnOpenRepo(object data)
        {
            string id = (string)data;
            Console.WriteLine("Received id: "+id);
            projectManager.SelectProject(id);
            CurrentProject = id;
            Console.WriteLine("Project set");
            versionStorage = new VersionStorage(this, id);
            Console.WriteLine("VersionStorage initialized");
            versionManager = new VersionManager(this, id);
            Console.WriteLine("VersionManager initialized");

            observer = new FileObserver(this, projectManager.CurrentProject.Path);
            Console.WriteLine("FileObserver initialized");
            UIEventBus = new UIEventBus(this);
            Console.WriteLine("Event bus initialized");

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
        public async void CreateVersion(string path)
        {
            if (SaveBlocked) return;
            await FileChangeSemaphore.WaitAsync();
            observer!.watcher.EnableRaisingEvents = false;
            System.Drawing.Image screenshot = imageFormatter.Resize(imageFormatter.CaptureWorkingWindow());
            UIEventBus!.Notify(new ui.Notification("Збереження версії...", true));
            string id = versionStorage!.Store(path);
            versionManager!.AddVersion(id, new VersionNode() { TimeCreated = DateTime.Now.ToString("g"), Image = imageFormatter.ToBase64(screenshot) });

            UIEventBus.Notify(new ui.Notification("Версію збережено!"));
            UIEventBus.Versions.Update(versionManager.tree);

            observer!.watcher.EnableRaisingEvents = true;
            DelaySaveChanges();
            FileChangeSemaphore.Release();
        }
        public void DelaySaveChanges()
        {
            SaveBlocked = true;
            SaveDelay.Start();
        }
        #endregion

        public async void onFileMissing()
        {
            DelaySaveChanges();
            MessageBoxResult decision = await Electron.Dialog.ShowMessageBoxAsync(new MessageBoxOptions(
                "iterate не бачить файл: "
                + Path.GetFileName(projectManager.CurrentProject.Path)
                + ". Оберіть спосіб вирішення.")
            { Buttons = new string[] { "Чекати ще", "Знайти файл", "Відновити зі сховища", "Закрити програму" } });
            switch (decision.Response)
            {
                case 0:
                    observer!.LostFilePanicDelay.Start();
                    break;
                case 1:
                    var paths = await Electron.Dialog.ShowOpenDialogAsync(UIEventBus?.sidePanel, new OpenDialogOptions
                    {
                        Title = "Оберіть файл для відслідковування"
                    });
                    //remake the watcher to use new path
                    observer = new FileObserver(this, paths[0]);
                    break;
                case 2:
                    versionStorage!.Retrieve(projectManager.CurrentProject.Path, versionManager!.GetCurrentVersion().Id);
                    break;
                case 3:
                    Electron.App.Exit();
                    break;
                default:
                    throw new Exception("Unexpected responce");
            }
        }

        private void TryExit()
        {
            if (Electron.WindowManager.BrowserWindows.Count == 0)
            {
                Electron.App.Quit();
            }
        }
    }
}
