#Simple Configuration

Include the following code in the `Program.cs` file somewhere before the builder.build() call.

Create an `appsettings.json` file in the root of the project and create a `DefaultConnection` under `ConnectionStrings`:

`{
  "ConnectionStrings": {
	"DefaultConnection": "YOUR SQL SERVER CONNECTION STRING"
  }
}`

`var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddMembershipIdentitySqlServer<MembershipUser, MembershipRole>(connectionString);`

