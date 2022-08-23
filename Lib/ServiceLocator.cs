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

namespace LiveChatLib2;

internal class ServicesLocator
{
    private Container Container { get; }
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
        this.Container = new Container();
        this.InitConfig();
        this.InitBasements();
        this.InitClientTable();
        this.InitStorage();
        this.InitParsers();
        this.InitWorkers();
        this.InitServices();
    }

    private void InitConfig()
    {
        var liveChatConfig = File.ReadAllText(appConfigPath);
        var bilibiliConfig = File.ReadAllText(bilibiliConfigPath);
        this.Container
            .Register(() => LoadConfigFromYaml<LiveChatLibConfig>(liveChatConfig))
            .AsSingleton();
        this.Container
            .Register(() => LoadConfigFromYaml<BilibiliParserConfig>(bilibiliConfig))
            .AsSingleton();
    }

    private void InitBasements()
    {
        this.Container
            .Register<IMessageQueue<SendWorker>>(typeof(EasyMessageQueue<SendWorker>))
            .AsSingleton();
        this.Container
            .Register<IMessageQueue<CrawlerWorker>>(typeof(EasyMessageQueue<CrawlerWorker>))
            .AsSingleton();
        this.Container
            .Register<IMessageQueue<RecordWorker>>(typeof(EasyMessageQueue<RecordWorker>))
            .AsSingleton();
        this.Container
            .Register<IMessageQueue<ClientMessage>>(typeof(EasyMessageQueue<ClientMessage>))
            .AsSingleton();
        this.Container
            .Register(this.BuildWebSocketServer)
            .AsSingleton();
    }

    private void InitStorage()
    {
        this.Container
            .Register<IBilibiliChatStorage>(typeof(BilibiliChatStorage))
            .AsSingleton();
        this.Container
            .Register<IBilibiliUserInfoStorage>(typeof(BilibiliUserInfoStorage))
            .AsSingleton();
    }

    private void InitParsers()
    {
        this.Container
            .Register<IBilibiliParser>(typeof(BilibiliParser))
            .AsSingleton();
    }

    private void InitWorkers()
    {
        this.Container
            .Register<IWorker<ClientMessage>>(typeof(ClientMessageProcessWorker))
            .AsSingleton();
        this.Container
            .Register<IWorker<RecordWorkItem>>(typeof(RecordWorker))
            .AsSingleton();
        this.Container
            .Register<IWorker<CrawlerWorkItem>>(typeof(CrawlerWorker))
            .AsSingleton();
        this.Container
            .Register<IWorker<SendWorkItem>>(typeof(SendWorker))
            .AsSingleton();
    }

    private void InitServices()
    {
        this.Container
            .Register<DistributeService>(typeof(DistributeService))
            .AsSingleton();
    }

    private static T LoadConfigFromYaml<T>(string text)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var config = deserializer.Deserialize<T>(text);
        return config;
    }

    private WebSocketServer BuildWebSocketServer()
    {
        var config = this.Container.Resolve<LiveChatLibConfig>();
        var server = new WebSocketServer(config.DistributorPort ?? 6099);
        return server;
    }

    private void InitClientTable()
    {
        this.Container
            .Register(() => new ClientTable())
            .AsSingleton();
    }

    public T Resolve<T>() => this.Container.Resolve<T>();
}

