namespace Sitecore.Support
{
  using System;
  using System.Text;

  public class Trace
  {
    public static void Info(string message) {
      Sitecore.Diagnostics.Log.Debug(Contextualize(message));
    }

    public static void Info(string message, Exception exception)
    {
      var strBuilder = new StringBuilder();
      strBuilder.AppendLine(message);
      strBuilder.Append(exception);
      Diagnostics.Log.Debug(Contextualize(strBuilder.ToString()));
    }

    private static string Contextualize(string message) {
      return $"[DBG:97026]:: {message}";
    }
  }
}
