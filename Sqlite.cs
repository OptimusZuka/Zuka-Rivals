using System.Runtime.InteropServices;

// Procura as DLLs nativas apenas na pasta do sistema (seguranca).
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace IRacingTracker;

/// <summary>
/// Acesso ao SQLite atraves do winsqlite3.dll, que ja vem com o Windows 10/11 e esta assinado
/// pela Microsoft. Assim nao e preciso instalar nenhuma biblioteca de terceiros com DLLs nativas
/// sem assinatura (que o Controlo Inteligente de Aplicacoes poderia bloquear).
/// </summary>
internal static class Native
{
    private const string Lib = "winsqlite3.dll";

    public const int SQLITE_OK = 0;
    public const int SQLITE_ROW = 100;
    public const int SQLITE_DONE = 101;
    public const int SQLITE_OPEN_READWRITE = 0x2;
    public const int SQLITE_OPEN_CREATE = 0x4;
    public static readonly IntPtr SQLITE_TRANSIENT = new IntPtr(-1);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_open_v2(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string filename, out IntPtr db, int flags, IntPtr vfs);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_close_v2(IntPtr db);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_prepare_v2(
        IntPtr db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, int nByte, out IntPtr stmt, IntPtr tail);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_step(IntPtr stmt);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_finalize(IntPtr stmt);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_changes(IntPtr db);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr sqlite3_errmsg(IntPtr db);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_bind_null(IntPtr stmt, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_bind_int64(IntPtr stmt, int index, long value);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_bind_double(IntPtr stmt, int index, double value);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_bind_text(
        IntPtr stmt, int index, [MarshalAs(UnmanagedType.LPUTF8Str)] string value, int n, IntPtr destructor);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_column_count(IntPtr stmt);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_column_type(IntPtr stmt, int col);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern long sqlite3_column_int64(IntPtr stmt, int col);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern double sqlite3_column_double(IntPtr stmt, int col);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr sqlite3_column_text(IntPtr stmt, int col);
}

/// <summary>Ligacao simples a uma base de dados SQLite (so para uso na thread da interface).</summary>
public sealed class SqliteDb : IDisposable
{
    private IntPtr _db = IntPtr.Zero;

    public SqliteDb(string path)
    {
        int rc = Native.sqlite3_open_v2(path, out _db, Native.SQLITE_OPEN_READWRITE | Native.SQLITE_OPEN_CREATE, IntPtr.Zero);
        if (rc != Native.SQLITE_OK)
        {
            string msg = _db != IntPtr.Zero ? ErrMsg() : "codigo " + rc;
            if (_db != IntPtr.Zero) Native.sqlite3_close_v2(_db);
            _db = IntPtr.Zero;
            throw new InvalidOperationException("Nao foi possivel abrir a base de dados: " + msg);
        }
    }

    private string ErrMsg()
    {
        return Marshal.PtrToStringUTF8(Native.sqlite3_errmsg(_db)) ?? "";
    }

    private IntPtr Prepare(string sql)
    {
        int rc = Native.sqlite3_prepare_v2(_db, sql, -1, out IntPtr stmt, IntPtr.Zero);
        if (rc != Native.SQLITE_OK)
            throw new InvalidOperationException("SQLite (preparar): " + ErrMsg() + "  | " + sql);
        return stmt;
    }

    private void Bind(IntPtr stmt, object?[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            int idx = i + 1;
            int rc;
            switch (args[i])
            {
                case null: rc = Native.sqlite3_bind_null(stmt, idx); break;
                case int n: rc = Native.sqlite3_bind_int64(stmt, idx, n); break;
                case long l: rc = Native.sqlite3_bind_int64(stmt, idx, l); break;
                case bool b: rc = Native.sqlite3_bind_int64(stmt, idx, b ? 1 : 0); break;
                case double d: rc = Native.sqlite3_bind_double(stmt, idx, d); break;
                case string s: rc = Native.sqlite3_bind_text(stmt, idx, s, -1, Native.SQLITE_TRANSIENT); break;
                default: throw new ArgumentException("Tipo de parametro nao suportado: " + args[i]!.GetType());
            }
            if (rc != Native.SQLITE_OK)
                throw new InvalidOperationException("SQLite (parametro " + idx + "): " + ErrMsg());
        }
    }

    /// <summary>Executa INSERT/UPDATE/DELETE/CREATE... e devolve o numero de linhas alteradas.</summary>
    public int Execute(string sql, params object?[] args)
    {
        IntPtr stmt = Prepare(sql);
        try
        {
            Bind(stmt, args);
            int rc = Native.sqlite3_step(stmt);
            if (rc != Native.SQLITE_DONE && rc != Native.SQLITE_ROW)
                throw new InvalidOperationException("SQLite (executar): " + ErrMsg() + "  | " + sql);
            return Native.sqlite3_changes(_db);
        }
        finally
        {
            Native.sqlite3_finalize(stmt);
        }
    }

    /// <summary>Executa um SELECT. Cada linha e um array: long, double, string ou null.</summary>
    public List<object?[]> Query(string sql, params object?[] args)
    {
        var rows = new List<object?[]>();
        IntPtr stmt = Prepare(sql);
        try
        {
            Bind(stmt, args);
            while (true)
            {
                int rc = Native.sqlite3_step(stmt);
                if (rc == Native.SQLITE_DONE) break;
                if (rc != Native.SQLITE_ROW)
                    throw new InvalidOperationException("SQLite (consultar): " + ErrMsg() + "  | " + sql);

                int cols = Native.sqlite3_column_count(stmt);
                var row = new object?[cols];
                for (int c = 0; c < cols; c++)
                {
                    switch (Native.sqlite3_column_type(stmt, c))
                    {
                        case 1: row[c] = Native.sqlite3_column_int64(stmt, c); break;
                        case 2: row[c] = Native.sqlite3_column_double(stmt, c); break;
                        case 3: row[c] = Marshal.PtrToStringUTF8(Native.sqlite3_column_text(stmt, c)); break;
                        default: row[c] = null; break;
                    }
                }
                rows.Add(row);
            }
        }
        finally
        {
            Native.sqlite3_finalize(stmt);
        }
        return rows;
    }

    public void Dispose()
    {
        if (_db != IntPtr.Zero)
        {
            Native.sqlite3_close_v2(_db);
            _db = IntPtr.Zero;
        }
    }
}
