using iterate.ui.data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static iterate.file.ProjectManager;

namespace iterate.versions
{
    public class VersionManager : IDisposable
    {
        private readonly string TREE_PATH;

        private IterateApplicationContext context;

        private XmlSerializer serializer;
        private FileStream treeStream;

        public event EventHandler<VersionTreeData>? DataChanged;
        public event EventHandler<VersionNode>? CurrentVersionChanged;
        public event EventHandler<VersionNode>? VersionEdited;
        public event EventHandler<VersionNode>? VersionAdded;
        public event EventHandler<string>? VersionRemoved;

        public VersionTreeData tree;

        public VersionManager(IterateApplicationContext context, string id)
        {
            this.context = context;
            TREE_PATH = Path.Combine(context.HOME_PATH, id, "VersionTree.xml");
            serializer = new XmlSerializer(typeof(VersionTreeData));
            if (!File.Exists(TREE_PATH))
            {
                treeStream = new FileStream(TREE_PATH, FileMode.Create, FileAccess.ReadWrite);

                tree = new VersionTreeData();
                tree.CurrentNode = tree.RootId = context.versionStorage!.Store(context.projectManager.CurrentProject.Path);
                tree.Nodes.Add(new VersionNode() { Id = tree.RootId });
                serializer.Serialize(treeStream, tree);
            } else
            {
                treeStream = new FileStream(TREE_PATH, FileMode.Open, FileAccess.ReadWrite);

                tree = (serializer.Deserialize(treeStream) as VersionTreeData)!;
            }
        }
        public VersionNode? GetVersion(string key)
        {
            return tree.Nodes.Find(o => o.Id == key);
        }
        public void AddVersion(string id, VersionNode node)
        {
            VersionNode parent = tree.Nodes.Find(o => o.Id == tree.CurrentNode)!;
            node.Id = id;
            tree.Nodes.Add(node);
            parent.Children.Add(node.Id);
            tree.CurrentNode = node.Id;
            Save();
            DataChanged?.Invoke(this, tree);
            VersionAdded?.Invoke(this, node);
            CurrentVersionChanged?.Invoke(this, node);
        }
        public void SetVersion(VersionNode node)
        {
            VersionNode? changing = GetVersion(node.Id);
            if (changing == null) return;
            changing.TimeCreated = node.TimeCreated;
            changing.Description = node.Description;
            changing.Image = node.Image;
            Save();
            DataChanged?.Invoke(this, tree);
            VersionEdited?.Invoke(this, node);
        }
        public void SetCurrentVersion(string id)
        {
            if (tree.Nodes.Any(o => o.Id == id))
            {
                tree.CurrentNode = id;
                Save();
                DataChanged?.Invoke(this, tree);
                CurrentVersionChanged?.Invoke(this, GetCurrentVersion());
            }
        }
        public VersionNode GetCurrentVersion()
        {
            return GetVersion(tree.CurrentNode);
        }
        private void Save()
        {
            treeStream.Position = 0;
            serializer.Serialize(treeStream, tree);
        }

        public void Dispose()
        {
            treeStream.Dispose();
        }

        public void DeleteVersion(string id)
        {
            if (id == tree.RootId) return;
            VersionNode? deleting = GetVersion(id);
            if (deleting == null) return;

            VersionNode parent = tree.Nodes.Find(o => o.Children.Contains(id))!;
            parent.Children.Remove(id);
            parent.Children.AddRange(deleting.Children);
            if (id == tree.CurrentNode) SetCurrentVersion(parent.Id);

            tree.Nodes.Remove(deleting);
            Save();
            VersionRemoved?.Invoke(this, id);
            DataChanged?.Invoke(this, tree);
        }
    }
}
