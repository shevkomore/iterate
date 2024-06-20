using ElectronNET.API;
using ElectronNET.API.Entities;
using iterate.ui.data;
using iterate.versions;
using Microsoft.AspNetCore.Hosting.Server;
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

        public System.Timers.Timer LostFilePanicDelay = new System.Timers.Timer(TimeSpan.FromSeconds(5).TotalMilliseconds);

        public FileObserver(IterateApplicationContext context, string path) {
            this.path = path;
            this.context = context;

            LostFilePanicDelay.AutoReset = false;
            LostFilePanicDelay.Elapsed += (s, e) => context.onFileMissing();
            if (!File.Exists(path))
            {
                LostFilePanicDelay.Start();
            }

            watcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
            watcher.NotifyFilter =  NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastWrite;

            watcher.Changed += onFileChanged;
            watcher.Created += onCreated;
            watcher.Renamed += onRenamed;  //which is used by some programs to save data
            watcher.Deleted += onDeleted;
            watcher.EnableRaisingEvents = true;
        }

        private void onCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("File creation detected: " + e.FullPath);
            if (LostFilePanicDelay.Enabled)
            {
                LostFilePanicDelay.Stop();
            }
            context.CreateVersion(e.FullPath);
                
        }

        private void onFileChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("File change detected: " + e.FullPath);
            context.CreateVersion(e.FullPath);
        }
        
        public async void onDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("File deletion detected: "+e.Name);
            LostFilePanicDelay.Start();
        }

        private void onRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine("Detected rename: "+e.OldName+"->"+e.Name);
            if(e.OldName == watcher.Filter) //probably our file is ready for removal
            {
                LostFilePanicDelay.Start();
                return;
            }
            if (e.Name == watcher.Filter)   //probably our file got restored
            {
                LostFilePanicDelay.Stop();
                context.CreateVersion(e.FullPath);
                return;
            }
            Console.WriteLine("Uh oh! This should not be reachable! Filter seems to be not matching any of checked filenames.");
            Console.WriteLine("The filter that failed is " + watcher.Filter);
        }
    }
}
