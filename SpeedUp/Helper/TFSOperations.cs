using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Configuration;
using System.Net;
using System.Collections.ObjectModel;

namespace SpeedUp
{
    public class TFSOperations
    {
        string collectionUrl, projectPath;
        NetworkCredential credential;

        public TFSOperations(string url, string path)
        {
            collectionUrl = url;
            projectPath = path;

            credential = new NetworkCredential();
        }

        public string CollectionUrl
        {
            get { return collectionUrl; }
            set { collectionUrl = value; }
        }

        public string ProjectPath
        {
            get { return projectPath; }
            set { projectPath = value; }
        }

        public enum PathType
        {
            File,
            Directory
        };

        public bool CheckOut(string Path, PathType PathType, int RecursionLevel = 2)
        {
            bool result = false;

            
                //CommonHelper.WriteMessage(string.Format("Check that {0} {1} exists.", (PathType)PathType, Path));

                if (PathType.Equals(PathType.File) && File.Exists(Path))
                {
                    result = CheckOut(Path, RecursionType.None);
                }
                else if (PathType.Equals(PathType.Directory) && Directory.Exists(Path))
                {
                    result = CheckOut(Path, (RecursionType)RecursionLevel);
                }
                else
                {
                    throw new Exception(string.Format("{0} {1} does not exist.", (PathType)PathType, Path));
                }
            
           

            return result;

        }

        public bool CheckIn(string Path, PathType PathType, int RecursionLevel = 2, string Comment = "I'm too lazy to write my own comment")
        {
            bool result = false;

           
                //CommonHelper.WriteMessage(string.Format("Check that {0} {1} exists.", (PathType)PathType, Path));

                if (PathType.Equals(PathType.File) && File.Exists(Path))
                {
                    result = CheckIn(Path, RecursionType.None, Comment);
                }
                else if (PathType.Equals(PathType.Directory) && Directory.Exists(Path))
                {
                    result = CheckIn(Path, (RecursionType)RecursionLevel, Comment);
                }
                else
                {
                    throw new Exception(string.Format("{0} {1} does not exist.", (PathType)PathType, Path));
                }
           

            return result;
        }

        public Dictionary<string, TfsFileData> GetTFSProjectInfo(string Path, PathType PathType, int RecursionLevel = 2)
        {
            Dictionary<string, TfsFileData> result = null;

            if (PathType.Equals(PathType.File) && File.Exists(Path))
            {
                result = GetTFSProjectInfo(Path, RecursionType.None);
            }
            else if (PathType.Equals(PathType.Directory) && Directory.Exists(Path))
            {
                result = GetTFSProjectInfo(Path, (RecursionType)RecursionLevel);
            }
            else
            {
                throw new Exception(string.Format("{0} {1} does not exist.", (PathType)PathType, Path));
            }

            return result;
        }

        private Dictionary<string, TfsFileData> GetTFSProjectInfo(string Path, RecursionType RecursionLevel)
        {
            Dictionary<string, TfsFileData> TFSFileDataDict = new Dictionary<string, TfsFileData>();
            WorkspaceInfo[] wsis = Workstation.Current.GetAllLocalWorkspaceInfo();

            foreach (WorkspaceInfo wsi in wsis)
            {
                //Ensure that all this processing is for the current server.
                if (!wsi.ServerUri.DnsSafeHost.ToLower().Equals(collectionUrl.ToLower().Replace("http://", "").Split('/')[0]))
                {
                    continue;
                }
                var workspaceName = wsi.Name;
                var owner = wsi.OwnerName;
                Workspace ws = GetWorkspace(wsi);
                ItemSet items = ws.VersionControlServer.GetItems(Path, RecursionType.Full);
                //ItemSet items = version.GetItems(@"$\ProjectName\FileName.cs", RecursionType.Full);

                foreach (Item item in items.Items)
                {
                    if (item.ItemType == ItemType.Folder)
                        continue;

                    GetRequest request = new GetRequest(new ItemSpec(item.ServerItem, RecursionType.None), VersionSpec.Latest);
                    GetStatus status = ws.Get(request, GetOptions.Preview);
                    TfsFileData tfd = new TfsFileData();
                    if (status.NumOperations == 0)
                    {
                        tfd.IsLatest = "Yes";
                    }
                    else
                    {
                        tfd.IsLatest = "No";
                    }
                    TFSFileDataDict.Add(item.ServerItem.Substring(item.ServerItem.LastIndexOf("/") + 1).ToLower(), tfd);
                }
             
                ItemSpec[] itemSpecs = new ItemSpec[1];
                itemSpecs[0] = new ItemSpec(Path, (RecursionType)RecursionType.OneLevel);
                PendingSet[] pendingSet =
                     ws.VersionControlServer.QueryPendingSets(
                                         itemSpecs,
                                         workspaceName,
                                         owner,
                                         false);
                for (int i = 0; i < pendingSet.Length; i++)
                {
                    for (int n = 0; n < pendingSet[i].PendingChanges.Length; n++)
                    {
                        if (TFSFileDataDict.ContainsKey(pendingSet[i].PendingChanges[n].FileName.ToLower()))
                        {
                            TfsFileData tfd = TFSFileDataDict[pendingSet[i].PendingChanges[n].FileName.ToLower()];
                            tfd.FileName = pendingSet[i].PendingChanges[n].FileName;
                            tfd.CheckoutBy = pendingSet[i].OwnerName;
                            tfd.Encoding = pendingSet[i].PendingChanges[n].EncodingName;
                            tfd.IsAdd = pendingSet[i].PendingChanges[n].IsAdd.ToString();
                            tfd.IsLock = pendingSet[i].PendingChanges[n].IsLock.ToString();
                        }
                    }
                }
                break;
            }
            return TFSFileDataDict;
        }
        public bool GetLatestVersion(string Path, PathType PathType, int RecursionLevel = 2)
        {
            bool result = false;


            //CommonHelper.WriteMessage(string.Format("Check that {0} {1} exists.", (PathType)PathType, Path));

            if (PathType.Equals(PathType.File) && File.Exists(Path))
            {
                result = GetLatestVersion(Path, RecursionType.None);
            }
            else if (PathType.Equals(PathType.Directory) && Directory.Exists(Path))
            {
                result = GetLatestVersion(Path, (RecursionType)RecursionLevel);
            }
            else
            {
                throw new Exception(string.Format("{0} {1} does not exist.", (PathType)PathType, Path));
            }

            return result;
        }
        private bool GetLatestVersion(string Path, RecursionType RecursionLevel)
        {
            bool result = false;
            WorkspaceInfo[] wsis = Workstation.Current.GetAllLocalWorkspaceInfo();

            foreach (WorkspaceInfo wsi in wsis)
            {
                //Ensure that all this processing is for the current server.
                if (!wsi.ServerUri.DnsSafeHost.ToLower().Equals(collectionUrl.ToLower().Replace("http://", "").Split('/')[0]))
                {
                    continue;
                }

                Workspace ws = GetWorkspace(wsi);
                
                //CommonHelper.WriteMessage(string.Format("Check-Out {0}.", Path));
                GetRequest request = new GetRequest(new ItemSpec(Path, RecursionType.Full), VersionSpec.Latest);
                GetStatus status = ws.Get(request,GetOptions.Overwrite);
                
                //CommonHelper.WriteMessage(string.Format("Checked-Out {0}.", Path));

                result = true;
                break;
            }
            return result;
        }

        private bool CheckOut(string Path, RecursionType RecursionLevel)
        {
            bool result = false;

            //CommonHelper.WriteMessage(string.Format("Get All LocalWorkspaceInfos."));

            WorkspaceInfo[] wsis = Workstation.Current.GetAllLocalWorkspaceInfo();

            foreach (WorkspaceInfo wsi in wsis)
            {
                //Ensure that all this processing is for the current server.
                if (!wsi.ServerUri.DnsSafeHost.ToLower().Equals(collectionUrl.ToLower().Replace("http://", "").Split('/')[0]))
                {
                    continue;
                }

                Workspace ws = GetWorkspace(wsi);

                //CommonHelper.WriteMessage(string.Format("Check-Out {0}.", Path));

                int status = ws.PendEdit(new string[]{Path}, (RecursionType)RecursionLevel,null,LockLevel.CheckOut, false);

                //CommonHelper.WriteMessage(string.Format("Checked-Out {0}.", Path));

                result = true;
                break;
            }

            return result;
        }

        public bool PendAdd(string Path)
        {
            bool result = false;
 
            WorkspaceInfo[] wsis = Workstation.Current.GetAllLocalWorkspaceInfo();

            foreach (WorkspaceInfo wsi in wsis)
            {
                //Ensure that all this processing is for the current server.
                if (!wsi.ServerUri.DnsSafeHost.ToLower().Equals(collectionUrl.ToLower().Replace("http://", "").Split('/')[0]))
                {
                    continue;
                }

                Workspace ws = GetWorkspace(wsi);
                ws.PendAdd(Path);

                result = true;
                break;
            }

            return result;
        }
        private bool CheckIn(string Path, RecursionType RecursionLevel, string Comment)
        {
            bool result = false;

                //CommonHelper.WriteMessage(string.Format("Get All LocalWorkspaceInfos."));

                WorkspaceInfo[] wsis = Workstation.Current.GetAllLocalWorkspaceInfo();

                foreach (WorkspaceInfo wsi in wsis)
                {
                    //Ensure that all this processing is for the current server.
                    if (!wsi.ServerUri.DnsSafeHost.ToLower().Equals(collectionUrl.ToLower().Replace("http://", "").Split('/')[0]))
                    {
                        continue;
                    }

                    Workspace ws = GetWorkspace(wsi);

                    var pendingChanges = ws.GetPendingChangesEnumerable(Path, (RecursionType)RecursionLevel);
                    
                    WorkspaceCheckInParameters checkinParamenters = new WorkspaceCheckInParameters(pendingChanges, Comment);

                    //if (RecursionLevel == 0)
                    //{
                    //    CommonHelper.WriteMessage(string.Format("Check-in {0}.", Path));
                    //}
                    //else
                    //{
                    //    CommonHelper.WriteMessage(string.Format("Check-in {0} with recursion level {1}.", Path, (RecursionType)RecursionLevel));
                    //}

                    ws.CheckIn(checkinParamenters);

                    //if (RecursionLevel == 0)
                    //{
                    //    CommonHelper.WriteMessage(string.Format("Checked-in {0}.", Path));
                    //}
                    //else
                    //{
                    //    CommonHelper.WriteMessage(string.Format("Checked-in {0}  with recursion level {1}.", Path, (RecursionType)RecursionLevel));
                    //}

                    result = true;
                    break;
                }

            
           

            return result;
        }

        public bool UndoPendingChange(string Path, PathType PathType, int RecursionLevel = 2)
        {
            bool result = false;


            //CommonHelper.WriteMessage(string.Format("Check that {0} {1} exists.", (PathType)PathType, Path));

            if (PathType.Equals(PathType.File) && File.Exists(Path))
            {
                result = UndoPendingChange(Path, RecursionType.None);
            }
            else if (PathType.Equals(PathType.Directory) && Directory.Exists(Path))
            {
                result = UndoPendingChange(Path, (RecursionType)RecursionLevel);
            }
            else
            {
                throw new Exception(string.Format("{0} {1} does not exist.", (PathType)PathType, Path));
            }

            return result;
        }
        private bool UndoPendingChange(string Path, RecursionType RecursionLevel)
        { 
            bool result = false;

            WorkspaceInfo[] wsis = Workstation.Current.GetAllLocalWorkspaceInfo();

            foreach (WorkspaceInfo wsi in wsis)
            {
                //Ensure that all this processing is for the current server.
                if (!wsi.ServerUri.DnsSafeHost.ToLower().Equals(collectionUrl.ToLower().Replace("http://", "").Split('/')[0]))
                {
                    continue;
                }

                Workspace ws = GetWorkspace(wsi);

                PendingChange[] pendingChanges = ws.GetPendingChanges(new ItemSpec[]{new ItemSpec(Path, RecursionLevel)}, false);
                ws.Undo(pendingChanges);
     
                result = true;
                break;
            }

            return result;
            
        }

        private Workspace GetWorkspace(WorkspaceInfo wsi)
        {

            TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(wsi.ServerUri);

            //CommonHelper.WriteMessage(string.Format("Get Workspace."));

            Workspace ws = wsi.GetWorkspace(tpc);

            return ws;
        }

    }
}
