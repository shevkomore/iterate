using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace iterate.versions
{
    public class VersionStorage
    {
        private static readonly string VERSIONS_FOLDER = "versions";
        private static readonly string STORAGE_FOLDER = "data";
        private static readonly string TEMP_FOLDER = "temp";
        private static readonly string VERSION_FILE_SEPARATOR = "\n";

        string folder;
        IterateApplicationContext context;
        public VersionStorage(IterateApplicationContext context, string id)
        {
            this.context = context;
            this.folder = Path.Combine(context.HOME_PATH, id);
            Directory.CreateDirectory(Path.Combine(folder, VERSIONS_FOLDER));
            Directory.CreateDirectory(Path.Combine(folder, STORAGE_FOLDER));
            Directory.CreateDirectory(Path.Combine(folder, TEMP_FOLDER));
        }
        //REMEMBER!
        //this class is yet to be connected to anything apart from being initialized in context
        //Run it this way first, get it to autosave a few versions (without storage)
        //Then make version tree from start to finish
        //and make storage work. Presto?
        public string Store(string path)
        {
            string working_copy_path = Path.Combine(folder, TEMP_FOLDER, Path.GetFileName(path));
            if(File.Exists(working_copy_path)) File.Delete(working_copy_path);
            File.Copy(path, working_copy_path);
            string version_id = context.IDGenerator.Generate();

            List<string> storage_locations;
            using(Stream file = File.OpenRead(working_copy_path))
            {
                storage_locations = PutDataInStorage(file);
            }
            string version_path = Path.Combine(folder, VERSIONS_FOLDER, version_id);
            using (StreamWriter version = File.CreateText(version_path))
            {
                foreach (string storage_location in storage_locations) 
                { 
                    version.Write(storage_location); 
                    version.Write(VERSION_FILE_SEPARATOR);
                }
            }
            return version_id;
        }
        public bool Retrieve(string path, string id)
        {
            string id_path = Path.Combine(folder, VERSIONS_FOLDER, id);
            string build_path = Path.Combine(folder, TEMP_FOLDER, Path.GetFileName(path));
            if (File.Exists(build_path)) File.Delete(build_path);

            using(Stream build_stream = File.Create(build_path))
            {
                List<string> storage_locations = new List<string>();
                using(StreamReader version = File.OpenText(id_path))
                {
                    //This is generally a pretty short list, so we can just get it in full and split right there
                    storage_locations.AddRange(version.ReadToEnd().Split(VERSION_FILE_SEPARATOR.ToCharArray()));
                }
                foreach (string storage_location in storage_locations)
                {
                    if (storage_location.Length > 0)
                    {
                        using (Stream data = File.OpenRead(Path.Combine(folder, STORAGE_FOLDER, storage_location)))
                        {
                            Copy(data, build_stream);
                        }
                    }
                }
            }
            try
            {
                using (Stream build_stream = File.OpenRead(build_path))
                using (Stream destination = File.OpenWrite(path))
                {
                    Copy(build_stream, destination);
                    destination.SetLength(destination.Position);
                }
                File.Delete(build_path);
                return true;
            }
            catch (IOException)
            {
                return false;
            }

        }
        private static readonly int CHUNK_SIZE = 4096;
        private List<string> PutDataInStorage(Stream file)
        {
            List<string> res = new List<string>();
            //TODO split file between files of CHUNK_SIZE size
            string filename = context.IDGenerator.Hash(file);
            string filepath = Path.Combine(folder, STORAGE_FOLDER, filename);
            if (!File.Exists(filepath))
            {
                using (Stream dest = File.Create(filepath))
                {
                    Copy(file, dest);
                }
            }
            res.Add(filename);
            return res;
        }
        private static void Copy(Stream source, Stream destination)
        {
            byte[] buffer = new Byte[1024];
            int bytesRead;

            while ((bytesRead = source.Read(buffer, 0, 1024)) > 0)
            {
                destination.Write(buffer, 0, bytesRead);
            }
        }
    }
}
