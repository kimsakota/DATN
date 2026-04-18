using DATN.Services;

namespace DATN
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var db = new BsonData.Database("FallDetectionDB");
            db.Connect("DataStore");
            builder.Services.AddSingleton(db);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            
            // SignalR for real-time notifications
            builder.Services.AddSignalR();

            // Register Core Services
            builder.Services.AddSingleton<FallDetectionService>();
            builder.Services.AddSingleton<NotificationService>();
            
            // Add Hosted Service for MQTT Background Worker
            builder.Services.AddHostedService<MqttBackgroundService>();

            // CORS policy for mobile app + SignalR compatibility
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.SetIsOriginAllowed(_ => true)  // Allow all origins (dev)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();  // Required for SignalR WebSocket
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Map SignalR Hub endpoint
            app.MapHub<NotificationHub>("/hubs/notification");

            app.Run();
        }
    }
}