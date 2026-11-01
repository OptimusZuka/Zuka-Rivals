using System.IO.MemoryMappedFiles;
using System.Text;

namespace IRacingTracker;

/// <summary>Descricao de uma variavel de telemetria publicada pelo iRacing.</summary>
public sealed record VarInfo(string Name, int Type, int Offset, int Count, string Desc, string Unit);

/// <summary>
/// Le a memoria partilhada do iRacing (o "SDK"). Nao depende de bibliotecas externas.
/// Layout do cabecalho (documentado no SDK oficial, irsdk_defines.h):
///   0 ver | 4 status | 8 tickRate | 12 sessionInfoUpdate | 16 sessionInfoLen | 20 sessionInfoOffset
///   24 numVars | 28 varHeaderOffset | 32 numBuf | 36 bufLen | 48.. varBuf[4] (16 bytes cada: tickCount, bufOffset)
/// Cada cabecalho de variavel tem 144 bytes: type, offset, count, countAsTime, name[32], desc[64], unit[32].
/// </summary>
public sealed class IrsdkReader : IDisposable
{
    private const string MapName = "Local\\IRSDKMemMapFileName";
    private const int VarHeaderSize = 144;

    private MemoryMappedFile? _mmf;
    private MemoryMappedViewAccessor? _view;
    private byte[] _buf = Array.Empty<byte>();
    private int _lastSessionUpdate = -1;

    public bool Connected { get; private set; }
    public int TickRate { get; private set; }
    public int SessionInfoUpdate { get; private set; }
    public string SessionYaml { get; private set; } = "";
    public Dictionary<string, VarInfo> Vars { get; } = new();

    /// <summary>Tenta ler uma nova frame. Devolve true se os dados estao atualizados.</summary>
    public bool Poll()
    {
        if (_view == null && !TryOpen())
        {
            Connected = false;
            return false;
        }

        try
        {
            int status = _view!.ReadInt32(4);
            if ((status & 1) == 0)
            {
                // iRacing aberto mas sem sessao ativa
                Close();
                return false;
            }

            Connected = true;
            TickRate = _view.ReadInt32(8);
            SessionInfoUpdate = _view.ReadInt32(12);

            if (Vars.Count == 0)
                ReadVarHeaders();

            if (SessionInfoUpdate != _lastSessionUpdate)
            {
                ReadSessionYaml();
                _lastSessionUpdate = SessionInfoUpdate;
            }

            return ReadLatestBuffer();
        }
        catch
        {
            Close();
            return false;
        }
    }

    private bool TryOpen()
    {
        try
        {
            _mmf = MemoryMappedFile.OpenExisting(MapName, MemoryMappedFileRights.Read);
            _view = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            return true;
        }
        catch
        {
            Close();
            return false;
        }
    }

    private void Close()
    {
        Connected = false;
        _view?.Dispose();
        _mmf?.Dispose();
        _view = null;
        _mmf = null;
        Vars.Clear();
        _lastSessionUpdate = -1;
        _buf = Array.Empty<byte>();
    }

    private void ReadVarHeaders()
    {
        int numVars = _view!.ReadInt32(24);
        int offset = _view.ReadInt32(28);
        Vars.Clear();
        for (int i = 0; i < numVars; i++)
        {
            int p = offset + i * VarHeaderSize;
            int type = _view.ReadInt32(p);
            int varOffset = _view.ReadInt32(p + 4);
            int count = _view.ReadInt32(p + 8);
            string name = ReadCString(p + 16, 32);
            string desc = ReadCString(p + 48, 64);
            string unit = ReadCString(p + 112, 32);
            if (name.Length > 0)
                Vars[name] = new VarInfo(name, type, varOffset, count, desc, unit);
        }
    }

    private string ReadCString(int pos, int maxLen)
    {
        var bytes = new byte[maxLen];
        _view!.ReadArray(pos, bytes, 0, maxLen);
        int end = Array.IndexOf(bytes, (byte)0);
        if (end < 0) end = maxLen;
        return Encoding.Latin1.GetString(bytes, 0, end);
    }

    private void ReadSessionYaml()
    {
        int len = _view!.ReadInt32(16);
        int off = _view.ReadInt32(20);
        if (len <= 0 || off <= 0) { SessionYaml = ""; return; }
        var bytes = new byte[len];
        _view.ReadArray(off, bytes, 0, len);
        // Latin1 nao perde nenhum byte; cada texto e reinterpretado depois (SessionParser.FixText)
        SessionYaml = Encoding.Latin1.GetString(bytes).TrimEnd('\0');
    }

    private bool ReadLatestBuffer()
    {
        int numBuf = Math.Min(_view!.ReadInt32(32), 4);
        int bufLen = _view.ReadInt32(36);
        if (numBuf <= 0 || bufLen <= 0) return false;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            int best = 0, bestTick = int.MinValue;
            for (int i = 0; i < numBuf; i++)
            {
                int tick = _view.ReadInt32(48 + i * 16);
                if (tick > bestTick) { bestTick = tick; best = i; }
            }

            int bufOffset = _view.ReadInt32(48 + best * 16 + 4);
            if (_buf.Length != bufLen) _buf = new byte[bufLen];
            _view.ReadArray(bufOffset, _buf, 0, bufLen);

            // Se o iRacing escreveu enquanto copiavamos, repetir
            if (_view.ReadInt32(48 + best * 16) == bestTick)
                return true;
        }
        return false;
    }

    // ---- Leitura de valores (tipos do SDK: 0 char, 1 bool, 2 int, 3 bitfield, 4 float, 5 double) ----

    public bool Has(string name) => Vars.ContainsKey(name);

    public bool[]? Bools(string name)
    {
        if (!Vars.TryGetValue(name, out var v) || v.Type != 1 || _buf.Length == 0) return null;
        var r = new bool[v.Count];
        for (int i = 0; i < v.Count; i++) r[i] = _buf[v.Offset + i] != 0;
        return r;
    }

    public int[]? Ints(string name)
    {
        if (!Vars.TryGetValue(name, out var v) || (v.Type != 2 && v.Type != 3) || _buf.Length == 0) return null;
        var r = new int[v.Count];
        for (int i = 0; i < v.Count; i++) r[i] = BitConverter.ToInt32(_buf, v.Offset + 4 * i);
        return r;
    }

    public float[]? Floats(string name)
    {
        if (!Vars.TryGetValue(name, out var v) || v.Type != 4 || _buf.Length == 0) return null;
        var r = new float[v.Count];
        for (int i = 0; i < v.Count; i++) r[i] = BitConverter.ToSingle(_buf, v.Offset + 4 * i);
        return r;
    }

    public double[]? Doubles(string name)
    {
        if (!Vars.TryGetValue(name, out var v) || v.Type != 5 || _buf.Length == 0) return null;
        var r = new double[v.Count];
        for (int i = 0; i < v.Count; i++) r[i] = BitConverter.ToDouble(_buf, v.Offset + 8 * i);
        return r;
    }

    public void Dispose() => Close();
}
