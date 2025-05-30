using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using NRPC.Abstractions.Metadata;

namespace SuperSocket.JsonRpc;

public class JsonElementExpressionConverter : IExpressionConverter
{
    public bool CanConvert(Type parameterType, Type sourceType)
    {
        // We can convert from object (containing JsonElement) to most primitive and common types
        return sourceType == typeof(object) && IsSupportedTargetType(parameterType);
    }

    public Expression Convert(Expression sourceExpression, Type dataType)
    {
        if (!CanConvert(dataType, sourceExpression.Type))
        {
            throw new NotSupportedException($"Cannot convert from {sourceExpression.Type} to {dataType}");
        }

        // Cast the source object to JsonElement
        var jsonElementExpression = Expression.Convert(sourceExpression, typeof(JsonElement));

        // Create the conversion expression based on the target type
        return CreateConversionExpression(jsonElementExpression, dataType);
    }

    private bool IsSupportedTargetType(Type type)
    {
        // Remove nullable wrapper if present
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType == typeof(string) ||
               underlyingType == typeof(int) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(bool) ||
               underlyingType == typeof(byte) ||
               underlyingType == typeof(sbyte) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(ushort) ||
               underlyingType == typeof(uint) ||
               underlyingType == typeof(ulong) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(DateTimeOffset) ||
               underlyingType == typeof(Guid) ||
               underlyingType == typeof(JsonElement);
    }

    private Expression CreateConversionExpression(Expression jsonElementExpression, Type targetType)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        var isNullable = underlyingType != null;
        var actualType = underlyingType ?? targetType;

        // If target type is JsonElement, just return it directly
        if (actualType == typeof(JsonElement))
        {
            return isNullable ? 
                Expression.Convert(jsonElementExpression, targetType) : 
                jsonElementExpression;
        }

        // Create the conversion expression for the underlying type
        Expression conversionExpression = actualType switch
        {
            Type t when t == typeof(string) => CreateStringConversion(jsonElementExpression),
            Type t when t == typeof(int) => CreateNumericConversion(jsonElementExpression, typeof(int), nameof(JsonElement.GetInt32)),
            Type t when t == typeof(long) => CreateNumericConversion(jsonElementExpression, typeof(long), nameof(JsonElement.GetInt64)),
            Type t when t == typeof(double) => CreateNumericConversion(jsonElementExpression, typeof(double), nameof(JsonElement.GetDouble)),
            Type t when t == typeof(float) => CreateNumericConversion(jsonElementExpression, typeof(float), nameof(JsonElement.GetSingle)),
            Type t when t == typeof(decimal) => CreateNumericConversion(jsonElementExpression, typeof(decimal), nameof(JsonElement.GetDecimal)),
            Type t when t == typeof(bool) => CreateBooleanConversion(jsonElementExpression),
            Type t when t == typeof(byte) => CreateNumericConversion(jsonElementExpression, typeof(byte), nameof(JsonElement.GetByte)),
            Type t when t == typeof(sbyte) => CreateNumericConversion(jsonElementExpression, typeof(sbyte), nameof(JsonElement.GetSByte)),
            Type t when t == typeof(short) => CreateNumericConversion(jsonElementExpression, typeof(short), nameof(JsonElement.GetInt16)),
            Type t when t == typeof(ushort) => CreateNumericConversion(jsonElementExpression, typeof(ushort), nameof(JsonElement.GetUInt16)),
            Type t when t == typeof(uint) => CreateNumericConversion(jsonElementExpression, typeof(uint), nameof(JsonElement.GetUInt32)),
            Type t when t == typeof(ulong) => CreateNumericConversion(jsonElementExpression, typeof(ulong), nameof(JsonElement.GetUInt64)),
            Type t when t == typeof(DateTime) => CreateDateTimeConversion(jsonElementExpression),
            Type t when t == typeof(DateTimeOffset) => CreateDateTimeOffsetConversion(jsonElementExpression),
            Type t when t == typeof(Guid) => CreateGuidConversion(jsonElementExpression),
            _ => throw new NotSupportedException($"Conversion to {actualType} is not supported")
        };

        // If the target type is nullable, wrap the conversion in a nullable constructor
        return isNullable ? 
            Expression.Convert(conversionExpression, targetType) : 
            conversionExpression;
    }

    private Expression CreateStringConversion(Expression jsonElementExpression)
    {
        // jsonElement.GetString()
        return Expression.Call(
            jsonElementExpression,
            typeof(JsonElement).GetMethod(nameof(JsonElement.GetString))!
        );
    }

    private Expression CreateNumericConversion(Expression jsonElementExpression, Type targetType, string methodName)
    {
        // jsonElement.GetInt32(), GetInt64(), etc.
        return Expression.Call(
            jsonElementExpression,
            typeof(JsonElement).GetMethod(methodName)!
        );
    }

    private Expression CreateBooleanConversion(Expression jsonElementExpression)
    {
        // jsonElement.GetBoolean()
        return Expression.Call(
            jsonElementExpression,
            typeof(JsonElement).GetMethod(nameof(JsonElement.GetBoolean))!
        );
    }

    private Expression CreateDateTimeConversion(Expression jsonElementExpression)
    {
        // jsonElement.GetDateTime()
        return Expression.Call(
            jsonElementExpression,
            typeof(JsonElement).GetMethod(nameof(JsonElement.GetDateTime))!
        );
    }

    private Expression CreateDateTimeOffsetConversion(Expression jsonElementExpression)
    {
        // jsonElement.GetDateTimeOffset()
        return Expression.Call(
            jsonElementExpression,
            typeof(JsonElement).GetMethod(nameof(JsonElement.GetDateTimeOffset))!
        );
    }

    private Expression CreateGuidConversion(Expression jsonElementExpression)
    {
        // jsonElement.GetGuid()
        return Expression.Call(
            jsonElementExpression,
            typeof(JsonElement).GetMethod(nameof(JsonElement.GetGuid))!
        );
    }
}
