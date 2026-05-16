using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace PS2Disassembler
{
    internal sealed class PineIpcClient : IDisposable
    {
        private string _host = AppSettings.DefaultDebugHost;
        private int _port = AppSettings.DefaultPinePort;
        private const uint EeRamSizeBytes = 0x02000000u;
        private const int MaxIpcPayloadBytes = 600000;

        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly object _sync = new();

        // Pre-allocated command payload buffers (safe to reuse because all calls are
        // serialised under _sync, and SendCommand writes them synchronously before returning).
        // Eliminates 4 heap allocations per Read8 / Read32 / Write8 / Write32 call.
        private readonly byte[] _buf5  = new byte[5];  // MsgRead8 / MsgRead32 payload
        private readonly byte[] _buf6  = new byte[6];  // MsgWrite8 payload
        private readonly byte[] _buf9  = new byte[9];  // MsgWrite32 payload
        private readonly byte[] _buf1  = new byte[1];  // single-byte command (Version, Status…)

        // Pre-allocated length-prefix buffer; replaces BitConverter.GetBytes() per call.
        private readonly byte[] _lenBuf = new byte[4];
        // Pre-allocated receive buffer for the 4-byte response-length field.
        private readonly byte[] _rspLenBuf = new byte[4];

        private enum PineCmd : byte
        {
            MsgRead8 = 0x00,
            MsgRead16 = 0x01,
            MsgRead32 = 0x02,
            MsgRead64 = 0x03,
            MsgWrite8 = 0x04,
            MsgWrite16 = 0x05,
            MsgWrite32 = 0x06,
            MsgWrite64 = 0x07,
            MsgVersion = 0x08,
            MsgSaveState = 0x09,
            MsgLoadState = 0x0A,
            MsgTitle = 0x0B,
            MsgID = 0x0C,
            MsgUUID = 0x0D,
            MsgGameVersion = 0x0E,
            MsgStatus = 0x0F,
        }

        public bool IsConnected => _client?.Connected == true && _stream != null;
        public string Host => _host;
        public int Port => _port;

        public void Configure(string? host, int port)
        {
            lock (_sync)
            {
                string nextHost = AppSettings.NormalizeDebugHost(host);
                int nextPort = AppSettings.NormalizePort(port, AppSettings.DefaultPinePort);
                bool endpointChanged = !string.Equals(_host, nextHost, StringComparison.OrdinalIgnoreCase) || _port != nextPort;
                _host = nextHost;
                _port = nextPort;
                if (endpointChanged)
                    Disconnect();
            }
        }

        public void Connect()
        {
            lock (_sync)
            {
                Disconnect();
                var client = new TcpClient();
                var ar = client.BeginConnect(_host, _port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(150)))
                {
                    client.Close();
                    throw new IOException("PINE timeout.");
                }
                client.EndConnect(ar);
                client.NoDelay = true;
                client.ReceiveTimeout = 200;
                client.SendTimeout = 200;
                _client = client;
                _stream = client.GetStream();
            }
        }

        public void Disconnect()
        {
            lock (_sync)
            {
                try { _stream?.Dispose(); } catch { }
                try { _client?.Close(); } catch { }
                _stream = null;
                _client = null;
            }
        }

        public string GetVersionSafe() => GetString(PineCmd.MsgVersion);
        public string GetTitleSafe() => GetString(PineCmd.MsgTitle);
        public uint GetStatusSafe()
        {
            _buf1[0] = (byte)PineCmd.MsgStatus;
            byte[] rsp = SendCommand(_buf1);
            if (rsp.Length < 5)
                return 0xFFFFFFFFu;
            return BitConverter.ToUInt32(rsp, 1);
        }

        public byte[] ReadMemory(uint addr, int length)
        {
            if (length <= 0)
                return Array.Empty<byte>();

            addr = NormalizeAndValidateEeRamRange(addr, length);

            byte[] result = new byte[length];
            int i = 0;
            while (i < length)
            {
                uint cur = addr + (uint)i;
                int remaining = length - i;
                if ((cur & 3u) == 0 && remaining >= 4)
                {
                    uint value = Read32(cur);
                    byte[] word = BitConverter.GetBytes(value);
                    Buffer.BlockCopy(word, 0, result, i, 4);
                    i += 4;
                }
                else
                {
                    result[i] = Read8(cur);
                    i++;
                }
            }
            return result;
        }

        public void WriteMemory(uint addr, byte[] data)
        {
            if (data == null || data.Length == 0)
                return;

            addr = NormalizeAndValidateEeRamRange(addr, data.Length);
            WriteMemoryBatched(addr, data);
        }

        public void WritePatchesBatched(IReadOnlyList<(uint Addr, byte[] Bytes)> patches)
        {
            if (patches == null || patches.Count == 0)
                return;

            byte[] payload = new byte[MaxIpcPayloadBytes];
            int payloadLength = 0;

            foreach (var (rawAddr, bytes) in patches)
            {
                if (bytes == null || bytes.Length == 0)
                    continue;

                uint addr = NormalizeAndValidateEeRamRange(rawAddr, bytes.Length);
                AppendWriteCommands(addr, bytes, payload, ref payloadLength);
            }

            FlushPayload(payload, ref payloadLength);
        }

        private void WriteMemoryBatched(uint addr, byte[] data)
        {
            byte[] payload = new byte[MaxIpcPayloadBytes];
            int payloadLength = 0;
            AppendWriteCommands(addr, data, payload, ref payloadLength);
            FlushPayload(payload, ref payloadLength);
        }

        private void AppendWriteCommands(uint addr, byte[] data, byte[] payload, ref int payloadLength)
        {
            int i = 0;
            while (i < data.Length)
            {
                uint cur = addr + (uint)i;
                int remaining = data.Length - i;
                int valueSize = SelectWriteValueSize(cur, remaining);
                int commandSize = 1 + 4 + valueSize;

                if (payloadLength + commandSize > payload.Length)
                    FlushPayload(payload, ref payloadLength);

                payload[payloadLength++] = (byte)GetWriteCommand(valueSize);
                WriteUInt32LE(payload, payloadLength, cur);
                payloadLength += 4;

                switch (valueSize)
                {
                    case 8:
                        ulong value64 = BitConverter.ToUInt64(data, i);
                        WriteUInt64LE(payload, payloadLength, value64);
                        break;
                    case 4:
                        uint value32 = BitConverter.ToUInt32(data, i);
                        WriteUInt32LE(payload, payloadLength, value32);
                        break;
                    case 2:
                        ushort value16 = BitConverter.ToUInt16(data, i);
                        payload[payloadLength + 0] = (byte)(value16 & 0xFF);
                        payload[payloadLength + 1] = (byte)(value16 >> 8);
                        break;
                    default:
                        payload[payloadLength] = data[i];
                        break;
                }

                payloadLength += valueSize;
                i += valueSize;
            }
        }

        private void FlushPayload(byte[] payload, ref int payloadLength)
        {
            if (payloadLength <= 0)
                return;

            SendCommand(payload, payloadLength);
            payloadLength = 0;
        }

        private static int SelectWriteValueSize(uint addr, int remaining)
        {
            if ((addr & 7u) == 0 && remaining >= 8)
                return 8;
            if ((addr & 3u) == 0 && remaining >= 4)
                return 4;
            if ((addr & 1u) == 0 && remaining >= 2)
                return 2;
            return 1;
        }

        private static PineCmd GetWriteCommand(int valueSize) => valueSize switch
        {
            8 => PineCmd.MsgWrite64,
            4 => PineCmd.MsgWrite32,
            2 => PineCmd.MsgWrite16,
            _ => PineCmd.MsgWrite8
        };

        private static uint NormalizeAndValidateEeRamRange(uint addr, int length)
        {
            uint normalized = addr & 0x1FFFFFFFu;
            if (length <= 0)
                return normalized;

            ulong endExclusive = (ulong)normalized + (uint)length;
            if (normalized >= EeRamSizeBytes || endExclusive > EeRamSizeBytes)
                throw new IOException($"PINE EE RAM access out of range: 0x{normalized:X8}, len {length}.");

            return normalized;
        }

        private byte Read8(uint addr)
        {
            _buf5[0] = (byte)PineCmd.MsgRead8;
            WriteUInt32LE(_buf5, 1, addr);
            byte[] rsp = SendCommand(_buf5);
            return rsp[1];
        }

        private uint Read32(uint addr)
        {
            _buf5[0] = (byte)PineCmd.MsgRead32;
            WriteUInt32LE(_buf5, 1, addr);
            byte[] rsp = SendCommand(_buf5);
            return BitConverter.ToUInt32(rsp, 1);
        }

        private void Write8(uint addr, byte value)
        {
            _buf6[0] = (byte)PineCmd.MsgWrite8;
            WriteUInt32LE(_buf6, 1, addr);
            _buf6[5] = value;
            SendCommand(_buf6);
        }

        private void Write32(uint addr, uint value)
        {
            _buf9[0] = (byte)PineCmd.MsgWrite32;
            WriteUInt32LE(_buf9, 1, addr);
            WriteUInt32LE(_buf9, 5, value);
            SendCommand(_buf9);
        }

        private string GetString(PineCmd cmd)
        {
            _buf1[0] = (byte)cmd;
            byte[] rsp = SendCommand(_buf1);
            if (rsp.Length < 5)
                return string.Empty;
            int len = (int)BitConverter.ToUInt32(rsp, 1);
            if (len <= 0 || rsp.Length < 5 + len)
                return string.Empty;
            return Encoding.UTF8.GetString(rsp, 5, len).TrimEnd('\0');
        }

        private byte[] SendCommand(byte[] payload) => SendCommand(payload, payload.Length);

        private byte[] SendCommand(byte[] payload, int payloadLength)
        {
            lock (_sync)
            {
                if (_stream == null || _client == null || !_client.Connected)
                    throw new IOException("PINE not connected.");
                if (payloadLength <= 0 || payloadLength > payload.Length)
                    throw new IOException($"Invalid PINE payload length: {payloadLength}");

                // Write the 4-byte length prefix using the pre-allocated buffer.
                int msgLen = payloadLength + 4;
                _lenBuf[0] = (byte) msgLen;
                _lenBuf[1] = (byte)(msgLen >> 8);
                _lenBuf[2] = (byte)(msgLen >> 16);
                _lenBuf[3] = (byte)(msgLen >> 24);
                _stream.Write(_lenBuf, 0, 4);
                _stream.Write(payload, 0, payloadLength);
                _stream.Flush();

                // Read response length into pre-allocated buffer (no allocation).
                ReadInto(_rspLenBuf, 4);
                int respSize = BitConverter.ToInt32(_rspLenBuf, 0);
                if (respSize < 5 || respSize > 650000)
                    throw new IOException($"Invalid PINE response size: {respSize}");

                byte[] response = ReadExact(respSize - 4);
                if (response.Length < 1)
                    throw new IOException("Empty PINE response.");
                if (response[0] != 0)
                    throw new IOException($"PINE error code 0x{response[0]:X2}.");
                return response;
            }
        }

        // Reads exactly `length` bytes into a caller-supplied buffer (no allocation).
        private void ReadInto(byte[] buffer, int length)
        {
            int offset = 0;
            while (offset < length)
            {
                int read = _stream!.Read(buffer, offset, length - offset);
                if (read <= 0)
                    throw new IOException("PINE socket closed.");
                offset += read;
            }
        }

        private byte[] ReadExact(int length)
        {
            byte[] buffer = new byte[length];
            ReadInto(buffer, length);
            return buffer;
        }

        private static void WriteUInt32LE(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private static void WriteUInt64LE(byte[] buffer, int offset, ulong value)
        {
            buffer[offset + 0] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 4] = (byte)((value >> 32) & 0xFF);
            buffer[offset + 5] = (byte)((value >> 40) & 0xFF);
            buffer[offset + 6] = (byte)((value >> 48) & 0xFF);
            buffer[offset + 7] = (byte)((value >> 56) & 0xFF);
        }

        public void Dispose() => Disconnect();
    }
}
