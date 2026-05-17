namespace Aoyon.MaterialEditor.Migration;

[FilePath(ProjectMigrationState.StateFilePath, FilePathAttribute.Location.ProjectFolder)]
internal sealed class ProjectMigrationState : ScriptableSingleton<ProjectMigrationState>
{
    public const string StateFilePath = "Library/" + Constants.QualifiedName + "/ProjectMigrationState.json";
    private const int InitialGuaranteedDataVersion = 1;

    [InitializeOnLoadMethod]
    static void EnsureCreated()
    {
        if (System.IO.File.Exists(StateFilePath)) return;

        instance.Save(true);
    }

    [SerializeField]
    private int guaranteedDataVersion = InitialGuaranteedDataVersion;

    public int GuaranteedDataVersion => guaranteedDataVersion;

    public bool IsGuaranteedAtLeast(int dataVersion)
    {
        return guaranteedDataVersion >= dataVersion;
    }

    public void MarkGuaranteedAtLeast(int dataVersion)
    {
        if (guaranteedDataVersion >= dataVersion) return;

        guaranteedDataVersion = dataVersion;
        Save(true);
    }

}
