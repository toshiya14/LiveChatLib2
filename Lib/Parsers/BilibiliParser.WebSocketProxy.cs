﻿using LiveChatLib2.Exceptions;
using LiveChatLib2.Utils;
using WebSocketSharp.Async;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using LiveChatLib2.Services;
using LiveChatLib2.Models;
using LiveChatLib2.Models.QueueMessages.WorkItems;
using Newtonsoft.Json;

namespace LiveChatLib2.Parsers;

internal partial class BilibiliParser
{
    private const string ROOM_INIT_URL = @"https://api.live.bilibili.com/room/v1/Room/room_init";
    private const string GET_CONF_URL = @"https://api.live.bilibili.com/room/v1/Danmu/getConf";
    private const string WEBSOCKET_URL = @"wss://broadcastlv.chat.bilibili.com/sub";
    private const string SOURCE = "bilibili";

    private WebSocketClient Proxy { get; set; }

    private string? realRoomId;
    private string? roomToken;
    private bool waitBack = false;
    private CancellationToken? proxyCancellationToken;

    protected async Task StartProxy(CancellationToken ct)
    {
        this.proxyCancellationToken = ct;

        await this.Prepare();
        this.State = ParserListeningStatus.Connecting;

        // Event registration.
        this.Proxy.OnMessage += this.OnMessage;
        this.Proxy.OnError += this.OnError;
        this.Proxy.OnClose += this.OnClose;
        this.Proxy.OnOpen += this.OnOpen;

        await this.Proxy.ConnectAsync(ct);

        // Start Checking.
        _ = this.CheckProxyState();
    }

    private async Task CheckProxyState()
    {
        if (proxyCancellationToken == null)
        {
            throw new InvalidOperationException("Proxy are not initialized.");
        }

        var ct = proxyCancellationToken.Value;

        while (!ct.IsCancellationRequested)
        {
            if (this.State == ParserListeningStatus.Connected)
            {
                var now = DateTime.UtcNow;
                var heartBeatDuration = (now - lastSendHeartBeatTime).TotalMilliseconds;

                // send heartbeat interval.
                if (heartBeatDuration > this.Config.HeartBeatDuration)
                {
                    await SendHeartBeat(this.Proxy, ct);
                    lastSendHeartBeatTime = now;
                    waitBack = true;
                    log.Trace("Heartbeat sent.");
                    break;
                }

                // checking.
                if (waitBack && heartBeatDuration > this.Config.LostTimeoutThreshold)
                {
                    this.State = ParserListeningStatus.BadCommunication;
                    log.Trace("Bad communication detected.");
                    _ = this.Proxy.ReconnectAsync(ct);
                }
            }

            await Task.Delay(1000, ct);
        }
    }

    private void Login()
    {
        log.Trace($"Try to login to the room.");
        if (proxyCancellationToken == null)
        {
            throw new InvalidOperationException("Proxy are not initialized.");
        }

        var ct = proxyCancellationToken.Value;

        if (realRoomId == null || roomToken == null || this.Proxy == null)
        {
            throw new DataFormatException("BilibiliParser.WebSocketProxy not initialized.");
        }

        var package = MakeAuthPackage("0", realRoomId, roomToken);
        log.Debug($"Auth Package: {JsonConvert.SerializeObject(package)}");
        log.Info($"Connecting WebSocket with ROOMID: {realRoomId} ...");
        var bytes = package.ToByteArray();
        //Task.Delay(1000).Wait();
        this.Proxy.SendAsync(bytes, ct).Wait();
        log.Trace($"Login successfully.");
        this.State = ParserListeningStatus.Connected;
    }

    private async Task Prepare()
    {
        log.Trace("Preparing BilibiliParser.");
        if (proxyCancellationToken == null)
        {
            throw new InvalidOperationException("Proxy are not initialized.");
        }

        var ct = proxyCancellationToken.Value;

        if (this.Config.ProcessingRoomId == null)
        {
            throw new ArgumentException($"Config.ProcessingRoomId not specified.");
        }

        var realRoomId = await GetRealRoomId(this.Config.ProcessingRoomId, ct);
        var roomToken = await GetRoomToken(this.Config.ProcessingRoomId, ct);

        if (realRoomId == null)
        {
            throw new DataFormatException($"Failed to fetch real room id.");
        }

        if (roomToken == null)
        {
            throw new DataFormatException($"Failed to fetch room token.");
        }

        this.realRoomId = realRoomId;
        this.roomToken = roomToken;

        log.Debug($"Got room information from server: RealRoomId={realRoomId}, RoomToken={roomToken}");
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        log.Trace("WebSocket successfully open.");
        this.Login();
    }

    private void OnClose(object? sender, CloseEventArgs e)
    {
        log.Info($"BilibiliParser.WebSocketProxy stopped: #{e.Code}, {e.Reason}");
    }

    private void OnError(object? sender, WebSocketSharp.ErrorEventArgs e)
    {
        log.Error($"BilibiliParser.WebSocketProxy error:{e.Message}");
    }

    private void OnMessage(object? sender, MessageEventArgs args)
    {
        log.Trace("OnMessage verbose.");
        waitBack = false;
        lastReceiveTime = DateTime.UtcNow;
        try
        {
            foreach (var package in this.ParsePackagesFromBinary(args.RawData))
            {
                try
                {
                    var msg = this.ConvertPackageToMessage(package);
                    log.Debug($"Receive message from bilibili: {JsonConvert.SerializeObject(msg)}");

                    if (msg == null || msg.Type is "unknown")
                    {
                        log.Warn($"Recording an unknown package: {package.Id}");

                        this.RecordQueue.Enqueue(
                            new RecordWorkItem(
                                SOURCE,
                                RecordType.Package,
                                package
                            )
                        );

                        continue;
                    }
                    else
                    {
                        // collect user information.
                        if (msg.SenderId != null)
                        {
                            var user = this.UserStorage.PickUserInformation(msg.SenderId);
                            if (user == null)
                            {
                                this.CrawlerQueue.Enqueue(
                                    new BilibiliUserCrawlerWorkItem(
                                        msg.SenderId,
                                        new ClientInfo(
                                            ClientAction.Broadcast,
                                            DistributeServiceApp.ROUTE
                                        )
                                    )
                                );
                            }
                            else
                            {
                                // fill user info into message.
                                msg.Avatar = user.Face;
                                msg.Metadata["face_url"] = user.FaceUrl!;
                                msg.SenderName = user.Name;
                            }
                        }

                        this.RecordQueue.Enqueue(
                            new RecordWorkItem(
                                SOURCE,
                                RecordType.Chat,
                                msg
                            )
                        );
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static async Task<string?> GetRealRoomId(string roomId, CancellationToken cancellationToken)
    {
        var txt = await HttpRequests.DownloadString(
            @$"{ROOM_INIT_URL}?id={roomId}",
            null,
            cancellationToken);
        var jobj = JToken.Parse(txt);
        var id = jobj["data"]?["room_id"]?.ToString();
        return id;
    }

    private static async Task<string?> GetRoomToken(string roomId, CancellationToken cancellationToken)
    {
        var txt = await HttpRequests.DownloadString(
            @$"{GET_CONF_URL}?room_id={roomId}",
            null,
            cancellationToken);
        var jobj = JToken.Parse(txt);
        var token = jobj["data"]?["token"]?.ToString();
        return token;
    }
}
