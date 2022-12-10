using LiveChatLib2.Models;
using LiveChatLib2.Models.MessageRecords;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Models.QueueMessages.WorkItems;
using LiveChatLib2.Models.RemotePackages;
using LiveChatLib2.Storage;
using NLog;

namespace LiveChatLib2.Workers;

internal class RecordWorker : IWorker<RecordWorkItem>
{
    public RecordWorker(
        IBilibiliUserInfoStorage bilibiliUserInfoStorage,
        IBilibiliChatStorage bilibiliChatStorage
    )
    {
        this.BilibiliUserInfoStorage = bilibiliUserInfoStorage;
        this.BilibiliChatStorage = bilibiliChatStorage;
    }

    private IBilibiliUserInfoStorage BilibiliUserInfoStorage { get; }
    private IBilibiliChatStorage BilibiliChatStorage { get; }

    private readonly ILogger log = LogManager.GetCurrentClassLogger();

    public async Task DoWork(RecordWorkItem parameters, CancellationToken cancellationToken)
    {
        switch (parameters.Type)
        {
            case RecordType.User:
                await (parameters.Source switch
                {
                    WorkItemSources.BILIBILI => this.RecordBilibiliUser(parameters.Record as BilibiliUserInfo, cancellationToken),
                    _ => throw new InvalidDataException($"Unknown source: {parameters.Source}.")
                });
                break;

            case RecordType.Chat:
                await (parameters.Source switch
                {
                    WorkItemSources.BILIBILI => this.RecordBilibiliChat(parameters.Record as BilibiliMessageRecord, cancellationToken),
                    _ => throw new InvalidDataException($"Unknown source: {parameters.Source}.")
                });
                break;

            case RecordType.Package:
                await (parameters.Source switch
                {
                    WorkItemSources.BILIBILI => this.RecordBilibiliPackage(parameters.Record as BilibiliRemotePackage, cancellationToken),
                    _ => throw new InvalidDataException($"Unknown source: {parameters.Source}.")
                });
                break;

            default:
                log.Error($"Unknown record type: {parameters.Type}.");
                break;
        }
    }

    public async Task RecordBilibiliUser(BilibiliUserInfo? data, CancellationToken cancellationToken)
    {
        if (data == null)
        {
            log.Error("RecordBilibiliUser get null record.");
            return;
        }

        await Task.Run(() =>
        {
            this.BilibiliUserInfoStorage.RecordUserInformation(data);
        }, cancellationToken);
    }

    public async Task RecordBilibiliChat(BilibiliMessageRecord? data, CancellationToken cancellationToken)
    {
        if (data == null)
        {
            log.Error("RecordBilibiliChat get null record.");
            return;
        }

        await this.BilibiliChatStorage.RecordChatMessage(data, cancellationToken);
    }

    public async Task RecordBilibiliPackage(BilibiliRemotePackage? data, CancellationToken cancellationToken)
    {
        if (data == null)
        {
            log.Error("RecordBilibiliPackage get null record.");
            return;
        }

        await this.BilibiliChatStorage.RecordRemotePackage(data, cancellationToken);
    }
}
