namespace Sitecore.Support.Data.Eventing
{
  using System;
  using Sitecore.Data;
  using Sitecore.Data.DataProviders.Sql;
  using Sitecore.Diagnostics;

  class SqlServerEventQueue : Sitecore.Data.Eventing.SqlServerEventQueue
  {
    public SqlServerEventQueue(SqlDataApi api) : base(api) { }

    public SqlServerEventQueue(SqlDataApi api, Database database) : base(api, database) { }

    public override void ProcessEvents(Action<object, Type> handler)
    {
      Assert.ArgumentNotNull(handler, "handler");
      try
      {
        base.ProcessEvents(handler);
      }
      catch (Exception ex)
      {
        Trace.Info("EQ event processing failed", ex);
      }
    }
  }
}
