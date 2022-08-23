using System.Text;
using LiteDB;
using LiveChatLib2.Utils;

namespace LiveChatLib2.Models.RemotePackages;

internal class BilibiliRemotePackage
{
    [BsonField("_id")]
    public string Id { get; set; }
    public int Length => this.HeadLength + (this.Body?.Length ?? 0);
    public short HeadLength { get; private set; }
    public short ProtoVer { get; private set; }
    public BilibiliMessageType MessageType { get; private set; }
    public int Sequence { get; private set; }
    public byte[]? Body { get; private set; }

    [BsonIgnore]
    public Encoding BodyEncoding { get; set; }
    public string Content
    {
        get => this.Body == null ? "" : this.BodyEncoding.GetString(this.Body);
        set => this.Body = this.BodyEncoding.GetBytes(value);
    }
    public bool MultiMessage { get; private set; } = false;

    public BilibiliRemotePackage()
    {
        this.BodyEncoding = Encoding.UTF8;
        this.Id = new NUlid.Ulid().ToString();
    }
    public static IEnumerable<BilibiliRemotePackage> ExtractPackage(BilibiliMessageType msgType, byte[] body, Encoding? encoding = null, short protover = 0x1, short headlen = 0x10, int sequence = 0x1)
    {
        var package = new BilibiliRemotePackage
        {
            HeadLength = headlen,
            ProtoVer = protover,
            MessageType = msgType,
            Sequence = sequence,
            BodyEncoding = encoding ?? Encoding.UTF8
        };

        if (protover == 2 && msgType == BilibiliMessageType.Command)
        {
            var deflateStream = new MemoryStream(body, 2, body.Length-2);
            var extracted = DeflateHelper.Extract(deflateStream);
            using var ms = new MemoryStream(extracted);
            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new BinaryReader(ms);

            do
            {
                var p = new BilibiliRemotePackage()
                {
                    HeadLength = headlen,
                    ProtoVer = protover,
                    Sequence = sequence
                };
                var msgLength = reader.ReadBytes(4).ByteToInt32(true);
                var msgHeaderLength = reader.ReadBytes(2).ByteToInt16(true);
                var msgVer = reader.ReadBytes(2).ByteToInt16(true);
                var msgAc = reader.ReadBytes(4).ByteToInt32(true);
                var msgParam = reader.ReadBytes(4).ByteToInt32(true);
                switch (msgAc)
                {
                    case 3:
                        p.MessageType = BilibiliMessageType.ServerHeart;
                        break;

                    case 5:
                        p.MessageType = BilibiliMessageType.Command;
                        break;

                    case 8:
                        p.MessageType = BilibiliMessageType.Auth;
                        break;
                }

                p.Body = reader.ReadBytes(msgLength - 16);
                yield return p;
            } while (ms.Length - ms.Position > 16);
        }
        else
        {
            package.Body = body;
        }

        yield return package;
    }

    public BilibiliRemotePackage(BilibiliMessageType msgType, string content, Encoding? encoding = null, short protover = 0x1, short headlen = 0x10, int sequence = 0x1)
        : this()
    {
        this.HeadLength = headlen;
        this.ProtoVer = protover;
        this.MessageType = msgType;
        this.Sequence = sequence;
        this.BodyEncoding = encoding ?? Encoding.UTF8;
        this.Content = content;
    }

    public byte[] ToByteArray()
    {
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(this.Length.ToByteArray(true));
            writer.Write(this.HeadLength.ToByteArray(true));
            writer.Write(this.ProtoVer.ToByteArray(true));
            writer.Write(((int)this.MessageType).ToByteArray(true));
            writer.Write(this.Sequence.ToByteArray(true));
            if (this.Body != null)
                writer.Write(this.Body);

            writer.Flush();
        }

        var result = ms.ToArray();
        return result;
    }

    public static IEnumerable<BilibiliRemotePackage> LoadFromByteArray(byte[] package)
    {
        var ms = new MemoryStream(package);
        using var reader = new BinaryReader(ms);
        var length = reader.ReadBytes(4).ByteToInt32(true);
        if (length != package.Length)
            throw new Exception("LOADING PACKAGE FAILED: The length of the package does not match with its header.");

        var headlength = reader.ReadBytes(2).ByteToInt16(true);
        var protover = reader.ReadBytes(2).ByteToInt16(true);
        var command = reader.ReadBytes(4).ByteToInt32(true);
        var sequence = reader.ReadBytes(4).ByteToInt32(true);
        ms.Seek(headlength, SeekOrigin.Begin);
        var body = reader.ReadBytes(length - headlength);

        foreach (var item in ExtractPackage((BilibiliMessageType)command, body, Encoding.UTF8, protover, headlength, sequence)) 
        {
            yield return item;
        }
    }
}

public enum BilibiliMessageType
{
    ClientHeart = 0x02,
    Renqi = 0x03,
    Command = 0x05,
    Auth = 0x07,
    ServerHeart = 0x08,
}
