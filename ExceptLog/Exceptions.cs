using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
//using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
//using System.Windows.Controls;
using System.IO;

namespace ExceptionExtensions
{
    public record ExceptionLogFlags(bool Dialog, bool LogFile, bool LogWindow, bool LogStack)
    {
        public static readonly ExceptionLogFlags Default = new ExceptionLogFlags(false, true, true, true);
        public static int Convert(ExceptionLogFlags ef) => (ef.Dialog ? 1 : 0) | (ef.LogFile ? 0b10 : 0) | (ef.LogWindow ? 0b100 : 0) | (ef.LogStack ? 0b1000 : 0);
        public static ExceptionLogFlags Convert(int evId) => new ExceptionLogFlags((evId & 1) != 0, (evId & 0b10) != 0, (evId & 0b100) != 0, (evId & 0b1000) != 0);
        public static implicit operator int(ExceptionLogFlags ef) => Convert(ef);
        public static explicit operator ExceptionLogFlags(int evId) => Convert(evId);
    }
    public interface IExceptionLogFlags
    {
        ExceptionLogFlags exceptionLogFlags { get; }
    }

    public class FlagsException: Exception, IExceptionLogFlags
    {
        private readonly ExceptionLogFlags _exceptionLogFlags;
        public FlagsException(string? mesg = null, bool Dialog=false, bool LogFile=false, bool LogWindow=true, bool LogStack=true) 
        :base(mesg) 
        {
            _exceptionLogFlags = new ExceptionLogFlags(Dialog, LogFile, LogWindow, LogStack);
        }
        public ExceptionLogFlags exceptionLogFlags { get => _exceptionLogFlags; }
    }
    public class FlagsOperationCanceledException : OperationCanceledException, IExceptionLogFlags
    {
        private readonly ExceptionLogFlags _exceptionLogFlags;
        public FlagsOperationCanceledException(string? message = null, bool Dialog = false, bool LogFile = false, bool LogWindow = true, bool LogStack = false)
            : base(message)
        {
            _exceptionLogFlags = new ExceptionLogFlags(Dialog, LogFile, LogWindow, LogStack);
        }
        public ExceptionLogFlags exceptionLogFlags { get => _exceptionLogFlags; }
    }
    public class FlagsArgumentOutOfRangeException : ArgumentOutOfRangeException, IExceptionLogFlags
    {
        private readonly ExceptionLogFlags _exceptionLogFlags;
        public FlagsArgumentOutOfRangeException(string? paramName, object? actualValue, string? message, bool Dialog = true, bool LogFile = true, bool LogWindow = true, bool LogStack = true)
            : base(paramName, actualValue, message)
        {
            _exceptionLogFlags = new ExceptionLogFlags(Dialog, LogFile, LogWindow, LogStack);
        }
        public ExceptionLogFlags exceptionLogFlags { get => _exceptionLogFlags; }
    }
    public class FlagsArgumentException : ArgumentException, IExceptionLogFlags
    {
        private readonly ExceptionLogFlags _exceptionLogFlags;
        public FlagsArgumentException(string? paramName, string? message, bool Dialog = true, bool LogFile = true, bool LogWindow = true, bool LogStack = false)
            : base(message, paramName)
        {
            _exceptionLogFlags = new ExceptionLogFlags(Dialog, LogFile, LogWindow, LogStack);
        }
        public ExceptionLogFlags exceptionLogFlags { get => _exceptionLogFlags; }
    }

    public static class ExceptionExtension
    {
        public static ExceptionLogFlags GetExceptionExceptionLogFlags(this Exception exception)
        {
            if (exception is IExceptionLogFlags ef)
                 return ef.exceptionLogFlags;
            else return exception switch
            {
                OperationCanceledException => new ExceptionLogFlags(true, false, true, false),
                InvalidOperationException => new ExceptionLogFlags(true, false, true, true),
                ArgumentException => new ExceptionLogFlags(true, false, true, true),
                _=> ExceptionLogFlags.Default,
            };
        }

        public static void SetMessage(this Exception exception, string message)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var type = typeof(Exception);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var fieldInfo = type.GetField("_message", flags);
            fieldInfo?.SetValue(exception, message);
        }

        public static void UpdateMessage(this Exception exception)
        {
            if (exception is IExceptionLogFlags) return;
            switch (exception)
            {
                case OperationCanceledException:
                    exception.SetMessage("Операция прервана пользователем");
                    break;
            }
        }
    }
}
