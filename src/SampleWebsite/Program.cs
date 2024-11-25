using MembershipIdentityProvider.Code;
using MembershipIdentityProvider.Code.Identity;
using MembershipIdentityProvider.SqlServer.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Add services to the container.
builder.Services.AddOptions<MembershipSettings>().BindConfiguration(nameof(MembershipSettings));

// Membership Identity setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var membershipSettings = new MembershipSettings();
builder.Configuration.GetSection("MembershipSettings").Bind(membershipSettings);

builder.Services.AddMembershipIdentitySqlServer<MembershipUser, MembershipRole>(connectionString, membershipSettings);
/**************************************************************/

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();

await app.RunAsync();
