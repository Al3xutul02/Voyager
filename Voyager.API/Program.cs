using BusinessLogic.Mapper;
using BusinessLogic.Services;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Repository.Context;
using Repository.Repositories;
using Repository.Repositories.Abstractions;
using System.Reflection;

namespace Voyager.API;

/// <summary>
/// Application entry point and composition root. Wires up DI, configures
/// the Discord client and slash commands, and starts the ASP.NET Core host.
/// </summary>
public class Program
{
    /// <summary>
    /// Convenience singleton handle to the bot's <see cref="DiscordClient"/>
    /// so static factories (e.g. <c>Buttons.ClearMessage</c> needing
    /// <c>DiscordEmoji.FromName</c>) don't have to take it as a parameter.
    /// Assigned inside the AddSingleton factory below; reads always happen
    /// after the singleton has been resolved at startup, so the
    /// null-forgiving initializer is safe in practice.
    /// </summary>
    public static DiscordClient DiscordClient { get; set; } = null!;

    /// <summary>
    /// Process entry point. Configures the WebApplication builder, registers
    /// every service (Discord client, AutoMapper, EF Core, business
    /// services), wires up event handlers and slash commands, then runs the
    /// host until shutdown.
    /// </summary>
    public static async Task Main(string[] args)
    {
        // NOTE: do not set JsonConvert.DefaultSettings here. DSharpPlus
        // uses Newtonsoft internally and has its own [JsonProperty] attributes
        // on its DTOs. A global ContractResolver (e.g. CamelCasePropertyNames)
        // will collide with those attributes — e.g. it throws
        // "A member with the name 'components' already exists on
        // DiscordActionRowComponent" the first time a component is serialized.
        // If you need custom JSON settings for your own types, pass them
        // explicitly to JsonConvert.Serialize/DeserializeObject.

        var builder = WebApplication.CreateBuilder(args);
        
        // Add the discord client as a global service
        builder.Services.AddSingleton(provider =>
        {
            var token = builder.Configuration.GetValue<string>("Discord:Token");

            DiscordClient = new DiscordClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                // If we later add features that need to read
                // messages or reactions, OR in the relevant flags
                // e.g. | DiscordIntents.GuildMessages | DiscordIntents.MessageContent
                // — note MessageContent is a privileged intent that must also
                // be enabled in the Discord developer portal
                Intents = DiscordIntents.Guilds,
                AutoReconnect = true
            });

            DiscordClient.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            // Setup event subscriptions
            DiscordClient.Ready += Events.EventHandler.OnReady;
            DiscordClient.GuildCreated += Events.EventHandler.GuildCreated;
            DiscordClient.GuildDeleted += Events.EventHandler.GuildDeleted;
            DiscordClient.GuildMemberAdded += Events.EventHandler.GuildMemberAdded;
            DiscordClient.GuildMemberRemoved += Events.EventHandler.GuildMemberRemoved;
            DiscordClient.ComponentInteractionCreated += Events.EventHandler.ComponentInteractionCreated;

            var slashCommandsConfig = DiscordClient.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = provider
            });
            slashCommandsConfig.RegisterCommands(Assembly.GetExecutingAssembly());

            slashCommandsConfig.SlashCommandErrored += (s, e) =>
            {
                Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Slash command '{e.Context.CommandName}' errored: {e.Exception}");
                return Task.CompletedTask;
            };

            DiscordClient.ClientErrored += (s, e) =>
            {
                Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Discord client error in '{e.EventName}': {e.Exception}");
                return Task.CompletedTask;
            };

            DiscordClient.Ready += (s, e) => {
                Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Discord client is ready! Bot is connected.");
                return Task.CompletedTask;
            };

            return DiscordClient;
        });

        // Add services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddAutoMapper(cfg =>
        {
            var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];
            if (!string.IsNullOrWhiteSpace(licenseKey))
            {
                cfg.LicenseKey = licenseKey;
            }

            cfg.AddMaps(typeof(MappingProfile).Assembly);
        });

        // Pool VoyagerDbContext instances rather than allocating one per
        // request. Same scoped semantics from the caller's perspective;
        // EF Core resets the change tracker on return-to-pool. Default
        // pool size is 1024, plenty for a Discord bot.
        builder.Services.AddDbContextPool<VoyagerDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("VoyagerDbDevConnection"),
                             ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("VoyagerDbDevConnection"))));

        // Redirect DbContext (the base type used by BaseRepository) to the
        // SAME pooled VoyagerDbContext instance within the scope. Without
        // this, resolving DbContext would create a fresh non-pooled
        // VoyagerDbContext and bypass the pool entirely.
        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<VoyagerDbContext>());

        // Business logic services
        builder.Services.AddScoped<IEnumService, EnumService>();
        builder.Services.AddScoped<IUserService, UserService>();

        // Repositories
        builder.Services.AddScoped<IUserRepository, UserRepository>();

        var app = builder.Build();

        // Give the static EventHandler access to the DI container so it can
        // open scopes when component interactions fire.
        Events.EventHandler.Initialize(app.Services.GetRequiredService<IServiceScopeFactory>());

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Lifetime.ApplicationStarted.Register(async () => {

            // Apply any pending EF Core migrations on startup. This also acts
            // as the database connectivity check (Migrate() opens a connection)
            // and brings the schema up to date with the model automatically.
            // NOTE: this is convenient for a single-instance bot. For a serious
            // multi-instance / production deployment we'd apply migrations as a
            // separate deploy step instead, to avoid two instances migrating at
            // once.
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<VoyagerDbContext>();
                try
                {
                    dbContext.Database.Migrate();
                    Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Database migrated and connection successful.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Database migration failed: {ex.Message}");
                }
            }

            // Verify discord client connection
            try
            {
                var discordClient = app.Services.GetRequiredService<DiscordClient>();
                await discordClient.ConnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Error connecting Discord client: {ex.Message}");
            }
        });

        await app.RunAsync();
    }
}
