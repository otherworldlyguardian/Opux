﻿using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using EveLibCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Opux
{
    public class Program
    {
        public static DiscordSocketClient Client { get; private set; }
        public static CommandService Commands { get; private set; }
        public static DependencyMap Map { get; private set; }
        public static EveLib EveLib { get; private set; }
        public static string ApplicationBase { get; private set; }
        public static IConfigurationRoot Settings { get; private set; }

        static AutoResetEvent autoEvent = new AutoResetEvent(true);

        static Timer stateTimer = new Timer(Functions.RunTick, autoEvent, 1 * 100, 1 * 100);

        public static void Main(string[] args)
        {
            try
            {
                ApplicationBase = Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);
                if (!File.Exists(ApplicationBase + "/Opux.db"))
                    File.Copy(ApplicationBase + "/Opux.def.db", ApplicationBase + "/Opux.db");
                Client = new DiscordSocketClient();
                Commands = new CommandService();
                Map = new DependencyMap();
                EveLib = new EveLib();
                UpdateSettings();
                MainAsync(args).GetAwaiter().GetResult();

                Console.ReadKey();
                Client.StopAsync();
            }
            catch (Exception ex)
            {
                Functions.Client_Log(new LogMessage(LogSeverity.Error, "Main", ex.Message, ex));
            }
        }

        internal static async Task MainAsync(string[] args)
        {
            Client.Log += Functions.Client_Log;
            Client.LoggedOut += Functions.Event_LoggedOut;
            Client.LoggedIn += Functions.Event_LoggedIn;
            Client.Connected += Functions.Event_Connected;
            Client.Disconnected += Functions.Event_Disconnected;
            Client.GuildAvailable += Functions.Event_GuildAvaliable;
            Client.UserJoined += Functions.Event_UserJoined;

            try
            {
                await Functions.InstallCommands();
                await Client.LoginAsync(TokenType.Bot, Settings.GetSection("config")["token"]);
                await Client.StartAsync();
            }
            catch (HttpException ex)
            {
                if (ex.Reason.Contains("401"))
                {
                    await Functions.Client_Log(new LogMessage(LogSeverity.Error, "Discord.NET", $"Check your Token: {ex.Reason}"));
                }
            }
            catch (Exception ex)
            {
                await Functions.Client_Log(new LogMessage(LogSeverity.Error, "Main", ex.Message, ex));
            }
        }

        public static Task UpdateSettings()
        {
            Settings = new ConfigurationBuilder()
            .SetBasePath(Program.ApplicationBase)
            .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();
            Functions.nextNotificationCheck = DateTime.Parse(Functions.SQLiteDataQuery("cacheData", "data", "nextNotificationCheck").GetAwaiter().GetResult());
            return Task.CompletedTask;
        }
    }
}
