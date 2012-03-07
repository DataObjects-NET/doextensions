Xtensive.Orm.Web extension
==========================

Overview
--------
The extension adds integration for DataObjects.Net and ASP.NET. It contains SessionManager class 
which is an implementation of IHttpModule and automatically provides Session and transaction for each web request.

SessionManager has the following features:
1. When Session.Current is accessed, and there is no current Session, it will provide a new instance of Session.
   In that case a new transaction will be created. It will be committed on successful completion of http request, 
   otherwise it will be rolled back.
2. Setting SessionManager.Demand().Error to true will lead to rollback of this transaction.
3. SessionManager.Current (and SessionManager.Demand()) returns the instance of SessionManager 
   bound to the current HttpContext, i.e. current SessionManager. 
   Its Session property (if not null) is the same value as the one provided by Session.Current.

Note that presence of SessionManager does not prevent you from creating Sessions manually.
It operates relying on Session.Resolver event, which is raised only when there is no current Session.

Finally, no automatic Session + transaction will be provided, if you don't use Session.Current/Session.Demand() methods
in your code (directly or indirectly). So e.g. requests to static web pages won't lead to any DB interaction.

Prerequisites
-------------
DataObjects.Net 4.5 or later (http://dataobjects.net)

Configuration
-------------
1. Register SessionManager in HttpModulesSection in web.config file.

web.config: 

<configuration>
  <system.web>
    <httpModules>
      <add name="SessionManager" type="Xtensive.Orm.Web.SessionManager, Xtensive.Orm"/>
    </httpModules>
  </system.web>
</configuration>

2. Set SessionManager.DomainBuilder property in Application_Start method of your Global.asax.cs file.

using Xtensive.Orm.Web;

public class Global : System.Web.HttpApplication
{
  protected void Application_Start(object sender, EventArgs e)
  {
    SessionManager.DomainBuilder = DomainBuilder.Build;
  }
}

How to use
----------
  public partial class EditCustomer : System.Web.UI.Page
  {
    protected void Page_Load(object sender, EventArgs e)
    {
      // Session is provided automatically, transaction also starts
      var session = Session.Demand();
      var id = Request["customerId"];
      if (!string.IsNullOrEmpty(id)) {
        var customerId = int.Parse(id);
        var customer = session.Query.Single<Customer>(customerId);
      }
      ...
    }

    protected void Save(object sender, EventArgs e)
    {
      try {
        var session = Session.Demand();
        if (customer==null)
          customer = new Customer(session);
        customer.Name = textName.Text;
        ...
        Back();
      }
      catch(InvalidOperationException exception) {
        // This will roll back the transaction on end of http request
        SessionManager.Current.HasErrors = true;
      }
    }

More information
----------------
http://doextensions.codeplex.com