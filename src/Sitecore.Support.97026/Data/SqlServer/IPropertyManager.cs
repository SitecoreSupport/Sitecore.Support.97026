namespace Sitecore.Support.Data.SqlServer
{
  using System.Collections.Generic;
  using Sitecore.Data.DataProviders;

  public interface IPropertyManager
  {
    string GetProperty(string key, CallContext callContext);
    List<string> GetPropertyKeys(string prefix, CallContext callContext);
    bool SetProperty(string key, string value, CallContext callContext);

  }
}