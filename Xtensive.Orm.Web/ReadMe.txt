How to configure:
-----------------

To initialize this class, add it to HttpModulesSection in web.config file and set SessionManaget.DomainBuilder property in Application_Start method of your Global.asax.cs file. 

Example:
--------

web.config: 

<configuration>
  <system.web>
    <httpModules>
      <add name="SessionManager" type="Xtensive.Orm.Web.SessionManager, Xtensive.Orm"/>
    </httpModules>
  </system.web>
</configuration>


Global.asax.cs: 

public class Global : System.Web.HttpApplication
{
  protected void Application_Start(object sender, EventArgs e)
  {
    SessionManager.DomainBuilder = DomainBuilder.Build;
  }
}
