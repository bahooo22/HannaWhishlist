namespace WishlistService.Contracts.Responses;

public class OperationResponse<T>
{
    public bool Ok { get; set; }
    public T? Result { get; set; }
    public string? ErrorMessage { get; set; }
}
