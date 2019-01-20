namespace Malimbe.FodyRunner
{
    using System;

    [Flags]
    public enum LogLevel
    {
        None = 0,
        Debug = 1 << 0,
        Info = 1 << 1,
        Warning = 1 << 2,
        Error = 1 << 3,
        // ReSharper disable once UnusedMember.Global
        All = Debug | Info | Warning | Error
    }
}
