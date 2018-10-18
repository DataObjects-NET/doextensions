using System;
using Xtensive.Core;
using Microsoft.AspNetCore.Builder;

namespace Xtensive.Orm.Web
{
  public static class AplicationBuilderExtensions
  {
    public static IApplicationBuilder UseSessionManager(this IApplicationBuilder builder)
    {
      return builder.UseMiddleware<SessionManagerMiddleware>();
    }

    public static IApplicationBuilder UseSessionManager(this IApplicationBuilder builder, Func<Pair<Session, System.IDisposable>> sessionProvider)
    {
      SessionManagerMiddleware.SessionProvider = sessionProvider;
      return builder.UseMiddleware<SessionManagerMiddleware>();
    }
  }
}