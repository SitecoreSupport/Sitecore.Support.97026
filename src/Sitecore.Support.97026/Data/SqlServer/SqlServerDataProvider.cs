namespace Sitecore.Support.Data.SqlServer
{
  using System.Collections.Generic;
  using Sitecore.Data.DataProviders;
  using Sitecore.Eventing;

  public class SqlServerDataProvider: Sitecore.Data.SqlServer.SqlServerDataProvider
  {

    public SqlServerDataProvider(string connectionString) : base(connectionString)
    {
      PropertyManager = new FileBasedPropertyManager();
    }

    public override EventQueue GetEventQueue()
    {
      return new Sitecore.Support.Data.Eventing.SqlServerEventQueue(this.Api, this.Database);
    }

    protected override bool SetPropertyCore(string propertyName, string value, CallContext context)
    {
      return PropertyManager.SetProperty(propertyName, value, context);
    }

    protected override string GetPropertyCore(string propertyName, CallContext context)
    {
      return PropertyManager.GetProperty(propertyName, context);
    }

    public override List<string> GetPropertyKeys(string prefix, CallContext context)
    {
      return PropertyManager.GetPropertyKeys(prefix, context);
    }

    protected IPropertyManager PropertyManager { get; set; }
  }
}
