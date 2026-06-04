using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DragonCAD.App;
using DragonCAD.Core.Components.Definitions;

namespace DragonCAD.App.ComponentEditor;

public sealed class ComponentEditorWorkspaceHost : INotifyPropertyChanged
{
    private readonly ObservableCollection<ComponentEditorWorkspace> openWorkspaces = [];
    private int newComponentSequence;
    private ComponentEditorWorkspace? activeWorkspace;
    private ComponentEditorCloseDecision? pendingCloseDecision;

    public ComponentEditorWorkspaceHost()
    {
        NewComponentCommand = new DelegateCommand(OpenNewComponent);
        EditComponentCommand = new DelegateCommand(OpenEditComponent);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand NewComponentCommand { get; }

    public ICommand EditComponentCommand { get; }

    public IReadOnlyList<ComponentEditorWorkspace> OpenWorkspaces => openWorkspaces;

    public ComponentEditorWorkspace? ActiveWorkspace
    {
        get => activeWorkspace;
        private set => SetField(ref activeWorkspace, value);
    }

    public ComponentEditorCloseDecision? PendingCloseDecision
    {
        get => pendingCloseDecision;
        private set => SetField(ref pendingCloseDecision, value);
    }

    public ComponentEditorWorkspace OpenNewComponent()
    {
        newComponentSequence++;
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew($"dragon:new-component-{newComponentSequence:000}");
        OpenWorkspace(workspace);
        return workspace;
    }

    public ComponentEditorWorkspace OpenEditComponent(ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartEdit(definition);
        OpenWorkspace(workspace);
        return workspace;
    }

    public ComponentEditorCloseDecision RequestClose(ComponentEditorWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (!openWorkspaces.Contains(workspace))
        {
            ComponentEditorCloseDecision ignored = ComponentEditorCloseDecision.Ignored(workspace, "Workspace is already closed.");
            PendingCloseDecision = null;
            return ignored;
        }

        if (!workspace.IsDirty)
        {
            CloseWorkspace(workspace);
            PendingCloseDecision = null;
            return ComponentEditorCloseDecision.CloseNow(workspace);
        }

        ComponentEditorCloseDecision decision = ComponentEditorCloseDecision.PromptRequired(
            workspace,
            $"Unsaved changes in {workspace.ViewModel.ComponentId}",
            "Close without saving this component editor workspace?");
        PendingCloseDecision = decision;
        return decision;
    }

    public void ConfirmClose(ComponentEditorCloseDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.Kind != ComponentEditorCloseDecisionKind.PromptRequired ||
            !ReferenceEquals(PendingCloseDecision, decision))
        {
            return;
        }

        CloseWorkspace(decision.Workspace);
        PendingCloseDecision = null;
    }

    public void CancelClose() => PendingCloseDecision = null;

    private void OpenNewComponent(object? _) => OpenNewComponent();

    private void OpenEditComponent(object? parameter)
    {
        if (parameter is not ComponentDefinition definition)
        {
            throw new ArgumentException("Edit component command requires a component definition.", nameof(parameter));
        }

        OpenEditComponent(definition);
    }

    private void OpenWorkspace(ComponentEditorWorkspace workspace)
    {
        openWorkspaces.Add(workspace);
        ActiveWorkspace = workspace;
        PendingCloseDecision = null;
        OnPropertyChanged(nameof(OpenWorkspaces));
    }

    private void CloseWorkspace(ComponentEditorWorkspace workspace)
    {
        if (!openWorkspaces.Remove(workspace))
        {
            return;
        }

        if (ReferenceEquals(ActiveWorkspace, workspace))
        {
            ActiveWorkspace = openWorkspaces.LastOrDefault();
        }

        OnPropertyChanged(nameof(OpenWorkspaces));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record ComponentEditorCloseDecision(
    ComponentEditorCloseDecisionKind Kind,
    ComponentEditorWorkspace Workspace,
    string Title,
    string Message)
{
    public static ComponentEditorCloseDecision CloseNow(ComponentEditorWorkspace workspace) =>
        new(ComponentEditorCloseDecisionKind.CloseNow, workspace, "Close component editor", "Workspace can close.");

    public static ComponentEditorCloseDecision PromptRequired(ComponentEditorWorkspace workspace, string title, string message) =>
        new(ComponentEditorCloseDecisionKind.PromptRequired, workspace, title, message);

    public static ComponentEditorCloseDecision Ignored(ComponentEditorWorkspace workspace, string message) =>
        new(ComponentEditorCloseDecisionKind.Ignored, workspace, "Close component editor", message);
}

public enum ComponentEditorCloseDecisionKind
{
    CloseNow,
    PromptRequired,
    Ignored
}
