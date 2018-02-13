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
using System.Text;
using System.Threading.Tasks;

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
        public async Task Load(IEnumerable<DirectoryInfo> Directories)
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
            { Log.Error(e);  }
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
        public async Task MD5CalculateParallel(int LevelParalellism = 3)
        {

            int n = 1;
            var l = Context.Files.Where(vfile => (vfile.MD5 == null)).ToList();
            Context.Configuration.AutoDetectChangesEnabled = false;
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = LevelParalellism;
            Task _save = SaveTask();
            try
            {
                Parallel.ForEach<kFileInfo>(l, po, f =>
                  {
                      if (File.Exists(f.FullName))
                      {
                          MD5 md5 = MD5.Create();
                          f.MD5 = md5.ComputeHash(File.ReadAllBytes(f.FullName));
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

    }
}
