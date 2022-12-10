using LiveChatLib2.Configs;
using LiveChatLib2.Models;
using LiveChatLib2.Parsers;
using LiveChatLib2.Queue;
using LiveChatLib2.Services;
using LiveChatLib2.Storage;
using WebSocketSharp.Async;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using LiveChatLib2.Workers;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using LiveChatLib2.Utils;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Models.QueueMessages.WorkItems;
using LightInject;
using NLog;

namespace LiveChatLib2;

internal class ServicesLocator
{
    private ServiceContainer Container { get; }
    private static ILogger log = LogManager.GetCurrentClassLogger();

    private static readonly string configBasePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "configs"
    );
    private static readonly string bilibiliConfigPath = Path.Combine(
        configBasePath,
        "bilibili.config"
    );
    private static readonly string appConfigPath = Path.Combine(
        configBasePath,
        "app.config"
    );

    public ServicesLocator()
    {
        this.Container = new ServiceContainer();
        this.InitConfig();
        this.InitBasements();
        this.InitClientTable();
        this.InitStorage();
        this.InitParsers();
        this.InitWorkers();
        this.InitServices();
    }

    private void RegisterService<TInterface, TInstance>() where TInstance : TInterface
    {
        this.Container.Register<TInterface, TInstance>(new PerContainerLifetime());
    }

    private void RegisterService<TInterface>(Func<IServiceFactory, TInterface> creator)
    {
        this.Container.Register(creator, new PerContainerLifetime());
    }

    private void RegisterService(Type Interface, Func<IServiceFactory, object> creator)
    {
        this.Container.Register(Interface, creator, new PerContainerLifetime());
    }

    private void InitConfig()
    {
        this.RegisterService((factory) => LoadConfigFromYaml<LiveChatLibConfig>(appConfigPath));
        this.RegisterService((factory) => LoadConfigFromYaml<BilibiliParserConfig>(bilibiliConfigPath));
    }

    private void InitBasements()
    {
        this.RegisterService<IMessageQueue<SendWorkItem>, EasyMessageQueue<SendWorkItem>>();
        this.RegisterService<IMessageQueue<CrawlerWorkItem>, EasyMessageQueue<CrawlerWorkItem>>();
        this.RegisterService<IMessageQueue<RecordWorkItem>, EasyMessageQueue<RecordWorkItem>>();
        this.RegisterService<IMessageQueue<ClientMessage>, EasyMessageQueue<ClientMessage>>();
        this.RegisterService(this.BuildWebSocketServer);
    }

    private void InitStorage()
    {
        this.RegisterService<IBilibiliUserInfoStorage, BilibiliUserInfoStorage>();
        this.RegisterService<IBilibiliChatStorage, BilibiliChatStorage>();
    }

    private void InitParsers()
    {
        this.RegisterService<IBilibiliParser, BilibiliParser>();
    }

    private void InitWorkers()
    {
        this.RegisterService<IWorker<ClientMessage>, ClientMessageProcessWorker>();
        this.RegisterService<IWorker<RecordWorkItem>, RecordWorker>();
        this.RegisterService<IWorker<CrawlerWorkItem>, CrawlerWorker>();
        this.RegisterService<IWorker<SendWorkItem>, SendWorker>();
    }

    private void InitServices()
    {
        this.RegisterService<DistributeService, DistributeService>();
    }

    private static T LoadConfigFromYaml<T>(string path) where T : class, new()
    {
        var fi = new FileInfo(path);
        if (!fi.Exists)
        {
            var config = new T();
            return config;
        }
        else
        {
            var text = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder().Build();
            var config = deserializer.Deserialize<T>(text);
            return config;
        }
    }

    private WebSocketServer BuildWebSocketServer(IServiceFactory factory)
    {
        var config = factory.GetInstance<LiveChatLibConfig>();
        if (config is null)
        {
            throw new InvalidOperationException("LiveChatLibConfig should be initialized before WebSocketServer.");
        }

        var server = new WebSocketServer(config.DistributorPort ?? 6099);
        return server;
    }

    private void InitClientTable()
    {
        this.RegisterService((factory) => new ClientTable());
    }

    public T? Resolve<T>() where T : class
    {
        return (T)this.Resolve(typeof(T))!;
    }

    public object? Resolve(Type Interface) {
        try
        {
            var instance = this.Container.TryGetInstance(Interface);
            return instance;
        }
        catch (Exception ex)
        {
            log.Error($"Failed to resolve {Interface.Name} service, would return null: {ex.GetType().Name}|{ex.Message}");
            return null;
        }
    }
}

