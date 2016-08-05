using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Xml.Linq;

namespace Syncer
{
    public partial class Syncer : ServiceBase
    {
        private List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();

        public Syncer()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var jobs = XElement.Load(config.FilePath).Element("syncJobs").Elements("syncJob").ToArray();
            foreach (XElement job in jobs)
            {
                var source = Path.Combine(job.Element("source").Value);
                var target = Path.Combine(job.Element("target").Value);
                if (!source.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    source += Path.DirectorySeparatorChar;
                if (!target.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    target += Path.DirectorySeparatorChar;

                var types = job.Element("fileTypes").Elements("type").Select(x => x.Value).ToArray();
                foreach (string type in types)
                {
                    var watcher = new FileSystemWatcher();
                    watcher.Path = source;
                    watcher.IncludeSubdirectories = true;
                    watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                    watcher.Filter = type;

                    Action<string> copy = fullPath =>
                    {
                        var targetRelPath = fullPath.Substring(fullPath.IndexOf(source) + source.Length);
                        var targetFullPath = Path.Combine(target, targetRelPath);
                        try
                        {
                            File.Copy(fullPath, targetFullPath, true);
                        }
                        catch (IOException e)
                        {
                            Debug.WriteLine(e.Message);
                            EventLog.WriteEntry(e.Message + "\r\n" + e.StackTrace, EventLogEntryType.Error);
                        }
                    };

                    var directCopy = new FileSystemEventHandler((sender, e) => copy(e.FullPath));
                    var renameCopy = new RenamedEventHandler((sender, e) => copy(e.FullPath));
                    //var del = new FileSystemEventHandler((sender, e)
                    //    => File.Delete(Path.Combine(target, e.FullPath.Substring(e.FullPath.IndexOf(source) + source.Length))));

                    watcher.Changed += directCopy;
                    watcher.Created += directCopy;
                    //watcher.Deleted += del;
                    watcher.Renamed += renameCopy;

                    watchers.Add(watcher);

                    watcher.EnableRaisingEvents = true;
                }
            }
        }

        protected override void OnStop()
        {
            watchers.RemoveAll(x => true);
        }
    }
}
