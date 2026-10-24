namespace NaturalQuery.Application.Abstractions.Sql;

public interface IReadOnlySqlValidator
{
    string ValidateAndNormalize(string sql);
}
