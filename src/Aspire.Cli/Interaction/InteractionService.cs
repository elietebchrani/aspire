// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Interaction;

internal class InteractionService : IInteractionService
{
    public async Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots3)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync(statusText, (context) => action());
    }

    public void ShowStatus(string statusText, Action action)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots3)
            .SpinnerStyle(Style.Parse("purple"))
            .Start(statusText, (context) => action());
    }

    public async Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promptText, nameof(promptText));
        var prompt = new TextPrompt<string>(promptText);

        if (defaultValue is not null)
        {
            prompt.DefaultValue(defaultValue);
            prompt.ShowDefaultValue();
        }

        if (validator is not null)
        {
            prompt.Validate(validator);
        }

        return await AnsiConsole.PromptAsync(prompt, cancellationToken);
    }

    public async Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(promptText, nameof(promptText));
        ArgumentNullException.ThrowIfNull(choices, nameof(choices));
        ArgumentNullException.ThrowIfNull(choiceFormatter, nameof(choiceFormatter));

        var prompt = new SelectionPrompt<T>()
            .Title(promptText)
            .UseConverter(choiceFormatter)
            .AddChoices(choices)
            .PageSize(10)
            .EnableSearch()
            .HighlightStyle(Style.Parse("darkmagenta"));

        return await AnsiConsole.PromptAsync(prompt, cancellationToken);
    }

    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingSdkVersion)
    {
        var cliInformationalVersion = VersionHelper.GetDefaultTemplateVersion();
        
        DisplayError("The app host is not compatible. Consider upgrading the app host or Aspire CLI.");
        Console.WriteLine();
        AnsiConsole.MarkupLine($"\t[bold]Aspire Hosting SDK Version[/]: {appHostHostingSdkVersion}");
        AnsiConsole.MarkupLine($"\t[bold]Aspire CLI Version[/]: {cliInformationalVersion}");
        AnsiConsole.MarkupLine($"\t[bold]Required Capability[/]: {ex.RequiredCapability}");
        Console.WriteLine();
        return ExitCodeConstants.AppHostIncompatible;
    }

    public void DisplayError(string errorMessage)
    {
        DisplayMessage("thumbs_down", $"[red bold]{errorMessage}[/]");
    }

    public void DisplayMessage(string emoji, string message)
    {
        AnsiConsole.MarkupLine($":{emoji}:  {message}");
    }

    public void DisplaySuccess(string message)
    {
        DisplayMessage("thumbs_up", message);
    }

    public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green bold]Dashboard[/]:");
        if (dashboardUrls.CodespacesUrlWithLoginToken is not null)
        {
            AnsiConsole.MarkupLine($":chart_increasing:  Direct: [link={dashboardUrls.BaseUrlWithLoginToken}]{dashboardUrls.BaseUrlWithLoginToken}[/]");
            AnsiConsole.MarkupLine($":chart_increasing:  Codespaces: [link={dashboardUrls.CodespacesUrlWithLoginToken}]{dashboardUrls.CodespacesUrlWithLoginToken}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($":chart_increasing:  [link={dashboardUrls.BaseUrlWithLoginToken}]{dashboardUrls.BaseUrlWithLoginToken}[/]");
        }
        AnsiConsole.WriteLine();
    }
}
