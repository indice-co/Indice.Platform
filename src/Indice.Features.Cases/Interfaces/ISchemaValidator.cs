namespace Indice.Features.Cases.Interfaces
{
    internal interface ISchemaValidator
    {
        bool IsValid(string schema, string data);
    }
}
