using System;
using System.Collections.Generic;
using System.IO;

namespace WebDiff {
    class SavedWebsite {
        public bool Latest { get; internal set; }
        public DateTime Retrieved { get; set; }
        public Website URL { get; set; }

        public SavedWebsite(List<Website> sites, string path) {
            path = Path.GetFileName(path);
            int split = path.IndexOf(' ');
            Retrieved = DateTime.Parse(path[split..path.LastIndexOf('.')].Replace('_', ':'));
            ulong hashCode = ulong.Parse(path[..split]);
            URL = sites.Find(x => x.HashCode == hashCode);
        }
    }
}