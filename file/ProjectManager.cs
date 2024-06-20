using System.Xml.Serialization;

namespace iterate.file
{
    public class ProjectManager
    {
        public class ProjectData
        {
            public string Id;
            public string Path;
        }
        private readonly string HOME_PATH;


        private List<ProjectData> projects;
        private IterateApplicationContext context;
        private string currentId;

        public ProjectData CurrentProject
        {
            get
            {
                ProjectData current = projects.Find(x => x.Id == currentId);
                return new ProjectData()
                {
                    Id = current.Id,
                    Path = current.Path
                };
            }
        }


        public ProjectManager(IterateApplicationContext context) {
            this.context = context;
            HOME_PATH = Path.Combine(context.HOME_PATH, "RepositoryList.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(HOME_PATH));
            projects = LoadDictionary();
        }

        private List<ProjectData> LoadDictionary()
        {
            if (File.Exists(HOME_PATH))
            {
                try
                {
                    using (var stream = new FileStream(HOME_PATH, FileMode.Open))
                    {
                        var serializer = new XmlSerializer(typeof(List<ProjectData>));
                        var list = (List<ProjectData>)serializer.Deserialize(stream);
                        return list;
                    }
                }
                catch
                {
                    return new List<ProjectData>();
                }
            }
            else
            {
                projects = new List<ProjectData>();
                SaveDictionary();
                return new List<ProjectData>();
            }
        }

        private void SaveDictionary()
        {
            using (var stream = new FileStream(HOME_PATH, FileMode.Create))
            {
                var serializer = new XmlSerializer(typeof(List<ProjectData>));
                serializer.Serialize(stream, projects);
            }
        }

        public bool SelectProject(string id)
        {
            ProjectData project = projects.Find(o => o.Id == id);
            if (project == null) return false;
            currentId = project.Id;
            return true;
        }

        public string AddFilePath(string filePath)
        {
            ProjectData project = projects.Find(o => o.Path == filePath);
            if (project == null)
            {
                string id = Path.GetFileNameWithoutExtension(filePath) + "-" + context.IDGenerator.Generate();
                project = new ProjectData() { Id = id, Path = filePath };
                projects.Add(project);
                SaveDictionary();
            }
            return project.Id;
        }
        public bool UpdateFilePath(string id, string filePath)
        {
            ProjectData project = projects.Find(o => o.Id == id);
            if (project == null) return false;
            project.Path = filePath;
            SaveDictionary();
            return true;
        }

        public bool RemoveProject(string id)
        {
            int index = projects.FindIndex(o=>o.Id == id);
            if (index == -1) return false;
            projects.RemoveAt(index);
            SaveDictionary();
            return true;
        }

        public string GetPath(string id)
        {
            return projects.Find(o => o.Id == id)?.Path;
        }

        public List<ProjectData> GetAllProjects()
        {
            return new List<ProjectData>(projects);
        }
    }
}