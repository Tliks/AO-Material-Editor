namespace Aoyon.MaterialEditor.Migration;

internal interface IMigrator
{
    public int TargetVersion { get; }
    void MigrateImpl(MaterialEditorComponentBase component);
}

internal static class Migrator
{
    private static readonly IMigrator[] migrators = new IMigrator[]
    {
        new V0(),
    };

    public static void Migrate(MaterialEditorComponentBase component)
    {
        Undo.RecordObject(component, "Migrate Material Editor");
        var migrationCount = 0;
        for (var i = 0; i < migrators.Length; i++)
        {
            var migrator = migrators[i];
            if (component.DataVersion == migrator.TargetVersion)
            {
                migrator.MigrateImpl(component);
                component.DataVersion = migrator.TargetVersion + 1;
                migrationCount++;
            }
        }
        if (migrationCount > 0)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }

    public static void DrawMigrationButton(MaterialEditorComponentBase component)
    {
        if (GUILayout.Button("migration.required".LS()))
        {
            Migrate(component);
        }
    }

    /// <summary>
    /// Check if the component is up to date and draw the migration button if it is not.
    /// Return true if the component is up to date.
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    public static bool CheckAndDrawMigrationButton(MaterialEditorComponentBase component)
    {
        if (component.IsLatestDataVersion()) return true;

        DrawMigrationButton(component);
        return false;
    }
}
