namespace Sitecore.Support.Data.Properties
{
  #region Usings

  using Sitecore;
  using Sitecore.Abstractions;
  using Sitecore.Configuration;
  using Sitecore.Data.DataProviders.Sql;
  using Sitecore.Data.Properties;
  using Sitecore.Diagnostics;
  using Sitecore.IO;
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;

  #endregion

  public class FileSystemStore : PropertyStore
  {
    #region Fields

    protected static readonly string PropertiesFolder;
    private ConcurrentDictionary<string, object> _propertyFileLock;
    private ConcurrentDictionary<string, string> _propertyValueCache;

    #endregion

    #region Constructors

    static FileSystemStore()
    {
      string path = AppContext.BaseDirectory + FileUtil.MakePath(Settings.DataFolder.Replace(@"/", @"\"), "properties");
      PropertiesFolder = path.Replace(@"\\", @"\");
      FileUtil.EnsureFolder(PropertiesFolder);
    }

    public FileSystemStore(SqlDataApi api, BaseEventManager eventManager, BaseCacheManager cacheManager) :
      base(eventManager, cacheManager)
    {
      _propertyFileLock = new ConcurrentDictionary<string, object>(5, 20);
      _propertyValueCache = new ConcurrentDictionary<string, string>(5, 20);
    }

    #endregion

    #region Methods

    protected override ICollection<string> GetPropertyKeysCore(string fullPrefix)
    {
      Assert.ArgumentNotNullOrEmpty(fullPrefix, nameof(fullPrefix));

      var files =
        Directory.GetFiles(PropertiesFolder, $"{Prefix}*")
          .Select(
            path =>
              path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1)
                .Replace($"{Prefix}_", ""));
      var fileList = files.ToList();

      Log.Info($"DBG::GetPropertyKeys({fullPrefix})::fileList.Count={fileList.Count}", this);

      return fileList;
    }

    public override string GetStringValue(string name)
    {
      Assert.ArgumentNotNullOrEmpty(name, "name");
      string[] values = new string[] { this.GetStringValueCore(name) };

      return StringUtil.GetString(values);
    }

    protected override string GetStringValueCore(string prefixedName)
    {
      string value;
      string propertyKey = prefixedName;

      if (!prefixedName.StartsWith($"{Prefix}_") || !prefixedName.StartsWith($"WEB_")
        || !prefixedName.StartsWith($"MASTER_") || !prefixedName.StartsWith($"CORE_"))
      {
        propertyKey = GetPrefixedPropertyName(prefixedName).ToUpperInvariant();
      }


      Log.Info($"DBG::GetProperty({propertyKey})::propertyKey={propertyKey}", this);

      if (_propertyValueCache.TryGetValue(propertyKey, out value))
      {

        Log.Info($"DBG::GetProperty({propertyKey})::_propertyValueCache[{propertyKey}]={value}", this);

        return value;
      }

      var filePath = BuildFilePath(propertyKey);

      Log.Info($"DBG::GetProperty({propertyKey})::filePath={filePath}", this);

      string fileValue = ReadFile(filePath);

      Log.Info($"DBG::GetProperty({propertyKey})::fileValue={fileValue}", this);

      _propertyValueCache.AddOrUpdate(propertyKey, fileValue, (k, v) => fileValue);

      return fileValue;
    }

    protected override void RemoveCore(string prefixedName)
    {
    }

    protected override void RemovePrefixCore(string fullPrefix)
    {
    }

    protected override void SetStringValueCore(string prefixedName, string value)
    {
      Assert.ArgumentNotNull(prefixedName, nameof(prefixedName));
      Assert.ArgumentNotNull(value, nameof(value));

      prefixedName = prefixedName.ToUpperInvariant();

      Log.Info($"DBG::SetProperty({prefixedName}, {value})::propertyFileName={prefixedName}", this);

      string filePath = BuildFilePath(prefixedName);

      CreateFileIfNeeded(filePath);

      Log.Info($"DBG::SetProperty({prefixedName})::filePath={filePath}", this);

      object fileLock = this.GetFileLock(filePath);

      lock (fileLock)
      {
        try
        {
          File.WriteAllText(filePath, value);
        }
        catch (Exception ex)
        {
          Log.Error($"Failed to set property '{filePath}'", ex, this);
        }
      }

      _propertyValueCache.AddOrUpdate(prefixedName, value, (k, v) => value);
    }

    #endregion

    #region Private Methods

    private string ReadFile(string filePath)
    {
      Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
      object fileLock = GetFileLock(filePath);
      lock (fileLock)
      {
        if (!File.Exists(filePath))
        {
          return string.Empty;
        }
        return File.ReadAllText(filePath, Encoding.UTF8).Trim();
      }
    }

    private void CreateFileIfNeeded(string filePath)
    {
      if (File.Exists(filePath))
      {
        return;
      }

      try
      {
        File.Create(filePath);
      }
      catch (Exception ex)
      {
        Log.Error($"Failed to create file property '{filePath}'", ex, this);
      }
    }

    private string BuildPropertyKey(string key, string databaseName) => $"{key}_{databaseName}";

    private object GetFileLock(string filePath)
    {
      object fileLock;

      Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

      if (!_propertyFileLock.TryGetValue(filePath, out fileLock))
      {
        fileLock = new object();
        Func<string, object, object> updateValueFactory = delegate (string key, object value)
        {
          fileLock = value;
          return value;
        };
        _propertyFileLock.AddOrUpdate(filePath, fileLock, updateValueFactory);
      }

      return fileLock;
    }

    private string BuildFilePath(string propertyFileName)
    {
      string path = FileUtil.MakePath(PropertiesFolder, propertyFileName);
      return path.Replace(@"\\", @"\");
    }

    #endregion
  }
}