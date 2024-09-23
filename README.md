# MembershipIdentityProvider

This project is an attempt to make a bridge between the old legacy asp.net membership tables used on .NET Framework (versions 4.X and before) and the current .NET 8 using the asp.net Identity Core API.

## Instalation ##

Via nuget package console

`install-package MembershipIdentityProvider`

`install-package MembershipIdentityProvider.SqlServer`

It should work out-of-the-box by configuring the service in your Program.cs file by adding this line:

```
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddMembershipIdentitySqlServer<MembershipUser, MembershipRole>(connectionString);
```

Create an `appsettings.json` file in the root of the project and create a `DefaultConnection` under `ConnectionStrings`:

```{
  "ConnectionStrings": {
	"DefaultConnection": "YOUR SQL SERVER CONNECTION STRING"
  }
}
```

**Please note that only a few features are available at this moment, which I may or may not implement in the future.**
* Login (from `aspnet_Membership` and `aspnet_users` tables)
* Role support (from `aspnet_UserInRoles` and `aspnet_Roles` tables)
* Currently only Sql Server is supported
* Roles are treated as Claims
* Currently there's only support for hashed passwords using the `HashAlgorithmName.SHA256` algorithm.

Upon login the available user Roles will be available on `User.Claims` property on Controllers and Views (Razor).
I also have provided an extension method to simplify this retrieval:

`using MembershipIdentityProvider.Code.Extensions;`
```
public IActionResult Index()
{
    // Retrieves all the user Roles from asp.net Membership.
    var userRoles = User.GetUserRoles();
    ...
}
```
