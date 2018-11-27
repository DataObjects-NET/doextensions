using System;
using Xtensive.Core;
using Microsoft.AspNetCore.Builder;

namespace Xtensive.Orm.Web
{
  public static class AplicationBuilderExtensions
  {
    public static IApplicationBuilder UseSessionManager(this IApplicationBuilder builder)
    {
      return builder.UseMiddleware<SessionManager>();
    }

    public static IApplicationBuilder UseSessionManager(this IApplicationBuilder builder, Func<Pair<Session, System.IDisposable>> sessionProvider)
    {
      SessionManager.SessionProvider = sessionProvider;
      return builder.UseMiddleware<SessionManager>();
    }
  }
}