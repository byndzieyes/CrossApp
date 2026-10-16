namespace Core.Reports;

public sealed record MovementSummary(
    string Sku,
    string Name,
    string Unit,
    int MovementCount,
    int Received,
    int Issued,
    int NetChange);
