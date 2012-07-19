using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Linq;
using System.Web.Security;
using Xtensive.Orm.Web;

namespace Xtensive.Orm.Security.Web
{
  /// <summary>
  /// Implementation of <see cref="RoleProvider"/>.
  /// </summary>
  public class OrmRoleProvider : RoleProvider
  {
    private Type rootPrincipalType;
    private Type rootRoleType;
    private Configuration configuration;

    private Domain Domain
    {
      get { return SessionManager.Domain; }
    }

    private IQueryable<MembershipPrincipal> GetPrincipalQueryRoot(Session session)
    {
      return session.Query.All(rootPrincipalType) as IQueryable<MembershipPrincipal>;
    }

    private IQueryable<Role> GetRoleQueryRoot(Session session)
    {
      return session.Query.All(rootRoleType) as IQueryable<Role>;
    }

    /// <summary>
    /// Gets or sets the name of the application to store and retrieve role information for.
    /// </summary>
    /// <returns>The name of the application to store and retrieve role information for.</returns>
    public override string ApplicationName
    {
      get { return configuration.ApplicationName; }
      set { configuration.ApplicationName = value; }
    }

    /// <summary>
    /// Initializes the specified name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="settings">The settings.</param>
    public override void Initialize(string name, NameValueCollection settings)
    {
      if (String.IsNullOrEmpty(name))
        name = Wellknown.MembershipProviderName;

      base.Initialize(name, settings);
      configuration = new Configuration(settings);

      rootPrincipalType = Domain.Model.Hierarchies
        .Select(h => h.Root.UnderlyingType)
        .FirstOrDefault(t => typeof (MembershipPrincipal).IsAssignableFrom(t));

      if (rootPrincipalType == null)
        throw new InvalidOperationException("No descendants of MembershipPrincipal type are found in domain model");

      rootRoleType = Domain.Model.Hierarchies
        .Select(h => h.Root.UnderlyingType)
        .FirstOrDefault(t => typeof (Role).IsAssignableFrom(t));

      if (rootRoleType == null)
        throw new InvalidOperationException("No descendants of Role type are found in domain model");
    }

    /// <summary>
    /// Gets a value indicating whether the specified user is in the specified role for the configured applicationName.
    /// </summary>
    /// <param name="username">The user name to search for.</param>
    /// <param name="roleName">The role to search in.</param>
    /// <returns>
    /// true if the specified user is in the specified role for the configured applicationName; otherwise, false.
    /// </returns>
    public override bool IsUserInRole(string username, string roleName)
    {
      Validation.CheckParameter(ref username, "username");
      Validation.CheckParameter(ref roleName, "roleName");

      using (var session = Domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          var principal = GetUserByName(session, username);
          if (principal == null)
            return false;

          return principal.Roles
            .Any(p => p.Name == roleName);
        }
      }
    }

    /// <summary>
    /// Gets a list of the roles that a specified user is in for the configured applicationName.
    /// </summary>
    /// <param name="username">The user to return a list of roles for.</param>
    /// <returns>
    /// A string array containing the names of all the roles that the specified user is in for the configured applicationName.
    /// </returns>
    public override string[] GetRolesForUser(string username)
    {
      Validation.CheckParameter(ref username, "username");

      using (var session = Domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          var principal = GetUserByName(session, username);
          if (principal == null)
            return new string[]{};

          return principal.Roles
            .Select(p => p.Name)
            .ToArray();
        }
      }
    }

    /// <summary>
    /// Adds a new role to the data source for the configured applicationName.
    /// </summary>
    /// <param name="roleName">The name of the role to create.</param>
    public override void CreateRole(string roleName)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    /// Removes a role from the data source for the configured applicationName.
    /// </summary>
    /// <param name="roleName">The name of the role to delete.</param>
    /// <param name="throwOnPopulatedRole">If true, throw an exception if <paramref name="roleName"/> has one or more members and do not delete <paramref name="roleName"/>.</param>
    /// <returns>
    /// true if the role was successfully deleted; otherwise, false.
    /// </returns>
    public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    /// Gets a value indicating whether the specified role name already exists in the role data source for the configured applicationName.
    /// </summary>
    /// <param name="roleName">The name of the role to search for in the data source.</param>
    /// <returns>
    /// true if the role name already exists in the data source for the configured applicationName; otherwise, false.
    /// </returns>
    public override bool RoleExists(string roleName)
    {
      Validation.CheckParameter(ref roleName, "roleName");

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        var role = GetRoleByName(session, roleName);
        return role!=null;
      }
    }

    /// <summary>
    /// Adds the specified user names to the specified roles for the configured applicationName.
    /// </summary>
    /// <param name="usernames">A string array of user names to be added to the specified roles.</param>
    /// <param name="roleNames">A string array of the role names to add the specified user names to.</param>
    public override void AddUsersToRoles(string[] usernames, string[] roleNames)
    {
      Validation.CheckParameter(usernames, "usernames");
      Validation.CheckParameter(roleNames, "roleNames");

      for (int i = 0; i < usernames.Length; i++)
        Validation.CheckParameter(ref usernames[i], "username");
      for (int i = 0; i < roleNames.Length; i++)
        Validation.CheckParameter(ref roleNames[i], "roleName");

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        var users = GetPrincipalQueryRoot(session)
          .Where(u => u.Name.In(usernames))
          .Prefetch(u => u.Roles)
          .ToList();
        var roles = GetRoleQueryRoot(session)
          .Where(r => r.Name.In(roleNames))
          .ToList();

        foreach (var user in users)
          user.Roles.AddRange(roles);

        t.Complete();
      }
    }

    /// <summary>
    /// Removes the specified user names from the specified roles for the configured applicationName.
    /// </summary>
    /// <param name="usernames">A string array of user names to be removed from the specified roles.</param>
    /// <param name="roleNames">A string array of role names to remove the specified user names from.</param>
    public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
    {
      Validation.CheckParameter(usernames, "usernames");
      Validation.CheckParameter(roleNames, "roleNames");

      for (int i = 0; i < usernames.Length; i++)
        Validation.CheckParameter(ref usernames[i], "username");
      for (int i = 0; i < roleNames.Length; i++)
        Validation.CheckParameter(ref roleNames[i], "roleName");

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        var users = GetPrincipalQueryRoot(session)
          .Where(u => u.Name.In(usernames))
          .Prefetch(u => u.Roles)
          .ToList();
        var roles = GetRoleQueryRoot(session)
          .Where(r => r.Name.In(roleNames))
          .ToList();

        foreach (var user in users)
          user.Roles.ExceptWith(roles);

        t.Complete();
      }
    }

    /// <summary>
    /// Gets a list of users in the specified role for the configured applicationName.
    /// </summary>
    /// <param name="roleName">The name of the role to get the list of users for.</param>
    /// <returns>
    /// A string array containing the names of all the users who are members of the specified role for the configured applicationName.
    /// </returns>
    public override string[] GetUsersInRole(string roleName)
    {
      Validation.CheckParameter(ref roleName, "roleName");

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        var role = GetRoleByName(session, roleName);
        if (role==null)
          throw new ProviderException(string.Format("Role {0} is not found", roleName));

        return role.Principals
          .Select(p => p.Name)
          .ToArray();
      }
    }

    /// <summary>
    /// Gets a list of all the roles for the configured applicationName.
    /// </summary>
    /// <returns>
    /// A string array containing the names of all the roles stored in the data source for the configured applicationName.
    /// </returns>
    public override string[] GetAllRoles()
    {
      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        return GetRoleQueryRoot(session)
          .Select(r => r.Name)
          .ToArray();
      }
    }

    /// <summary>
    /// Gets an array of user names in a role where the user name contains the specified user name to match.
    /// </summary>
    /// <param name="roleName">The role to search in.</param>
    /// <param name="usernameToMatch">The user name to search for.</param>
    /// <returns>
    /// A string array containing the names of all the users where the user name matches <paramref name="usernameToMatch"/> and the user is a member of the specified role.
    /// </returns>
    public override string[] FindUsersInRole(string roleName, string usernameToMatch)
    {
      Validation.CheckParameter(ref roleName, "roleName");
      Validation.CheckParameter(ref usernameToMatch, "usernameToMatch");

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        var role = GetRoleByName(session, roleName);
        if (role==null)
          throw new ProviderException(string.Format("Role {0} is not found", roleName));

        return role.Principals
          .Where(p => p.Name.Contains(usernameToMatch))
          .OrderBy(p => p.Name)
          .Select(p => p.Name)
          .ToArray();
      }
    }

    private Role GetRoleByName(Session session, string roleName)
    {
      return GetRoleQueryRoot(session)
        .SingleOrDefault(r => r.Name==roleName);
    }

    private MembershipPrincipal GetUserByName(Session session, string username)
    {
      return GetPrincipalQueryRoot(session)
        .SingleOrDefault(s => s.Name==username);
    }
  }
}
