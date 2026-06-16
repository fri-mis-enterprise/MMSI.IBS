using Google.Cloud.Storage.V1;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Services;
using IBS.Services.AccessControl;
using IBS.Utility;
using IBSWeb.Hubs;
using IBSWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// QuestPDF
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// DBContext (scoped)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddDefaultIdentity<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

// Razor
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new DecimalJsonConverter());
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    })
    .AddSessionStateTempDataProvider();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Repositories + DI
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.Configure<GCSConfigOptions>(builder.Configuration);
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddScoped<IUserAccessService, UserAccessService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAccessControlService, AccessControlService>();
builder.Services.AddScoped<IHubConnectionRepository, HubConnectionRepository>();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ICloudStorageService, LocalFileStorageService>();
}
else
{
    builder.Services.AddSingleton<ICloudStorageService, CloudStorageService>();
}
builder.Services.AddScoped<ITugboatMonitoringService, TugboatMonitoringService>();
builder.Services.AddScoped<IVesselPlanningService, VesselPlanningService>();
builder.Services.AddScoped<IJobOrderService, JobOrderService>();
builder.Services.AddScoped<IDispatchTicketService, DispatchTicketService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<IMaritimeServiceService, MaritimeServiceService>();
builder.Services.AddScoped<IPortService, PortService>();
builder.Services.AddScoped<IPrincipalService, PrincipalService>();
builder.Services.AddScoped<ITariffRateService, TariffRateService>();
builder.Services.AddScoped<ITerminalService, TerminalService>();
builder.Services.AddScoped<ITugMasterService, TugMasterService>();
builder.Services.AddScoped<ITugboatOwnerService, TugboatOwnerService>();
builder.Services.AddScoped<ITugboatService, TugboatService>();
builder.Services.AddScoped<IVesselService, VesselService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IChartOfAccountService, ChartOfAccountService>();
builder.Services.AddScoped<IAuditTrailService, AuditTrailService>();

// SignalR
builder.Services.AddSignalR();

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024 * 1024 * 100; // 100MB cap
});

if (builder.Environment.IsProduction())
{
    var bucketName = builder.Configuration["GoogleCloudStorageBucketName"]!;
    var storageClient = StorageClient.Create();

    builder.Services.AddDataProtection()
        .SetApplicationName("IBS-Web")
        .AddKeyManagementOptions(options =>
        {
            options.XmlRepository = new GcsXmlRepository(
                storageClient,
                bucketName,
                "dataprotection-keys.xml"
            );
        });
}

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("Healthy"));

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/User/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    var localStoragePath = app.Configuration["LocalStoragePath"] ?? "App_Data/LocalStorage";
    var absolutePath = Path.IsPathRooted(localStoragePath)
        ? localStoragePath
        : Path.Combine(app.Environment.ContentRootPath, localStoragePath);

    if (!Directory.Exists(absolutePath))
    {
        Directory.CreateDirectory(absolutePath);
    }

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(absolutePath),
        RequestPath = "/local-storage"
    });
}

app.UseMiddleware<MaintenanceMiddleware>();

app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=User}/{controller=Home}/{action=Index}/{id?}");

// SignalR
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<TugboatHub>("/tugboatHub");
app.MapHub<PlanningHub>("/planningHub");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync(); // creates all tables automatically
    await DbSeeder.SeedAsync(services);
}

app.Run();

public partial class Program { }

// Custom JSON converter for decimal formatting
public class DecimalJsonConverter : System.Text.Json.Serialization.JsonConverter<decimal>
{
    public override decimal Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        return reader.GetDecimal();
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, decimal value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteNumberValue(Math.Round(value, 4, MidpointRounding.AwayFromZero));
    }
}
