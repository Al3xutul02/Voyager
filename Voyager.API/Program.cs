using BusinessLogic.Mapper;
using BusinessLogic.Services;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Repository.Context;
using Repository.Repositories;
using Repository.Repositories.Abstractions;
using System.Reflection;

namespace Voyager.API;

public class Program
{
    public static readonly JsonSerializerSettings DefaultJsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Include,
        DefaultValueHandling = DefaultValueHandling.Populate,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        ReferenceLoopHandling = ReferenceLoopHandling.Error,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public static async Task Main(string[] args)
    {
        // Configure Newtonsoft library settings
        JsonConvert.DefaultSettings = () =>
        {
            return new JsonSerializerSettings(DefaultJsonSettings);
        };

        var builder = WebApplication.CreateBuilder(args);
        
        // Add the discord client as a global service
        builder.Services.AddSingleton(provider =>
        {
            var token = builder.Configuration.GetValue<string>("Discord:Token");

            var discordClient = new DiscordClient(new DiscordConfiguration
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

            discordClient.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            // Setup event subscriptions
            discordClient.Ready += Events.EventHandler.OnReady;
            discordClient.GuildCreated += Events.EventHandler.GuildCreated;
            discordClient.GuildDeleted += Events.EventHandler.GuildDeleted;
            discordClient.GuildMemberAdded += Events.EventHandler.GuildMemberAdded;
            discordClient.GuildMemberRemoved += Events.EventHandler.GuildMemberRemoved;
            discordClient.ComponentInteractionCreated += Events.EventHandler.ComponentInteractionCreated;

            var slashCommandsConfig = discordClient.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = provider
            });
            slashCommandsConfig.RegisterCommands(Assembly.GetExecutingAssembly());

            slashCommandsConfig.SlashCommandErrored += (s, e) =>
            {
                Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Slash command '{e.Context.CommandName}' errored: {e.Exception}");
                return Task.CompletedTask;
            };

            discordClient.ClientErrored += (s, e) =>
            {
                Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Discord client error in '{e.EventName}': {e.Exception}");
                return Task.CompletedTask;
            };

            discordClient.Ready += (s, e) => {
                Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Discord client is ready! Bot is connected.");
                return Task.CompletedTask;
            };

            return discordClient;
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

        builder.Services.AddDbContext<VoyagerDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("VoyagerDbDevConnection"),
                             ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("VoyagerDbDevConnection"))));
        builder.Services.AddScoped<DbContext, VoyagerDbContext>();

        // Business logic services
        builder.Services.AddScoped<IMediaSerivce, MediaService>();
        builder.Services.AddScoped<IUserService, UserService>();

        // Repositories
        builder.Services.AddScoped<IUserRepository, UserRepository>();

        var app = builder.Build();

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

            // Verify database connection
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<VoyagerDbContext>();
                try
                {
                    dbContext.Database.OpenConnection();
                    dbContext.Database.CloseConnection();
                    Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Database connection successful.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Database connection failed: {ex.Message}");
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
