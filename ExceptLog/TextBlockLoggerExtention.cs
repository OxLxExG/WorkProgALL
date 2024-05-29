using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Karambolo.Extensions.Logging.File;

namespace TextBlockLogging
{
    public static class TextBlockLoggerExtention
    {
        //private static ILoggingBuilder AddTextBlock<TProvider>(this ILoggingBuilder builder) 
        //    where TProvider : TextBlockLoggerProvider
        //{
        //    if (builder == null)
        //    {
        //        throw new ArgumentNullException("builder");
        //    }

        //    builder.AddConfiguration();
        //    builder.AddTextBlockFormatter<TextBlockLoggerFormatter,TProvider, TextBlockLoggerFormatterOptions>(optionsName);
        //    builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, TProvider>());
        //    //LoggerProviderOptions.RegisterProviderOptions<TextBlockOptions, TProvider>(builder.Services);

        //    builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TextBlockOptions>,
        //        TextBoxLoggerOptionsChangeTokenSource<TProvider>>((IServiceProvider sp) =>
        //            new TextBoxLoggerOptionsChangeTokenSource<TProvider>(optionsName,
        //                sp.GetRequiredService<ILoggerProviderConfiguration<TProvider>>())));
        //    builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TextBlockOptions>,
        //        TextBlockLoggerOptionsSetup<TProvider>>((IServiceProvider sp) =>
        //        new TextBlockLoggerOptionsSetup<TProvider>(optionsName, sp.GetRequiredService<ILoggerProviderConfiguration<TProvider>>())));
        //    return builder;
        //}
        //internal sealed class TextBoxLoggerOptionsChangeTokenSource<TProvider> : ConfigurationChangeTokenSource<TextBlockOptions> 
        //    where TProvider : TextBlockLoggerProvider
        //{
        //    public TextBoxLoggerOptionsChangeTokenSource(string optionsName, ILoggerProviderConfiguration<TProvider> providerConfiguration)
        //        : base(optionsName, providerConfiguration.Configuration)
        //    {
        //    }
        //}
        //internal sealed class TextBlockLoggerOptionsSetup<TProvider> : NamedConfigureFromConfigurationOptions<TextBlockOptions> 
        //    where TProvider : TextBlockLoggerProvider
        //{
        //    public TextBlockLoggerOptionsSetup(string optionsName, ILoggerProviderConfiguration<TProvider> providerConfiguration)
        //        : base(optionsName, providerConfiguration.Configuration)
        //    {
        //    }
        //}
        public static ILoggingBuilder AddTextBlock<TProvider, TOptions, TFormatter>(this ILoggingBuilder builder,
             Action<TOptions>? configure = null, 
             Action<TFormatter>? formatter = null)
             where TProvider : TextBlockLoggerProvider
             where TOptions : TextBlockOptions
             where TFormatter : TextBlockFormatter
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            builder.AddConfiguration();


            var optionsName = typeof(TProvider).ToString();

            builder.AddTextBlockFormatter<TFormatter, TProvider, TextBlockLoggerFormatterOptions>(optionsName);

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, TProvider>());
            //LoggerProviderOptions.RegisterProviderOptions<TOptions, TProvider>(builder.Services);
            //builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, 
            //    BoxLoggerProviderConfigureOptions<TOptions, TProvider>>());
            //builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, 
            //    LoggerProviderOptionsChangeTokenSource<TOptions, TProvider>>());



            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TextBlockOptions>,
                OptionsChangeTokenSource<TProvider>>(sp =>
                    new OptionsChangeTokenSource<TProvider>(optionsName, sp.GetRequiredService<ILoggerProviderConfiguration<TProvider>>())));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TextBlockOptions>, OptionsSetup<TProvider>>(sp =>
                new OptionsSetup<TProvider>(optionsName, sp.GetRequiredService<ILoggerProviderConfiguration<TProvider>>())));

            if (configure != null)
            {
                builder.Services.Configure(configure);
            }
            if (formatter != null)
            {
                builder.Services.Configure(formatter);
            }

            return builder;
        }

        internal sealed class BoxLoggerProviderConfigureOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] 
        TOptions, TProvider> : ConfigureFromConfigurationOptions<TOptions>
            where TOptions : TextBlockOptions
            where TProvider : TextBlockLoggerProvider
        {
            internal const string RequiresDynamicCodeMessage = "Binding TOptions to configuration values may require generating dynamic code at runtime.";
            internal const string TrimmingRequiresUnreferencedCodeMessage = "TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.";

            [RequiresDynamicCode(RequiresDynamicCodeMessage)]
            [RequiresUnreferencedCode(TrimmingRequiresUnreferencedCodeMessage)]
            public BoxLoggerProviderConfigureOptions(ILoggerProviderConfiguration<TProvider> providerConfiguration)
                : base(providerConfiguration.Configuration)
            {
            }
        }

        internal sealed class OptionsChangeTokenSource<TProvider> : ConfigurationChangeTokenSource<TextBlockOptions>
            where TProvider : TextBlockLoggerProvider
        {
            public OptionsChangeTokenSource(string optionsName, ILoggerProviderConfiguration<TProvider> providerConfiguration)
                : base(optionsName, providerConfiguration.Configuration) { }
        }

        internal sealed class OptionsSetup<TProvider> : NamedConfigureFromConfigurationOptions<TextBlockOptions>
            where TProvider : TextBlockLoggerProvider
        {
            public OptionsSetup(string optionsName, ILoggerProviderConfiguration<TProvider> providerConfiguration)
                : base(optionsName, providerConfiguration.Configuration) { }
        }

        //public static ILoggingBuilder AddTextBlock(this ILoggingBuilder builder, Action<TextBlockOptions> configure)
        //{
        //    builder.AddTextBlock();
        //    builder.Services.Configure(configure);
        //    return builder;
        //}
        //public static ILoggingBuilder AddTextBlock(this ILoggingBuilder builder,
        //    Action<TextBlockOptions> configure, Action<TextBlockLoggerFormatterOptions> formatter)
        //{
        //    builder.AddTextBlock();
        //    builder.Services.Configure(configure);
        //    builder.Services.Configure(formatter);
        //    return builder;
        //}
        //public static ILoggingBuilder AddTextBlock<TProvider>(this ILoggingBuilder builder,
        //     Action<TextBlockOptions>? configure = null, Action<TextBlockLoggerFormatterOptions>? formatter = null,
        //     string? optionsName = null) where TProvider : TextBlockLoggerProvider
        //{
        //    if (optionsName == null)
        //    {
        //        optionsName = typeof(TProvider)!.ToString();
        //    }
        //    builder.AddConfiguration();

        //    builder.AddTextBlockFormatter<TextBlockLoggerFormatter, TextBlockLoggerFormatterOptions>();

        //    builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, TProvider>());

        //    LoggerProviderOptions.RegisterProviderOptions<TextBlockOptions, TProvider>(builder.Services);

        //    if (configure != null) builder.Services.Configure(optionsName, configure);
        //    if (formatter != null) builder.Services.Configure(optionsName, formatter);

        //    return builder;
        //}

        //public static ILoggingBuilder AddTextBlock(this ILoggingBuilder builder)
        //{
        //    builder.AddConfiguration();

        //    builder.AddTextBlockFormatter<TextBlockLoggerFormatter, TextBlockLoggerProvider, TextBlockLoggerFormatterOptions>();

        //    builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, TextBlockLoggerProvider>());
        //    LoggerProviderOptions.RegisterProviderOptions<TextBlockOptions, TextBlockLoggerProvider>(builder.Services);
        //    return builder;
        //}
        public static ILoggingBuilder AddTextBlockFormatter<
                TFormatter,
                TProvider,
                TOptions>(this ILoggingBuilder builder, string optionsName)
            where TOptions : ConsoleFormatterOptions
            where TProvider: TextBlockLoggerProvider
            where TFormatter : TextBlockFormatter
        {

            builder.Services.Add(ServiceDescriptor.Singleton<TFormatter>(
                (sp) =>
                {
                    return ActivatorUtilities.CreateInstance<TFormatter>(sp, new object[1] { optionsName }); 
                 })); ;
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TOptions>,
                TextLoggerFormatterConfigureOptions<TProvider, TOptions>> (sp=> 
                new TextLoggerFormatterConfigureOptions<TProvider, TOptions>(optionsName, sp.GetRequiredService<ILoggerProviderConfiguration<TProvider>>())));
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>,
                TextLoggerFormatterOptionsChangeTokenSource<TProvider, TOptions >>(sp=>
                new TextLoggerFormatterOptionsChangeTokenSource<TProvider, TOptions>(optionsName, 
                sp.GetRequiredService<ILoggerProviderConfiguration<TProvider>>())));
            return builder;
        }

        internal sealed class TextLoggerFormatterConfigureOptions<TProvider, TOptions> : NamedConfigureFromConfigurationOptions<TOptions>
            where TOptions : ConsoleFormatterOptions
            where TProvider : TextBlockLoggerProvider
        {
            public TextLoggerFormatterConfigureOptions(string optionsName, ILoggerProviderConfiguration<TProvider> providerConfiguration)
                : base(optionsName, providerConfiguration.Configuration.GetSection("FormatterOptions")) { }

            //public TextLoggerFormatterConfigureOptions(ILoggerProviderConfiguration<TProvider> providerConfiguration)
            //    : base(providerConfiguration.Configuration.GetSection("FormatterOptions")) { }
        }

        internal sealed class TextLoggerFormatterOptionsChangeTokenSource<TProvider, TOptions> : ConfigurationChangeTokenSource<TOptions>
            where TOptions : ConsoleFormatterOptions
            where TProvider : TextBlockLoggerProvider
        {
            public TextLoggerFormatterOptionsChangeTokenSource(string optionsName, ILoggerProviderConfiguration<TProvider> providerConfiguration)
                : base(optionsName, providerConfiguration.Configuration.GetSection("FormatterOptions")) { }

            public TextLoggerFormatterOptionsChangeTokenSource(ILoggerProviderConfiguration<TProvider> providerConfiguration)
                : base(providerConfiguration.Configuration.GetSection("FormatterOptions")) { }
        }


    }
}
