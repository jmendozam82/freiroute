using System;
using System.Data;
using Dapper;

namespace Freiroute.DAL.TypeHandlers;

/// <summary>
/// Permite que Dapper sepa cómo mapear System.TimeOnly hacia y desde PostgreSQL (time).
/// </summary>
public class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override void SetValue(IDbDataParameter parameter, TimeOnly time)
    {
        parameter.Value = time.ToTimeSpan();
    }

    public override TimeOnly Parse(object value)
    {
        if (value is TimeSpan timeSpan)
        {
            return TimeOnly.FromTimeSpan(timeSpan);
        }
        if (value is DateTime dateTime)
        {
            return TimeOnly.FromDateTime(dateTime);
        }
        
        throw new InvalidCastException($"No se puede castear {value.GetType()} a TimeOnly");
    }
}
