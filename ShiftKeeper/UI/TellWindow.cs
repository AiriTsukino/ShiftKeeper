using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ShiftKeeper.Models;
using ShiftKeeper.Services;
using ShiftKeeper.UI.Components;

namespace ShiftKeeper.UI;

public sealed class TellWindow : Window, IDisposable
{
    private readonly ChatCommandService chat;
    private readonly CancellationTokenSource lifetime = new();
    private StaffMember? recipient;
    private string message = string.Empty;
    private string status = string.Empty;
    private bool sending;

    public TellWindow(ChatCommandService chat)
        : base("Send Staff Tell###ShiftKeeperTellWindow", ImGuiWindowFlags.NoFocusOnAppearing)
    {
        this.chat = chat;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(460, 250),
            MaximumSize = new Vector2(760, 520),
        };
    }

    public void OpenFor(StaffMember member)
    {
        recipient = member;
        message = string.Empty;
        status = string.Empty;
        IsOpen = true;
    }

    public override void PreDraw() => ShiftKeeperTheme.Push();
    public override void PostDraw() => ShiftKeeperTheme.Pop();

    public override void Draw()
    {
        if (recipient is null)
        {
            ImGui.TextWrapped("No staff member is selected.");
            return;
        }

        ImGui.TextDisabled("Recipient");
        ImGui.TextWrapped(recipient.TellRecipient);
        ImGui.Spacing();
        ImGui.TextDisabled("Message");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextMultiline("##shift-keeper-tell-message", ref message, 450, new Vector2(-1, 96));
        var enterPressed = ImGui.IsItemActive() && ImGui.IsKeyPressed(ImGuiKey.Enter);
        ImGui.TextDisabled($"{message.Length}/450 characters. Press Enter or Send to deliver the tell.");
        UiHelpers.Help("ShiftKeeper sends this through the game's native chat command system as /tell Firstname Lastname@World message. Line breaks are converted to spaces.");

        if (!string.IsNullOrWhiteSpace(status))
            UiHelpers.Status(status, status.StartsWith("Sent", StringComparison.Ordinal) ? ShiftKeeperTheme.Green : ShiftKeeperTheme.Amber);

        var canSend = !sending && !string.IsNullOrWhiteSpace(message);
        if (!canSend) ImGui.BeginDisabled();
        if (ImGui.Button("Send Tell", new Vector2(120, 0)) || enterPressed && canSend)
            _ = SendAsync(recipient.TellRecipient, message);
        if (!canSend) ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(100, 0))) IsOpen = false;
    }

    private async Task SendAsync(string recipientText, string text)
    {
        if (sending) return;
        sending = true;
        status = "Sending…";
        var sent = await chat.SendTellAsync(recipientText, text, lifetime.Token).ConfigureAwait(false);
        status = sent ? $"Sent to {recipientText}." : chat.LastError;
        if (sent) message = string.Empty;
        sending = false;
    }

    public void Dispose() => lifetime.Cancel();
}
