namespace Malimbe.FodyRunner
{
    using System;

    public sealed class NotConfiguredException : Exception
    {
        public NotConfiguredException() : base(
            $"The {nameof(Runner)} is not configured. Configure it before calling {nameof(Runner.RunAsync)}.")
        {
        }
    }
}
