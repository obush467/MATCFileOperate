using log4net;
using MATCFileOperate;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MATCFileOperate
{
    public class kf : IEqualityComparer<kFileInfo>
    {
        public bool Equals(kFileInfo x, kFileInfo y)
        {
            if (x.FullName == y.FullName && x.Length == y.Length)
            {
                if (
                  //&& x.LastWriteTime==y.LastWriteTime
                  DateTimeOffset.Compare((DateTimeOffset)x.CreationTimeUtc, (DateTimeOffset)y.CreationTimeUtc)==0
                  )
                { return true; }
                else { return false; }
            }
            else return false;
        }

        public int GetHashCode(kFileInfo obj)
        {
            return obj.GetHashCode();
        }
    }
    public class FileOperator
    {
        public FileOperator(string connectionString,ILog log):this(new FileContext(connectionString), log)
        {}

        public FileOperator(DbConnection dbConnection, ILog log, bool contextOwnsConnection) : this(new FileContext(dbConnection, contextOwnsConnection), log)
        { }
        public FileOperator(FileContext context, ILog log)
        {
            Log = log;
            Context = context;
        }
        public FileContext Context { get; set; } 
        public ILog Log { get; set; }
        public void Load(IEnumerable<DirectoryInfo> Directories)
        {
            Log.Info("Старт");
            Context.Configuration.AutoDetectChangesEnabled = false;
            int n = 1;
            List<kFileInfo> filesContextList = Context.Files.ToList();
            //List<kFileInfo> AddList= new List<kFileInfo>();
            Task _save = SaveTask();
            try
            {
                foreach (DirectoryInfo Directory in Directories)
                {
                    List<FileInfo> Files = Directory.EnumerateFiles("*", SearchOption.AllDirectories)
                        .Where(seq => (new FileIOPermission(FileIOPermissionAccess.AllAccess, seq.FullName))
                            .GetPathList(FileIOPermissionAccess.Read).Count() > 0 & (seq.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden & seq.DirectoryName != "DfsrPrivate" & seq.Name != "DfsrPrivate").ToList<FileInfo>();
                    Log.Info(Files.Count + " файлов в " + Directory.FullName);
                    List<FileInfo> files_not_changed =
                    (from fileO in Files
                     join file1 in filesContextList
on new { fileO.FullName, fileO.Length }
    equals
    new { file1.FullName, file1.Length }
                     where fileO.LastWriteTimeUtc.CompareTo(fileO.LastWriteTimeUtc) == 0
                     select fileO).ToList();
                    Log.Info(files_not_changed.Count + " было");
                    Files.RemoveAll(p => files_not_changed.Contains(p));
                    Log.Info(files_not_changed.Count + " стало");
                    foreach (var File in Files)
                    {
                        kFileInfo kFile = new kFileInfo(File);
                        //kFileInfo kFile1 = Context.Files.Create<kFileInfo>();
                        //kFile1.FullName = File.FullName;
                        //kFile1.Name = File.Name;
                        //kFile1.IsReadOnly = File.IsReadOnly;
                        //kFile1.CreationTime = File.CreationTime;
                        //kFile1.CreationTimeUtc = File.CreationTimeUtc;
                        //kFile1.LastAccessTime = File.LastAccessTime;
                        //kFile1.LastAccessTimeUtc = File.LastAccessTimeUtc;
                        //kFile1.LastWriteTime = File.LastWriteTime;
                        //kFile1.LastWriteTimeUtc = File.LastWriteTimeUtc;
                        //kFile1.DirectoryName = File.DirectoryName;
                        //kFile1.Length = File.Length;
                        //kFile1.Attributes = (int)File.Attributes;
                        kf K = new kf();
                        if (filesContextList.Contains(kFile, K))
                        { Log.Info(n.ToString() + " " + File.Name + " уже есть"); }
                        else
                        {
                            //AddList.Add(kFile1);
                            Context.Files.Add(kFile);
                            Log.Info(n.ToString() + " " + File.Name);
                        }
                        n++;
                    };
                };
            }
            catch (SecurityException e)
            { Log.Error(e); }
            catch (UnauthorizedAccessException e)
            { Log.Error(e); }

            catch (IOException e)
            { Log.Error(e); }
            catch (Exception e)
            { Log.Error(e); }
            finally
            {
                _save.Dispose();
                Save();
            }
        }
        public void MD5CalculateParallel(int LevelParalellism = 3)
        {

            int n = 1;
            var l = Context.Files.Where(vfile => (vfile.MD5 == null)).ToList();
            Context.Configuration.AutoDetectChangesEnabled = false;
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = LevelParalellism;
            Task _save = SaveTask();
            try
            {
                Parallel.ForEach<kFileInfo>(l, po, async f =>
                  {
                      if (File.Exists(f.FullName))
                      {
                          MD5 md5 = MD5.Create();
                          FileStream sr = File.OpenRead(f.FullName);
                          byte[] srresult = new byte[sr.Length];
                          await sr.ReadAsync(srresult, 0, (int)sr.Length);
                          f.MD5 = md5.ComputeHash(srresult);
                          f.Length = (new FileInfo(f.FullName)).Length;
                          Log.Info(n.ToString() + " " + f.Name);
                          n++;

                      }
                  });
            }

            finally
            {
                _save.Dispose();
                Save();
            }

        }

        public async Task SaveTask()
        {
            while (1 == 1)
            {
                await Task.Delay(10000);
                Save();
            }

        }
        public void Save()
        {
            try
            {
                Context.ChangeTracker.DetectChanges();
                Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException e)
            {
                Log.Error("Команда базы данных не повлияла на ожидаемое количество трок.Это обычно означает конфликт оптимистичного параллелизма; то есть, строка в базе данных была изменена со времени запроса.",e);
            }
            catch (DbUpdateException e)
            {
                Log.Error("Произошла ошибка при отправке обновлений в базу данных.", e);
            }
            catch (DbEntityValidationException e)
            {
                Log.Error("Сохранение прервано, поскольку произошел сбой проверки значений свойства сущности.", e);
            }
            catch (NotSupportedException e)
            {
                Log.Error("Попытка использовать неподдерживаемое поведение, такое как выполнение нескольких асинхронных команд в параллельном режиме в том же экземпляре контекста.", e);
            }
        }

        public List<FileInfo> EnumerateFiles(DirectoryInfo[] directories)
        {
            List<FileInfo> files = new List<FileInfo>();
            try
            {
                foreach (DirectoryInfo dir in directories)
                {
                    if (dir.Name != "DfsrPrivate")
                    {
                        try
                        {
                            if (CanDirectoryProcessing(dir))
                            {
                                files.AddRange(dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Where(
                                    f => {
                                        if (f.Name == "Thumbs.db" ||
                                            ((f.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) ||
                                            ((f.Attributes & FileAttributes.System) == FileAttributes.System) ||
                                            ((f.Attributes & FileAttributes.Temporary) == FileAttributes.Temporary) ||
                                            ((f.Attributes & FileAttributes.Offline) == FileAttributes.Offline))
                                        { return false; }
                                        else
                                        { return true; }
                                    }));
                                files.AddRange(EnumerateFiles(dir.GetDirectories()));
                            }
                            //else
                            //Console.WriteLine("Доступ к папке {0} для пользователя - {1} запрещен", dir.Name, wi.Name);

                        }
                        catch (SecurityException e)
                        {
                            //Console.WriteLine("Доступ к папке {0} для пользователя - {1} запрещен", dir.Name, wi.Name);
                        }
                        catch (UnauthorizedAccessException) { }

                    }
                }
            }
            finally { }
            return files;
        }
        public bool CanDirectoryProcessing(DirectoryInfo directory)
        {
            WindowsIdentity user = WindowsIdentity.GetCurrent();
            bool bAllowed = false;
            try
            {
                //Получает набор разрешений для папки
                DirectorySecurity sec = directory.GetAccessControl(AccessControlSections.Access);
                AuthorizationRuleCollection dacls = sec.GetAccessRules(true, true, typeof(SecurityIdentifier));
                //Перечисляет access rule
                foreach (FileSystemAccessRule dacl in dacls)
                {
                    SecurityIdentifier sid = (SecurityIdentifier)dacl.IdentityReference;
                    //If the right is either create files or write access
                    if (
                            ((dacl.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write) ||
                            ((dacl.FileSystemRights & FileSystemRights.DeleteSubdirectoriesAndFiles) == FileSystemRights.DeleteSubdirectoriesAndFiles)
                        )
                    {
                        if ((sid.IsAccountSid() && user.User == sid) ||
                            (/*sid.IsAccountSid() &&*/ user.Groups.Contains(sid)))
                        {
                            if (dacl.AccessControlType == AccessControlType.Deny)
                                return false;
                            bAllowed = true;
                        };
                    };
                };
                return bAllowed;
            }
            catch (SecurityException e)
            {
                return false;
            }
            catch (UnauthorizedAccessException) { return false; }
        }

        public List<FileInfoW> lsToFileInfoW(DirectoryInfo rootDirectory)
        {
            List<FileInfoW> res = new List<FileInfoW>();
            List<DirectoryInfo> dirlist = rootDirectory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).ToList();
            if (dirlist != null & dirlist.Count > 0)
            { }
            return res;
        }
        public List<FileInfoW> lsToFileInfoW(List<FileInfo> fileIs,string pattern)
        {
            List<FileInfoW> res = new List<FileInfoW>();
            foreach (FileInfo fileI in fileIs)
            {
                FileInfoW vFileInfoW = new FileInfoW(fileI);
                vFileInfoW.Name = new matcFileName(fileI,pattern);
                res.Add(vFileInfoW);
            }
            return res;
        }
        public List<FileInfoW> lsToFileInfoW(DirectoryInfo rootDirectory,string pattern)
        {
            return lsToFileInfoW(EnumerateFiles(new DirectoryInfo[] {rootDirectory}),pattern);
        }

        public List<FileInfoW> CalcNumber(List<FileInfoW> list)
        {
            foreach (FileInfoW fileW in list)
            {
                fileW.Name.Number = regexNumber(fileW.fileInfo, "\\d{5}ГУ\\d{6}");
            }
                return list;
        }
        public string regexNumber(FileInfo file,string regexpattern)
        {
            Regex regex = new Regex(regexpattern);
            MatchCollection mc = regex.Matches(file.FullName);
            if (mc.Count > 0)
            { return mc[mc.Count - 1].Value; }
            else return null;
        }

        public DirectoryInfo DestinationDirectory { get; set; }
        public List<FileInfoW> DestinationFilesList { get; set; }
        public List<FileInfoW> TransferableFilesList { get; set; }
        public List<string> TransferableNumbersList { get; set; }
        public void calculateTransferableFilesList()
        {
            if (TransferableFilesList != null)
            {
                TransferableNumbersList = TransferableFilesList.Select(sel => sel.Name.Number).Distinct().ToList();
            }
            else
            {
                TransferableNumbersList = null;
            }
        }
        public void LoadDestinationByNumbers()
        {
            if (TransferableNumbersList != null)
            {
                var dds = DestinationDirectory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);

                var innerGroupJoinQuery =
                from dd in dds
                join tnl in TransferableNumbersList on dd.Name equals tnl
                select dd;
                var r = innerGroupJoinQuery.ToList();

                if (DestinationFilesList == null) DestinationFilesList = new List<FileInfoW>();
                foreach (var ddir in innerGroupJoinQuery)
                {
                    DestinationFilesList.AddRange(lsToFileInfoW(ddir, "^(\\d{5}ГУ\\d{6})(?:_(.*))*_(\\d{8})(?:_(\\d{1,2}))*$"));
                }
                var yyy1 = DestinationFilesList.Where(sel => sel.Name.Number == null).ToList();
                CalcNumber(yyy1);
                foreach (var y in yyy1)
                {
                    y.NeedRename = true;
                    y.Name.Date = y.fileInfo.LastWriteTime.ToString("yyyyMMdd");
                    y.Name.Content = "";
                }
                var dgg =
                    from dfl in DestinationFilesList
                    where
                    dfl.NeedRename == false
                    group dfl by new { dfl.Name.Number, dfl.Name.Content, dfl.Name.Date } into dfl1
                    where dfl1.Count() > 1
                    select new { dfl1.Key.Number, dfl1.Key.Content, dfl1.Key.Date };
                var ttt = dgg.ToList();

                var dgg1 =
                    from dfl in DestinationFilesList
                    where
                    dfl.NeedRename == true
                    group dfl by new { Number = dfl.Name.Number, dfl.Name.Content, dfl.Name.Date } into dfl2
                    where dfl2.Count() > 1
                    select new { dfl2.Key.Number, dfl2.Key.Content, dfl2.Key.Date }; ;
                var ttt1 = dgg1.ToList();
                var dgg2 =
                    from ttt1i in ttt1.AsEnumerable()
                    join ttti in ttt.AsEnumerable() on ttt1i.Number equals ttti.Number
                    select new { ttti, ttt1i };
                var ttt2 = dgg2.ToList();
            }
        }

        public void MD5CalculateParallel(List<FileInfoW> files, int LevelParalellism = 3)
        {

            int n = 1;
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = LevelParalellism;
            try
            {
                Parallel.ForEach<FileInfoW>(files, po, f =>
                {
                    if (f.fileInfo.Exists)
                    {
                        MD5 md5 = MD5.Create();
                        try
                        {
                            FileStream sr = f.fileInfo.OpenRead();
                            byte[] srresult = new byte[sr.Length];
                            sr.Read(srresult, 0, (int)sr.Length);
                            f.MD5 = md5.ComputeHash(srresult);
                            Log.Info(n.ToString() + " " + f.fileInfo.FullName);
                        }
                        catch (Exception)
                        {
                            Log.Info(n.ToString() + " ошибка " + f.fileInfo.FullName); 
                        }

                        n++;
                    }
                });
            }

            finally
            {

            }

        }

        public Task tcreate(List<FileInfoW> dl, List<FileInfoW> tl, Task PrevTask)
        {
            Task res;
            Action<Task[]> rr=(t)=>
            
                {
                    Task t2 = Task.Factory.StartNew(() =>
                    { MD5CalculateParallel(tl, 7); });
                    Task t1 = Task.Factory.StartNew(() =>
                    { MD5CalculateParallel(dl, 7); });
                    
                    Task t3 = Task.Factory.ContinueWhenAll(new Task[] { t1, t2 }, (tt) => {
                    } );
                    Task t4 = Task.Factory.ContinueWhenAll(new Task[] { t3 }, (tt) => { });
                    t4.Wait();

                };
            if (PrevTask == null)
            {
                res = Task.Factory.StartNew(() =>
                    {
                        Log.Info(dl[0].Name.Number + " запущен!!!!");
                        Task t1 = Task.Factory.StartNew(() =>
                        { MD5CalculateParallel(dl, 7);
                            Log.Info(dl[0].Name.Number + " посчитан 1");
                        });
                        Task t2 = Task.Factory.StartNew(() =>
                        { MD5CalculateParallel(tl, 7);
                            Log.Info(dl[0].Name.Number + " посчитан 2");
                        });
                        Task t3 = Task.Factory.ContinueWhenAll(new Task[] { t1, t2 }, (tt) => {
                             });
                        Task t4 = Task.Factory.ContinueWhenAll(new Task[] { t3 }, markToRenameReplace => { Log.Info(dl[0].Name.Number + " посчитан 4"); });
                        t4.Wait();
                    });
            }
            else
            {
                res = Task.Factory.ContinueWhenAll(new Task[] { PrevTask }, rr);
            }
            return res;
        }

        public List<Task> CreateTasks()
        {
            List<Task> res = new List<Task>();
            Task lasttask=null;
            foreach (string Num in TransferableNumbersList)
            {
                List<FileInfoW> destlist = DestinationFilesList.Where(w => w.Name.Number == Num).ToList();
                List<FileInfoW> translist = TransferableFilesList.Where(w => w.Name.Number == Num).ToList();
                Task newtask= tcreate(destlist, translist, lasttask);
                res.Add(newtask);
                lasttask = newtask;
                
            }
            return res;
        }
        public List<IGrouping<string, FileInfoW>> selectDubbles(List<List<FileInfoW>> lists)
        {
            List<FileInfoW> tmp = new List<FileInfoW>();
            foreach (List<FileInfoW>  l in lists)
            { tmp.AddRange(l); }
            var hashes =
                from tmpi in tmp
                where tmpi.MD5!=null
                group tmpi by  new { MD5 = BitConverter.ToString(tmpi.MD5) }   into tmpign
                where tmpign.Count() > 1
                select new { tmpign.Key.MD5};
            var res =
                from tmpi in tmp.AsEnumerable()
                where tmpi.MD5 != null
                join h in hashes.AsEnumerable() 
                on BitConverter.ToString(tmpi.MD5) equals h.MD5                
                group tmpi by BitConverter.ToString(tmpi.MD5) into tmpi1
                select tmpi1;
            return res.ToList();
        }
    }
}
