using UnityEngine.Rendering;

namespace Aoyon.MaterialEditor;

[Serializable]
internal struct MaterialProperty : IEquatable<MaterialProperty>
{
    public string PropertyName;
    public ShaderPropertyType PropertyType;

    // ShaderPropertyType.Texture
    public Texture? TextureValue;
    public Vector2 TextureOffsetValue;
    public Vector2 TextureScaleValue;
    // ShaderPropertyType.Color
    public Color ColorValue;
    // ShaderPropertyType.Vector
    public Vector4 VectorValue;
    // ShaderPropertyType.Int
    public int IntValue;
    // ShaderPropertyType.Float, ShaderPropertyType.Range
    public float FloatValue;

    public MaterialProperty()
    {
        PropertyName = string.Empty;
        PropertyType = ShaderPropertyType.Float;
        TextureValue = null;
        TextureOffsetValue = Vector2.zero;
        TextureScaleValue = Vector2.one;
        ColorValue = Color.white;
        VectorValue = Vector4.zero;
        IntValue = 0;
        FloatValue = 0f;
    }

    public readonly string PropertyValue =>
        PropertyType switch
        {
            ShaderPropertyType.Texture => TextureValue != null ? TextureValue.name : "null",
            ShaderPropertyType.Color => ColorValue.ToString(),
            ShaderPropertyType.Vector => VectorValue.ToString(),
            ShaderPropertyType.Int => IntValue.ToString(),
            ShaderPropertyType.Float => FloatValue.ToString(),
            ShaderPropertyType.Range => FloatValue.ToString(),
            _ => throw new NotImplementedException(),
        };

    public readonly bool TrySet(Material mat)
    {
        if (!Validate(mat, PropertyName, PropertyType)) return false;

        switch (PropertyType)
        {
            case ShaderPropertyType.Texture:
                {
                    mat.SetTexture(PropertyName, TextureValue);
                    mat.SetTextureOffset(PropertyName, TextureOffsetValue);
                    mat.SetTextureScale(PropertyName, TextureScaleValue);
                    break;
                }
            case ShaderPropertyType.Color:
                {
                    mat.SetColor(PropertyName, ColorValue);
                    break;
                }
            case ShaderPropertyType.Vector:
                {
                    mat.SetVector(PropertyName, VectorValue);
                    break;
                }
            case ShaderPropertyType.Int:
                {
                    mat.SetInt(PropertyName, IntValue);
                    break;
                }
            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                {
                    mat.SetFloat(PropertyName, FloatValue);
                    break;
                }
        }
        return true;
    }

    public static bool TryGet(Material mat, string propertyName, out MaterialProperty materialProperty)
    {
        return TryGet(mat, mat.shader.FindPropertyIndex(propertyName), out materialProperty);
    }

    public static bool TryGet(Material mat, int propertyIndex, out MaterialProperty materialProperty)
    {
        materialProperty = default;
        var shader = mat.shader;

        if (!ValidateIndex(shader, propertyIndex)) return false;
        var propertyNameID = shader.GetPropertyNameId(propertyIndex);
        var propertyName = shader.GetPropertyName(propertyIndex);
        var propertyType = shader.GetPropertyType(propertyIndex);

        materialProperty = new MaterialProperty
        {
            PropertyName = propertyName,
            PropertyType = propertyType
        };

        switch (propertyType)
        {
            case ShaderPropertyType.Texture:
                {
                    materialProperty.TextureValue = mat.GetTexture(propertyNameID);
                    materialProperty.TextureOffsetValue = mat.GetTextureOffset(propertyNameID);
                    materialProperty.TextureScaleValue = mat.GetTextureScale(propertyNameID);
                    break;
                }
            case ShaderPropertyType.Color:
                {
                    materialProperty.ColorValue = mat.GetColor(propertyNameID);
                    break;
                }
            case ShaderPropertyType.Vector:
                {
                    materialProperty.VectorValue = mat.GetVector(propertyNameID);
                    break;
                }
            case ShaderPropertyType.Int:
                {
                    materialProperty.IntValue = mat.GetInt(propertyNameID);
                    break;
                }
            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                {
                    materialProperty.FloatValue = mat.GetFloat(propertyNameID);
                    break;
                }
        }
        return true;
    }

    public static bool TryGetDefualtValue(Shader shader, int propertyIndex, Func<string, Texture?> getDefaultTexture, out MaterialProperty materialProperty)
    {
        materialProperty = default;

        if (!ValidateIndex(shader, propertyIndex)) return false;
        
        var propertyName = shader.GetPropertyName(propertyIndex);
        var propertyType = shader.GetPropertyType(propertyIndex);

        materialProperty = new MaterialProperty
        {
            PropertyName = propertyName,
            PropertyType = propertyType
        };

        switch (propertyType)
        {
            case ShaderPropertyType.Texture:
                materialProperty.TextureValue = getDefaultTexture(propertyName);
                materialProperty.TextureOffsetValue = new(0, 0);
                materialProperty.TextureScaleValue = new(1,1);
                break;
            case ShaderPropertyType.Vector:
                materialProperty.VectorValue = shader.GetPropertyDefaultVectorValue(propertyIndex);
                break;
            case ShaderPropertyType.Color:
                materialProperty.ColorValue = shader.GetPropertyDefaultVectorValue(propertyIndex);
                break;
            case ShaderPropertyType.Int:
                materialProperty.IntValue = shader.GetPropertyDefaultIntValue(propertyIndex);
                break;
            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                materialProperty.FloatValue = shader.GetPropertyDefaultFloatValue(propertyIndex);
                break;
        }

        return true;
    }

    private static bool Validate(Material mat, string propertyName, ShaderPropertyType propertyType)
    {
        if (!mat.HasProperty(propertyName))
        {
            return false;
        }
        var propertyIndex = mat.shader.FindPropertyIndex(propertyName);
        if (propertyIndex == -1 || !IsCompatiblePropertyType(mat.shader.GetPropertyType(propertyIndex), propertyType))
        {
            return false;
        }

        return true;
    }

    private static bool IsCompatiblePropertyType(ShaderPropertyType a, ShaderPropertyType b)
    {
        if (a == b) return true;
        return IsFloatLike(a) && IsFloatLike(b);
    }
    
    private static bool ValidateIndex(Shader shader, int propertyIndex)
    {
        return 0 <= propertyIndex && propertyIndex < shader.GetPropertyCount();
    }

    public readonly bool EqualsImpl(MaterialProperty other, bool strict)
    {
        if (PropertyType != other.PropertyType
            && (!IsFloatLike(PropertyType) || !IsFloatLike(other.PropertyType))) return false;
        if (PropertyName != other.PropertyName) return false;

        switch (PropertyType)
        {
            case ShaderPropertyType.Texture:
                var textureEquals = (TextureValue == null && other.TextureValue == null)
                    || (TextureValue != null && TextureValue.Equals(other.TextureValue));
                return textureEquals
                    && TextureOffsetValue.Equals(other.TextureOffsetValue)
                    && TextureScaleValue.Equals(other.TextureScaleValue);
            case ShaderPropertyType.Color:
                return strict ? ColorValue.Equals(other.ColorValue) : ColorValue == other.ColorValue;
            case ShaderPropertyType.Vector:
                return strict ? VectorValue.Equals(other.VectorValue) : VectorValue == other.VectorValue;
            case ShaderPropertyType.Int:
                return IntValue == other.IntValue;
            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                return strict ? FloatValue == other.FloatValue : Mathf.Approximately(FloatValue, other.FloatValue);
            default:
                return false;
        }
    }

    private static bool IsFloatLike(ShaderPropertyType type)
    {
        return type is ShaderPropertyType.Float or ShaderPropertyType.Range;
    }

    public readonly bool Equals(MaterialProperty other)
    {
        return EqualsImpl(other, true);
    }

    public override readonly int GetHashCode()
    {
        switch (PropertyType)
        {
            default: return HashCode.Combine(PropertyName, PropertyType);
            case ShaderPropertyType.Texture:
                {
                    return HashCode.Combine(PropertyName, PropertyType, TextureValue, TextureOffsetValue, TextureScaleValue);
                }
            case ShaderPropertyType.Color:
                {
                    return HashCode.Combine(PropertyName, PropertyType, ColorValue);
                }
            case ShaderPropertyType.Vector:
                {
                    return HashCode.Combine(PropertyName, PropertyType, VectorValue);
                }
            case ShaderPropertyType.Int:
                {
                    return HashCode.Combine(PropertyName, PropertyType, IntValue);
                }
            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                {
                    return HashCode.Combine(PropertyName, PropertyType, FloatValue);
                }
        }
    }
}

[Serializable]
internal struct MaterialKeywordStateOverride : IEquatable<MaterialKeywordStateOverride>
{
    public string Keyword;
    public bool Enabled;

    public MaterialKeywordStateOverride()
    {
        Keyword = string.Empty;
        Enabled = true;
    }

    public readonly bool Equals(MaterialKeywordStateOverride other)
    {
        return Keyword == other.Keyword && Enabled == other.Enabled;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Keyword, Enabled);
    }
}

[Serializable]
internal struct MaterialStringTagOverride : IEquatable<MaterialStringTagOverride>
{
    public string TagName;
    public string Value;
    public bool Remove;

    public MaterialStringTagOverride()
    {
        TagName = string.Empty;
        Value = string.Empty;
        Remove = false;
    }

    public readonly bool Equals(MaterialStringTagOverride other)
    {
        return TagName == other.TagName && Value == other.Value && Remove == other.Remove;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(TagName, Value, Remove);
    }
}

[Serializable]
internal struct MaterialShaderPassStateOverride : IEquatable<MaterialShaderPassStateOverride>
{
    public string PassName;
    public bool Enabled;

    public MaterialShaderPassStateOverride()
    {
        PassName = string.Empty;
        Enabled = true;
    }

    public readonly bool Equals(MaterialShaderPassStateOverride other)
    {
        return PassName == other.PassName && Enabled == other.Enabled;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(PassName, Enabled);
    }
}
