using System;
using System.Web;
using System.Web.Configuration;
using Xtensive.Core;

namespace Xtensive.Orm.Web
{
  /// <summary>
  /// Provides access to current <see cref="Domain"/> and <see cref="Session"/> for web applications;
  /// ensures <see cref="Domain"/> is built, and built just once.
  /// </summary>
  /// <remarks>
  /// To initialize this class, add it to <see cref="HttpModulesSection"/> configuration section 
  /// in <c>web.config</c> file and set its <see cref="DomainBuilder"/> in
  /// <c>Application_Start</c> method of your <c>Global.asax.cs</c> file.
  /// </remarks>
  /// <example>
  /// <c>web.config</c>:
  /// <code>
  /// &lt;configuration&gt;
  ///   &lt;system.web&gt;
  ///     &lt;httpModules&gt;
  ///       &lt;add name="SessionManager" type="Xtensive.Orm.Web.SessionManager, Xtensive.Orm"/&gt;
  ///     &lt;/httpModules&gt;
  ///   &lt;/system.web&gt;
  /// &lt;/configuration&gt;
  /// </code>
  /// <c>Global.asax.cs</c>:
  /// <code>
  ///   public class Global : System.Web.HttpApplication
  ///   {
  ///     protected void Application_Start(object sender, EventArgs e)
  ///     {
  ///       SessionManager.DomainBuilder = DomainBuilder.Build;
  ///     }
  ///   }
  /// </code>
  /// </example>
  public class SessionManager : IHttpModule
  {
    private static readonly object currentItemKey = new object();
    private static readonly object domainBuildLock = new object();
    private static Func<Domain> domainBuilder;
    private static Func<Pair<Session, IDisposable>> sessionProvider;
    private static volatile Domain domain;

    private readonly object provideSessionLock = new object();
    private volatile Session session;
    private IDisposable resource;

    /// <summary>
    /// Sets the domain builder delegate.
    /// This delegate is invoked to build the domain on first request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The setter of this property can be invoked just once per application lifetime,
    /// usually in <c>Application_Start</c> method in <c>Global.asax.cs</c>.
    /// The assigned domain builder can not be changed.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">Domain builder is already assigned.</exception>
    public static Func<Domain> DomainBuilder {
      protected get {
        return domainBuilder;
      }
      set {
        if (domainBuilder!=null)
          throw Exceptions.AlreadyInitialized("DomainBuilder");
        ArgumentValidator.EnsureArgumentNotNull(value, "value");
        domainBuilder = value;
      }
    }

    /// <summary>
    /// Sets the session provider delegate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This delegate is invoked to open the <see cref="Session"/> on the first attempt
    /// to read this property (see <see cref="HasSession"/> as well) in each web request.
    /// Normally, this delegate must also ensure a transaction is created there.
    /// </para>
    /// <para>
    /// The setter of this property can be invoked just once per application lifetime; 
    /// assigned session provider can not be changed.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">Session provider is already assigned.</exception>
    public static Func<Pair<Session, IDisposable>> SessionProvider {
      protected get {
        return sessionProvider;
      }
      set {
        if (sessionProvider!=null)
          throw Exceptions.AlreadyInitialized("SessionProvider");
        ArgumentValidator.EnsureArgumentNotNull(value, "value");
        sessionProvider = value;
      }
    }

    /// <summary>
    /// Gets the <see cref="SessionManager"/> instance
    /// bound to the current <see cref="HttpRequest"/>.
    /// </summary>
    public static SessionManager Current {
      get {
        var httpContext = HttpContext.Current;
        return httpContext==null ? null : (SessionManager) httpContext.Items[currentItemKey];
      }
      protected set {
        HttpContext.Current.Items[currentItemKey] = value;
      }
    }

    /// <summary>
    /// Gets the <see cref="SessionManager"/> instance 
    /// bound to the current <see cref="HttpRequest"/>,
    /// or throws <see cref="InvalidOperationException"/>, 
    /// if <see cref="Current"/> is <see langword="null" />.
    /// </summary>
    /// <returns>Current <see cref="SessionManager"/>.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Current"/> is <see langword="null" />.</exception>
    public static SessionManager Demand()
    {
      var current = Current;
      if (current==null)
        throw new InvalidOperationException("There is no current HttpRequest or SessionManager isn't bound to it yet.");
      return current;
    }

    /// <summary>
    /// Gets the domain used in web application.
    /// </summary>
    public static Domain Domain {
      get {
        EnsureDomainIsBuilt();
        return domain;
      }
    }

    /// <summary>
    /// Gets a value indicating whether current <see cref="SessionManager"/> has session.
    /// </summary>
    public bool HasSession {
      get { return session!=null; }
    }

    /// <summary>
    /// Gets or sets the session for the current <see cref="HttpContext"/>.
    /// </summary>
    public Session Session {
      get {
        EnsureSessionIsProvided();
        return session;
      }
    }

    /// <summary>
    /// Gets or sets value indicating whether an error has occurred 
    /// on execution of the current request and transaction should be rollbacked.
    /// </summary>
    public bool HasErrors { get; set; }

    #region Protected BeginRequest, Error, EndRequest & ProvideSession methods

    /// <summary>
    /// Handles request beginning.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    protected virtual void BeginRequest(object sender, EventArgs e)
    {
      if (resource!=null)
        try {
          EndRequest(null, null);
        }
        finally {
          resource = null;
        }

      HasErrors = false;
      Current = this;

      EnsureDomainIsBuilt();
    }
    
    /// <summary>
    /// Handles request processing error.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    protected virtual void Error(object sender, EventArgs e)
    {
      HasErrors = true;
    }

    /// <summary>
    /// Completes request processing.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    protected virtual void EndRequest(object sender, EventArgs e)
    {
      lock (provideSessionLock)
        try {
          resource.DisposeSafely();
        }
        finally {
          resource = null;
          session = null;
        }
    }

    /// <summary>
    /// Default <see cref="SessionProvider"/> implementation.
    /// </summary>
    /// <returns>A pair of <see cref="Session"/> and <see cref="IDisposable"/> 
    /// to invoke on request completion.</returns>
    protected virtual Pair<Session, IDisposable> ProvideSession()
    {
      var newSession = Domain.OpenSession(); // Open, but don't activate!
      var transactionScope = newSession.OpenTransaction();
      var newResource = transactionScope.Join(newSession);
      return new Pair<Session, IDisposable>(newSession, new Disposable(disposing => {
        try {
          if (!HasErrors)
            transactionScope.Complete();
        }
        finally {
          newResource.DisposeSafely();
        }
      }));
    }

    #endregion

    #region IHttpModule members

    /// <inheritdoc/>
    void IHttpModule.Init(HttpApplication context)
    {
      context.BeginRequest += BeginRequest;
      context.EndRequest += EndRequest;
      context.Error += Error;

      EnableSessionResolver();
    }

    /// <inheritdoc/>
    void IHttpModule.Dispose()
    {
    }

    #endregion

    #region Private / internal methods

    private static void EnsureDomainIsBuilt()
    {
      if (domain == null) lock (domainBuildLock) if (domain == null) {
        var builder = DomainBuilder;
        if (builder==null)
          throw new InvalidOperationException("SessionManager.DomainBuilder is not set.");
        DisableSessionResolver();
        try {
          var newDomain = builder.Invoke();
          if (newDomain==null)
            throw new InvalidOperationException("SessionManager.DomainBuilder returned null. Domain is not available.");
          domain = newDomain;
        }
        finally {
          EnableSessionResolver();
        }
      }
    }

    private static void EnableSessionResolver()
    {
      if (Session.Resolver==null)
        Session.Resolver = () => Demand().Session;
    }

    private static void DisableSessionResolver()
    {
      Session.Resolver = null;
    }

    private void EnsureSessionIsProvided()
    {
      if (session==null) lock (provideSessionLock) if (session==null) {
        var pair = sessionProvider==null ? ProvideSession() : SessionProvider.Invoke();
        session = pair.First;
        resource = pair.Second;
      }
    }

    #endregion
  }
}