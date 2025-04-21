#Simple Configuration

Include the following code in the `Program.cs` file somewhere before the builder.build() call.

Create an `appsettings.json` file in the root of the project and create a `DefaultConnection` under `ConnectionStrings` and a MembershipSettings entry:

`{
  "ConnectionStrings": {
	"DefaultConnection": "YOUR SQL SERVER CONNECTION STRING"
  },
  "MembershipSettings": {
    "PasswordFormat": 1,
    "ApplicationId": "F667FF98-9169-4A1E-9A69-1D1E749E0BEA"
  },
}`

In the services add the following line:
`builder.Services.AddOptions<MembershipSettings>().BindConfiguration(nameof(MembershipSettings));`

Then add the following code to the Program.cs file:

`var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var membershipSettings = new MembershipSettings();
builder.Configuration.GetSection("MembershipSettings").Bind(membershipSettings);
builder.Services.AddMembershipIdentitySqlServer<MembershipUser, MembershipRole>(connectionString, membershipSettings);
`
