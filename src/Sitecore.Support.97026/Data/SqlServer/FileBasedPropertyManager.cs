namespace Sitecore.Support.Data.SqlServer
{
  using System;
  using System.Collections.Generic;
  using System.Collections.Concurrent;
  using System.IO;
  using System.Linq;
  using System.Text;
  using Configuration;
  using Diagnostics;
  using IO;
  using Sitecore.Data.DataProviders;

  public class FileBasedPropertyManager : IPropertyManager
  {
    // Properties folder name constant
    protected static readonly string PropertiesFolder;
    private ConcurrentDictionary<string, object> _propertyFileLock;
    private ConcurrentDictionary<string, string> _propertyValueCache;

    static FileBasedPropertyManager()
    {
      PropertiesFolder = FileUtil.MakePath(Settings.DataFolder, "properties");
      FileUtil.EnsureFolder(FileBasedPropertyManager.PropertiesFolder);

    }

    public FileBasedPropertyManager()
    {
      this._propertyFileLock = new ConcurrentDictionary<string, object>(5, 20);
      this._propertyValueCache = new ConcurrentDictionary<string, string>(5, 20);
    }

    public string BuildPropertyKey(string key, string databaseName) => $"{key}_{databaseName}";

    #region IPropertyManager members

    public string GetProperty(string key, CallContext callContext)
    {
      string value;
      Assert.ArgumentNotNullOrEmpty(key, nameof(key));
      Assert.ArgumentNotNull(callContext, nameof(callContext));
      var propertyKey = this.BuildPropertyKey(key, callContext.DataManager.Database.Name);
#if DEBUG
      Log.Info($"DBG::GetProperty({key})::propertyKey={propertyKey}", this);
#endif
      if (this._propertyValueCache.TryGetValue(propertyKey, out value))
      {
#if DEBUG
        Log.Info($"DBG::GetProperty({key})::_propertyValueCache[{propertyKey}]={value}", this);
#endif
        return value;
      }
      var filePath = this.BuildFilePath(propertyKey);
#if DEBUG
      Log.Info($"DBG::GetProperty({key})::filePath={filePath}", this);
#endif
      string fileValue = this.ReadFile(filePath);
#if DEBUG
      Log.Info($"DBG::GetProperty({key})::fileValue={fileValue}", this);
#endif
      this._propertyValueCache.AddOrUpdate(propertyKey, fileValue, (k, v) => fileValue);
      return fileValue;
    }

    public List<string> GetPropertyKeys(string prefix, CallContext callContext)
    {
      Assert.ArgumentNotNullOrEmpty(prefix, nameof(prefix));
      Assert.ArgumentNotNull(callContext, nameof(callContext));
      var files =
        Directory.GetFiles(PropertiesFolder, $"{prefix}*{callContext.DataManager.Database.Name}")
          .Select(
            path =>
              path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1)
                .Replace($"_{callContext.DataManager.Database.Name}", ""));
      var fileList = files.ToList();
#if DEBUG
      Log.Info($"DBG::GetPropertyKeys({prefix})::fileList.Count={fileList.Count}", this);
#endif
      return fileList;
    }

    public bool SetProperty(string key, string value, CallContext callContext)
    {
      Assert.ArgumentNotNullOrEmpty(key, nameof(key));
      Assert.ArgumentNotNull(callContext, nameof(callContext));
      bool success;
      string propertyFileName = BuildPropertyKey(key, callContext.DataManager.Database.Name);
#if DEBUG
      Log.Info($"DBG::SetProperty({key}, {value})::propertyFileName={propertyFileName}", this);
#endif
      string filePath = this.BuildFilePath(propertyFileName);
#if DEBUG
      Log.Info($"DBG::SetProperty({key})::filePath={filePath}", this);
#endif
      object fileLock = this.GetFileLock(filePath);
      lock (fileLock)
      {
        try
        {
          File.WriteAllText(filePath, value);
          success = true;
        }
        catch (Exception ex)
        {
          Log.Error($"Failed to set property '{filePath}'", ex, this);
          success = false;
        }
        
      }
      this._propertyValueCache.AddOrUpdate(propertyFileName, value, (k, v) => value);
      return success;
    }

#endregion IPropertyManager members

    private string BuildFilePath(string propertyFileName) => FileUtil.MakePath(PropertiesFolder, propertyFileName);

    private object GetFileLock(string filePath)
    {
      object fileLock;
      Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
      if (!this._propertyFileLock.TryGetValue(filePath, out fileLock))
      {
        fileLock = new object();
        Func<string, object, object> updateValueFactory = delegate(string key, object value)
        {
          fileLock = value;
          return value;
        };
        this._propertyFileLock.AddOrUpdate(filePath, fileLock, updateValueFactory);
      }

      return fileLock;
    }

    private string ReadFile(string filePath)
    {
      Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
      object fileLock = this.GetFileLock(filePath);
      lock (fileLock)
      {
        if (!File.Exists(filePath))
        {
          return string.Empty;
        }
        return File.ReadAllText(filePath, Encoding.UTF8).Trim();
      }
    }
  }
}
