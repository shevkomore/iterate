using ElectronNET.API;
using ElectronNET.API.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iterate.file
{
    public class FileObserver
    {
        public string path;
        public bool active = true;
        public FileSystemWatcher watcher;
        private IterateApplicationContext context;
        public FileObserver(IterateApplicationContext context, string path) {
            this.path = path;
            this.context = context;
            if (!File.Exists(path))
            {
                active = false;
            }
            watcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
            watcher.NotifyFilter =  NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastWrite;
            watcher.Renamed += onRenamed;
            watcher.Deleted += onLost;
            watcher.EnableRaisingEvents = true;
        }

        public async void onLost(object sender, FileSystemEventArgs e)
        {
            context.DelaySaveChanges();
            MessageBoxResult decision = await Electron.Dialog.ShowMessageBoxAsync(new MessageBoxOptions(
                "iterate не бачить файл: "
                + Path.GetFileName(e.FullPath)
                + ". Оберіть спосіб вирішення.")
            { Buttons = new string[] { "Знайти файл", "Відновити зі сховища", "Закрити програму" } });
            switch (decision.Response)
            {
                case 0:
                    var path = await Electron.Dialog.ShowOpenDialogAsync(context.UIEventBus?.sidePanel, new OpenDialogOptions
                    {
                        Title = "Оберіть файл для відслідковування"
                    });
                    //remake the watcher to use new path
                    watcher.Dispose();
                    this.path = path[0];
                    watcher = new FileSystemWatcher(Path.GetDirectoryName(path[0]), Path.GetFileName(path[0]));
                    watcher.NotifyFilter = NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastWrite;
                    watcher.Renamed += onRenamed;
                    watcher.Deleted += onLost;
                    watcher.EnableRaisingEvents = true;
                    break;
                case 1:
                    context.versionStorage.Retrieve(this.path, context.versionManager.GetCurrentVersion().Id);
                    break;
                case 2:
                    Electron.App.Exit();
                    break;
                default:
                    throw new Exception("Unexpected responce");
            }

        }

        private void onRenamed(object sender, RenamedEventArgs e)
        {
            path = e.FullPath;
            watcher.Filter = e.Name;
            context.projectManager.UpdateFilePath(context.projectManager.CurrentProject.Id, path);
        }
    }
}
