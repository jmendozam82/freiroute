using System;
using System.Data;
using Dapper;

namespace Freiroute.DAL.TypeHandlers;

/// <summary>
/// Permite que Dapper sepa cómo mapear System.DateOnly hacia y desde PostgreSQL (date).
/// </summary>
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly date)
    {
        parameter.Value = date.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value)
    {
        if (value is DateTime dateTime)
        {
            return DateOnly.FromDateTime(dateTime);
        }
        
        throw new InvalidCastException($"No se puede castear {value.GetType()} a DateOnly");
    }
}
