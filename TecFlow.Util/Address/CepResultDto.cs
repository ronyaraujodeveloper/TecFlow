namespace TecFlow.Util.Address;

public sealed class CepResultDto
{
    public required string ZipCode { get; init; }

    public required string Street { get; init; }

    public required string Neighborhood { get; init; }

    public required string City { get; init; }

    public required string State { get; init; }
}
