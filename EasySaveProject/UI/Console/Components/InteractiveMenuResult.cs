public enum InteractiveActionType
{
    None,
    Job,
    Create,
    Run,
    Back
}

public class InteractiveMenuResult
{
    public bool Cancelled { get; set; }

    public bool CreateRequested { get; set; }

    public bool HasActiveSelection { get; set; }
    public InteractiveActionType ActionType { get; set; } = InteractiveActionType.None;

    public bool IsMultiSelection { get; set; }

    public List<int> SelectedIndices { get; set; } = new();

    public int SelectedIndex { get; set; }

}