namespace Aoyon.MaterialEditor;

[Serializable]
internal class MaterialEditSettings
{
    public List<MaterialEditCondition> Conditions = new();
}

[Serializable]
internal class MaterialEditCondition
{
    public enum Kind
    {
        None = 0,

        Direct = 1,
        ShaderAttribute = 2,
        ShaderSpecific = 1000,
    }

    public Kind ConditionKind = Kind.None;
    public MaterialDirectWrite Direct = new();
    public MaterialShaderAttributeWrite ShaderAttribute = new();
    public MaterialShaderSpecificOperation ShaderSpecific = new();
}

[Serializable]
internal class MaterialDirectWrite
{
    public enum Kind
    {
        None = 0,
        Shader = 1,
        Property = 2,
        PropertyTexture = 3,
        PropertyTextureOffset = 4,
        PropertyTextureScale = 5,
        RenderQueue = 10,
        LightmapFlags = 11,
        EnableInstancing = 12,
        DoubleSidedGI = 13,
        Keyword = 20,
        StringTag = 21,
        ShaderPass = 22,
    }

    public Kind WriteKind = Kind.None;

    public Shader? Shader = null;
    public string PropertyName = string.Empty;
    public MaterialProperty Property = new();
    public Texture? TextureValue = null;
    public Vector2 Vector2Value = Vector2.zero;

    public MaterialKeywordStateOverride Keyword = new();
    public MaterialStringTagOverride StringTag = new();
    public MaterialShaderPassStateOverride ShaderPass = new();

    public int RenderQueueValue = -1;
    public MaterialGlobalIlluminationFlags LightmapFlagsValue = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
    public bool BoolValue = false;
}

[Serializable]
internal class MaterialShaderAttributeWrite
{
    public enum Kind
    {
        None = 0,
        Toggle = 1,
        ToggleOff = 2,
        KeywordEnum = 3,
    }

    public Kind AttributeKind = Kind.None;
    public MaterialProperty Property = new();
    public string Keyword = string.Empty;
    public List<string> Keywords = new();
}

[Serializable]
internal class MaterialShaderSpecificOperation
{
    public string ShaderFamily = string.Empty;
    public string OperationId = string.Empty;
    public int OperationVersion = 1;
    public List<MaterialEditArgument> Arguments = new();

    public bool TryGetBool(string key, out bool value)
    {
        if (TryGetArgument(key, MaterialEditArgument.Kind.Bool, out var argument))
        {
            value = argument.BoolValue;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetInt(string key, out int value)
    {
        if (TryGetArgument(key, MaterialEditArgument.Kind.Int, out var argument))
        {
            value = argument.IntValue;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetString(string key, out string value)
    {
        if (TryGetArgument(key, MaterialEditArgument.Kind.String, out var argument))
        {
            value = argument.StringValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public bool TryGetObject<T>(string key, [NotNullWhen(true)] out T? value)
        where T : UnityEngine.Object
    {
        if (TryGetArgument(key, MaterialEditArgument.Kind.Object, out var argument)
            && argument.ObjectValue is T typed)
        {
            value = typed;
            return true;
        }

        value = null;
        return false;
    }

    private bool TryGetArgument(string key, MaterialEditArgument.Kind kind, [NotNullWhen(true)] out MaterialEditArgument? argument)
    {
        argument = Arguments.FirstOrDefault(item => item.Key == key && item.ValueKind == kind);
        return argument != null;
    }
}

[Serializable]
internal class MaterialEditArgument
{
    public enum Kind
    {
        Bool,
        Int,
        Float,
        Color,
        Vector,
        String,
        Object,
    }

    public string Key = string.Empty;
    public Kind ValueKind = Kind.Bool;

    public bool BoolValue = false;
    public int IntValue = 0;
    public float FloatValue = 0f;
    public Color ColorValue = Color.white;
    public Vector4 VectorValue = Vector4.zero;
    public string StringValue = string.Empty;
    public UnityEngine.Object? ObjectValue = null;
}
